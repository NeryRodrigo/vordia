using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Gesco.Application.Services
{
    public interface IAndeScrapingService
    {
        Task<(decimal Deuda, int Kw)> ConsultarConsumoAsync(string nis);
        Task<AndeConsultaResultadoDto> ConsultarEstadoAndeAsync(string nis);
        /// <summary>Abre Chrome con login ANDE; espera Acceder; minimiza. No consulta NIS.</summary>
        Task<(bool Ok, string Mensaje, bool YaEstabaLogueado)> AsegurarSesionAsync();
        /// <summary>PDF oficial vía Chrome del portal (misma sesión del relogin / Consultar Saldos). Null si falla.</summary>
        Task<byte[]?> DescargarFacturaPdfOficialPortalAsync(
            string nis,
            string? pdfUrlCompleta = null,
            string? secNis = null,
            string? secRec = null,
            string? fFact = null,
            bool electronica = true,
            string? kwfxToken = null,
            string? origen = null);
    }

    public class AndeScrapingService : IAndeScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AndeScrapingService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAndeCredentialsStore _creds;

        public AndeScrapingService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<AndeScrapingService> logger,
            IServiceScopeFactory scopeFactory,
            IAndeCredentialsStore creds)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _creds = creds;
        }

        private static readonly System.Threading.SemaphoreSlim _portalLock = new(1, 1);
        private static IBrowser? _sharedBrowser;
        private static IPage? _sharedPage;
        private static string? _profileDir;
        /// <summary>True tras Acceder OK en este proceso; evita pedir login otra vez al consultar NIS.</summary>
        private static bool _sesionAndeActiva;

        public async Task<(decimal Deuda, int Kw)> ConsultarConsumoAsync(string nis)
        {
            var consulta = await ConsultarFacturasRealesAsync(nis);
            if (consulta == null) return (0, 0);
            return (consulta.SaldoDeuda, consulta.ConsumoKw);
        }

        /// <summary>
        /// Consulta facturas reales: login Mi Cuenta manual (Acceder) + sesion Chrome reutilizada.
        /// </summary>
        private async Task<AndeConsultaResultadoDto?> ConsultarFacturasRealesAsync(string nis)
        {
            if (string.IsNullOrWhiteSpace(nis) || nis == "N/A" || nis == "SIN NIS" || nis == "\u2014")
                return null;

            string nisLimpio = new string(nis.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(nisLimpio)) return null;

            var viaPortal = await ConsultarViaPortalMiAndeAsync(nisLimpio);
            if (viaPortal != null) return viaPortal;

            var usarLegacy = bool.TryParse(_config["AndeConfig:UsarLegacy"], out var ul) && ul;
            if (!usarLegacy)
            {
                _logger.LogWarning("ANDE portal no devolvio facturas. Legacy desactivado (AndeConfig:UsarLegacy=false).");
                return null;
            }

            return await ConsultarViaEndpointLegacyAsync(nisLimpio);
        }

        private async Task<bool> AbrirConsultaFacturasPublicaAsync(IPage page, string portalUrl)
        {
            async Task<bool> TieneConsultaPublicaListaAsync()
            {
                try
                {
                    return await page.EvaluateFunctionAsync<bool>(@"() => {
                        const nis = document.querySelector('#in-Factura_Publica_nis');
                        if (!nis) return false;
                        // Contenedor visible o al menos el input existe tras cambiarContainer
                        const visible = !!(nis.offsetParent || (nis.getClientRects && nis.getClientRects().length));
                        const containerVisible = (() => {
                          const n = (document.body.innerText||'').normalize('NFD').replace(/[\u0300-\u036f]/g,'');
                          return /Consulte su Factura|Consulta de Facturas/i.test(n);
                        })();
                        return visible || containerVisible;
                      }");
                }
                catch { return false; }
            }

            async Task SafeGotoAsync(string url)
            {
                await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                    Timeout = 90000
                });
                await Task.Delay(3500);
            }

            try
            {
                await SafeGotoAsync(portalUrl);
                var preview = await page.EvaluateFunctionAsync<string>(@"() => ((document.body&&document.body.innerText)||'').replace(/\s+/g,' ').slice(0,250)");
                _logger.LogInformation("ANDE home preview: {Preview}", preview);

                // API oficial del portal SPA (descubierto en DOM):
                // <a onclick="cambiarContainer('factura_publico')">Consulta de Facturas</a>
                var opened = await page.EvaluateFunctionAsync<bool>(@"() => {
                    try {
                      if (typeof cambiarContainer === 'function') {
                        cambiarContainer('factura_publico');
                        return true;
                      }
                    } catch (e) {}
                    const a = document.querySelector(""a[onclick*=factura_publico]"")
                      || Array.from(document.querySelectorAll('a')).find(e => /Consulta de Facturas/i.test((e.textContent||'').trim()));
                    if (a) { a.click(); return true; }
                    return false;
                  }");
                _logger.LogInformation("ANDE: cambiarContainer/factura_publico => {Ok}", opened);
                await Task.Delay(2500);

                if (await TieneConsultaPublicaListaAsync())
                {
                    _logger.LogInformation("ANDE: Consulta de Facturas publica lista (#in-Factura_Publica_nis)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE: fallo abriendo consulta publica ({Msg})", ex.Message);
            }

            return false;
        }

        private async Task<bool> CompletarNisYConsultarAsync(IPage page, string nis)
        {
            try
            {
                return await page.EvaluateFunctionAsync<bool>(@"(nis) => {
                    const setNative = (el, value) => {
                      if (!el) return false;
                      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set;
                      try { el.focus(); } catch (e) {}
                      if (setter) setter.call(el, value); else el.value = value;
                      el.dispatchEvent(new Event('input', { bubbles: true }));
                      el.dispatchEvent(new Event('change', { bubbles: true }));
                      el.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
                      return true;
                    };

                    try {
                      if (typeof cambiarContainer === 'function') cambiarContainer('factura_publico');
                    } catch (e) {}

                    const nisInput = document.querySelector('#in-Factura_Publica_nis');
                    if (!nisInput) return false;
                    setNative(nisInput, nis);

                    // ANDE oficial: facturaPublicoComponent.consultar() (requiere sesion Mi Cuenta)
                    try {
                      if (window.facturaPublicoComponent && typeof facturaPublicoComponent.consultar === 'function') {
                        facturaPublicoComponent.consultar();
                        return true;
                      }
                    } catch (e) {}

                    const root = document.querySelector('#container-factura_publico') || document;
                    const btn = Array.from(root.querySelectorAll('button, a, input[type=submit], input[type=button]'))
                      .find(el => /consultar/i.test((el.textContent||'') + (el.value||'')));
                    if (btn) { btn.click(); return true; }
                    return true;
                  }", nis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE: error al completar NIS ({Msg})", ex.Message);
                return false;
            }
        }

        private async Task<AndeConsultaResultadoDto?> ExtraerResultadoDesdePaginaAsync(IPage page, string nisLimpio)
        {
            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var listo = await page.EvaluateFunctionAsync<bool>(@"() => {
                        const txt = ((document.body && document.body.innerText) || '');
                        const n = txt.normalize('NFD').replace(/[\u0300-\u036f]/g,'');
                        return /Ultimos Comprobantes/i.test(n) || (/Gs\./i.test(txt) && /(Pagado|Pendiente)/i.test(txt)) || /RUC\s*\/\s*CI/i.test(txt);
                    }");
                    if (listo) break;
                }
                catch { /* nav */ }
            }

            string? extraccion = null;
            for (int intento = 0; intento < 5; intento++)
            {
                try
                {
                    extraccion = await page.EvaluateFunctionAsync<string>(@"() => {
                        const txt = ((document.body && document.body.innerText) || '').replace(/\u00a0/g, ' ');
                        const n = txt.normalize('NFD').replace(/[\u0300-\u036f]/g,'');
                        let titular = '';
                        let documento = '';
                        let mName = n.match(/(?:Name|Nombre)\s*:?\s*([A-Z][A-Za-z .]{3,80})/i);
                        if (mName) titular = mName[1].replace(/\s+/g,' ').trim();
                        if (!titular) {
                          const after = n.match(/NIS\s*:?\s*\d{5,12}[\s\S]{0,120}?([A-Z]{2,}(?:\s+[A-Z]{2,}){1,5})/);
                          if (after) titular = after[1].replace(/\s+/g,' ').trim();
                        }
                        const mDoc = n.match(/RUC\s*\/\s*CI\s*:?\s*(\d{4,12})/i) || n.match(/(?:RUC|C\.?I\.?)\s*:?\s*(\d{4,12})/i);
                        if (mDoc) documento = mDoc[1];
                        let scope = txt;
                        const idx = n.search(/Ultimos Comprobantes|Historico/i);
                        if (idx >= 0) scope = txt.slice(idx);
                        const facturas = [];
                        const moneyRe = /Gs\.?\s*([\d.]{3,})/gi;
                        let m;
                        while ((m = moneyRe.exec(scope)) !== null) {
                          const windowTxt = scope.slice(m.index, m.index + 280);
                          const estado = /Pendiente(?:\s+de\s+Pago)?/i.test(windowTxt) ? 'Pendiente de Pago'
                                       : /Pagado/i.test(windowTxt) ? 'Pagado' : '';
                          if (!estado) continue;
                          const em = windowTxt.match(/Emisi[oó]n\s*:?\s*(\d{1,2}\/\d{1,2}\/\d{2,4})/i);
                          const ve = windowTxt.match(/Vencimiento\s*:?\s*(\d{1,2}\/\d{1,2}\/\d{2,4})/i);
                          const raw = (m[1] || '').replace(/\./g,'');
                          if (!/^\d{4,9}$/.test(raw)) continue;
                          facturas.push({ monto: m[1], estado, emision: em ? em[1] : '', vencimiento: ve ? ve[1] : '' });
                        }
                        return JSON.stringify({ titular, documento, facturas, preview: txt.slice(0, 1200) });
                    }");
                    break;
                }
                catch { await Task.Delay(1200); }
            }

            if (string.IsNullOrWhiteSpace(extraccion)) return null;

            using var docJson = System.Text.Json.JsonDocument.Parse(extraccion);
            var root = docJson.RootElement;
            var parsed = new AndeConsultaResultadoDto { Nis = nisLimpio };
            if (root.TryGetProperty("titular", out var t) && t.GetString() is { Length: > 0 } titular)
                parsed.Titular = titular;
            if (root.TryGetProperty("documento", out var d) && d.GetString() is { Length: > 0 } documentoCli)
                parsed.Documento = documentoCli;

            if (root.TryGetProperty("facturas", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var montoStr = item.TryGetProperty("monto", out var mo) ? mo.GetString() : null;
                    if (!decimal.TryParse(LimpiarMonto(montoStr), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
                        continue;
                    parsed.Facturas.Add(new AndeFacturaItemDto
                    {
                        Monto = monto,
                        Estado = item.TryGetProperty("estado", out var esEl) ? (esEl.GetString() ?? "") : "",
                        Emision = item.TryGetProperty("emision", out var em) ? (em.GetString() ?? "") : "",
                        Vencimiento = item.TryGetProperty("vencimiento", out var ve) ? (ve.GetString() ?? "") : ""
                    });
                }
            }

            parsed.Facturas = parsed.Facturas
                .GroupBy(f => $"{f.Monto}|{f.Estado}|{f.Emision}|{f.Vencimiento}")
                .Select(g => g.First())
                .ToList();
            parsed.SaldoDeuda = parsed.Facturas.Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)).Sum(f => f.Monto);
            parsed.FacturasPendientes = parsed.Facturas.Count(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase));
            parsed.Vencimiento = parsed.Facturas.FirstOrDefault(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))?.Vencimiento
                ?? parsed.Facturas.FirstOrDefault()?.Vencimiento
                ?? "";
            if (string.IsNullOrWhiteSpace(parsed.Titular))
                parsed.Titular = "TITULAR ANDE";

            if (parsed.Facturas.Count == 0)
            {
                try
                {
                    var html = await page.GetContentAsync();
                    var alt = ParseHtmlConsultaAnde(html, nisLimpio);
                    if (alt != null) return alt;
                }
                catch { /* ignore */ }
                return null;
            }

            parsed.EsSimulado = false;
            return parsed;
        }

        private AndeConsultaResultadoDto? ParseFromCapturedXhr(
            System.Collections.Concurrent.ConcurrentBag<string> bodies, string nis)
        {
            if (bodies.IsEmpty) return null;
            AndeConsultaResultadoDto? best = null;
            foreach (var body in bodies)
            {
                try
                {
                    // HTML embebido en XHR
                    if (body.Contains("Gs.", StringComparison.OrdinalIgnoreCase)
                        && (body.Contains("Pagado", StringComparison.OrdinalIgnoreCase)
                            || body.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)))
                    {
                        var fromHtml = ParseHtmlConsultaAnde(body, nis);
                        if (fromHtml != null && fromHtml.Facturas.Count > (best?.Facturas.Count ?? 0))
                            best = fromHtml;
                    }

                    // JSON con montos
                    if (!(body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("[")))
                        continue;

                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var parsed = new AndeConsultaResultadoDto { Nis = nis };
                    void Walk(System.Text.Json.JsonElement el, string path)
                    {
                        switch (el.ValueKind)
                        {
                            case System.Text.Json.JsonValueKind.Object:
                                decimal? monto = null;
                                string estado = "", emision = "", venc = "", titular = "", documento = "";
                                foreach (var p in el.EnumerateObject())
                                {
                                    var name = p.Name.ToLowerInvariant();
                                    if (p.Value.ValueKind == System.Text.Json.JsonValueKind.String
                                        || p.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    {
                                        var s = p.Value.ToString();
                                        if (name is "titular" or "nombre" or "name" or "cliente")
                                            titular = s;
                                        else if (name is "documento" or "ruc" or "ci" or "nro_documento")
                                            documento = s;
                                        else if (name.Contains("monto") || name.Contains("importe") || name.Contains("total") || name == "gs")
                                        {
                                            if (decimal.TryParse(LimpiarMonto(s), System.Globalization.NumberStyles.Any,
                                                    System.Globalization.CultureInfo.InvariantCulture, out var m) && m > 0)
                                                monto = m;
                                        }
                                        else if (name.Contains("estado") || name.Contains("situacion"))
                                            estado = s;
                                        else if (name.Contains("emision") || name.Contains("fecha_emi"))
                                            emision = s;
                                        else if (name.Contains("venc"))
                                            venc = s;
                                    }
                                    else
                                        Walk(p.Value, path + "." + p.Name);
                                }
                                if (monto is > 0 && (estado.Contains("Pagado", StringComparison.OrdinalIgnoreCase)
                                    || estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)
                                    || !string.IsNullOrEmpty(emision) || !string.IsNullOrEmpty(venc)))
                                {
                                    if (string.IsNullOrEmpty(estado)) estado = "Pendiente de Pago";
                                    parsed.Facturas.Add(new AndeFacturaItemDto
                                    {
                                        Monto = monto.Value,
                                        Estado = estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase) ? "Pendiente de Pago"
                                               : estado.Contains("Pagado", StringComparison.OrdinalIgnoreCase) ? "Pagado" : estado,
                                        Emision = emision,
                                        Vencimiento = venc
                                    });
                                }
                                if (!string.IsNullOrWhiteSpace(titular) && string.IsNullOrWhiteSpace(parsed.Titular))
                                    parsed.Titular = titular;
                                if (!string.IsNullOrWhiteSpace(documento) && string.IsNullOrWhiteSpace(parsed.Documento))
                                    parsed.Documento = documento;
                                break;
                            case System.Text.Json.JsonValueKind.Array:
                                foreach (var item in el.EnumerateArray()) Walk(item, path);
                                break;
                        }
                    }
                    Walk(doc.RootElement, "$");
                    if (parsed.Facturas.Count > (best?.Facturas.Count ?? 0))
                    {
                        parsed.Facturas = parsed.Facturas
                            .GroupBy(f => $"{f.Monto}|{f.Estado}|{f.Emision}|{f.Vencimiento}")
                            .Select(g => g.First()).ToList();
                        parsed.SaldoDeuda = parsed.Facturas.Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)).Sum(f => f.Monto);
                        parsed.FacturasPendientes = parsed.Facturas.Count(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase));
                        parsed.Vencimiento = parsed.Facturas.FirstOrDefault(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))?.Vencimiento
                            ?? parsed.Facturas.FirstOrDefault()?.Vencimiento ?? "";
                        if (string.IsNullOrWhiteSpace(parsed.Titular)) parsed.Titular = "TITULAR ANDE";
                        parsed.EsSimulado = false;
                        best = parsed;
                    }
                }
                catch { /* next body */ }
            }
            return best;
        }

        public async Task<(bool Ok, string Mensaje, bool YaEstabaLogueado)> AsegurarSesionAsync()
        {
            var documento = _creds.GetDocumento();
            var password = _creds.GetPassword();
            var tipoDoc = _config["AndeConfig:TipoDocumento"] ?? "C.I. Civil";
            var portalUrl = _config["AndeConfig:PortalUrl"] ?? "https://www.ande.gov.py/servicios/";

            await _portalLock.WaitAsync();
            try
            {
                _logger.LogInformation("ANDE: asegurando sesión (login Mi Cuenta)...");
                var page = await EnsurePortalPageAsync(headless: false);

                try
                {
                    await page.GoToAsync(portalUrl, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                        Timeout = 90000
                    });
                    await Task.Delay(2500);
                }
                catch (Exception exNav)
                {
                    _logger.LogWarning("ANDE: navegación portal falló ({Msg})", exNav.Message);
                }

                if (await PortalEstaLogueadoAsync(page))
                {
                    _sesionAndeActiva = true;
                    await MinimizeChromeWindowAsync(page);
                    _logger.LogInformation("ANDE: sesión ya activa — Chrome minimizado.");
                    return (true, "Sesión ANDE ya activa. Puede consultar el NIS.", true);
                }

                await RestoreChromeWindowAsync(page);

                if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Faltan credenciales ANDE (AndeConfig:Documento / Password).", false);
                }

                var formOk = await AbrirFormularioLoginAsync(page, portalUrl);
                if (!formOk)
                    return (false, "No se pudo abrir el formulario Mi Cuenta de ANDE.", false);

                await Task.Delay(1500);
                var filled = await AutocompletarLoginConTecladoAsync(page, tipoDoc, documento, password);
                if (!filled)
                    return (false, "No se pudieron cargar CI/contraseña en el formulario ANDE.", false);

                var esperaCfg = int.TryParse(_config["AndeConfig:EsperaLoginSegundos"], out var el) ? el : 300;
                var esperaLoginSeg = Math.Max(esperaCfg, 180);
                _logger.LogInformation("ANDE: pantalla login lista — pulse Acceder (hasta {Seg}s)...", esperaLoginSeg);

                var deadline = DateTime.UtcNow.AddSeconds(esperaLoginSeg);
                while (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(2000);
                    if (await PortalEstaLogueadoAsync(page))
                    {
                        _sesionAndeActiva = true;
                        await MinimizeChromeWindowAsync(page);
                        _logger.LogInformation("ANDE: Acceder OK — Chrome minimizado, listo para modal NIS.");
                        return (true, "Login ANDE OK. Ya puede consultar el NIS.", false);
                    }
                }

                _sesionAndeActiva = false;
                return (false, "No se detectó Acceder a tiempo. Complete captcha, pulse Acceder e intente de nuevo.", false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ANDE: error asegurando sesión");
                return (false, $"Error abriendo login ANDE: {ex.Message}", false);
            }
            finally
            {
                _portalLock.Release();
            }
        }

        private async Task<AndeConsultaResultadoDto?> ConsultarViaPortalMiAndeAsync(string nisLimpio)
        {
            var portalUrl = _config["AndeConfig:PortalUrl"] ?? "https://www.ande.gov.py/servicios/";
            // Consulta NIS: asume sesión ya asegurada (botón Consultar Saldos). Si caducó, reabre login.
            var headless = false;

            await _portalLock.WaitAsync();
            try
            {
                _logger.LogInformation("Consultando ANDE portal para NIS {Nis}", nisLimpio);
                var page = await EnsurePortalPageAsync(headless);
                var xhrBodies = new System.Collections.Concurrent.ConcurrentBag<string>();
                void OnResponse(object? _, ResponseCreatedEventArgs e)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var url = (e.Response.Url ?? "").ToLowerInvariant();
                            if (!(url.Contains("factura") || url.Contains("comprobante") || url.Contains("nis")
                                  || url.Contains("consulta") || url.Contains("suministro") || url.Contains("ajax")
                                  || url.Contains("api") || url.Contains("servicio")))
                                return;
                            if (!e.Response.Ok) return;
                            var text = await e.Response.TextAsync();
                            if (!string.IsNullOrWhiteSpace(text) && text.Length is > 40 and < 2_000_000)
                                xhrBodies.Add(text);
                        }
                        catch { /* ignore body read errors */ }
                    });
                }
                page.Response += OnResponse;

                try
                {
                // Si ya hubo Acceder OK: no recargar de golpe ni pedir login otra vez.
                // Solo asegurar que la sesión esté legible y consultar el NIS.
                var urlActual = (page.Url ?? "").ToLowerInvariant();
                var yaEnAnde = urlActual.Contains("ande.gov.py");

                if (!_sesionAndeActiva || !yaEnAnde)
                {
                    try
                    {
                        await page.GoToAsync(portalUrl, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                            Timeout = 90000
                        });
                        await Task.Delay(2500);
                    }
                    catch (Exception exNav)
                    {
                        _logger.LogWarning("ANDE: navegacion inicial fallo ({Msg})", exNav.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("ANDE: reutilizando página con sesión previa (sin segundo login).");
                    await Task.Delay(800);
                }

                var yaLogueado = await EsperarSesionPortalAsync(page, segundosMax: _sesionAndeActiva ? 25 : 8);
                if (!yaLogueado && _sesionAndeActiva)
                {
                    // La sesión del Acceder sigue vigente en memoria/proceso: refrescar home una vez y reintentar.
                    _logger.LogInformation("ANDE: sesión marcada activa; refrescando portal sin pedir Acceder...");
                    try
                    {
                        await page.GoToAsync(portalUrl, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                            Timeout = 90000
                        });
                    }
                    catch { /* ignore */ }
                    yaLogueado = await EsperarSesionPortalAsync(page, segundosMax: 30);
                }

                if (!yaLogueado)
                {
                    // Solo si realmente no hay sesión: pedir Acceder (no debería pasar tras AsegurarSesion).
                    _logger.LogWarning("ANDE: sin sesión real al consultar NIS — se requiere Acceder de nuevo.");
                    _sesionAndeActiva = false;
                    var documento = _creds.GetDocumento();
                    var password = _creds.GetPassword();
                    var tipoDoc = _config["AndeConfig:TipoDocumento"] ?? "C.I. Civil";
                    await RestoreChromeWindowAsync(page);
                    if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(password))
                        return null;
                    if (!await AbrirFormularioLoginAsync(page, portalUrl))
                        return null;
                    await Task.Delay(1000);
                    if (!await AutocompletarLoginConTecladoAsync(page, tipoDoc, documento, password))
                        return null;
                    var esperaCfg = int.TryParse(_config["AndeConfig:EsperaLoginSegundos"], out var el) ? el : 300;
                    var deadline = DateTime.UtcNow.AddSeconds(Math.Max(esperaCfg, 180));
                    while (DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(2000);
                        if (await PortalEstaLogueadoAsync(page))
                        {
                            _sesionAndeActiva = true;
                            await MinimizeChromeWindowAsync(page);
                            yaLogueado = true;
                            break;
                        }
                    }
                    if (!yaLogueado) return null;
                }
                else
                {
                    _sesionAndeActiva = true;
                    await MinimizeChromeWindowAsync(page);
                }

                // Consulta de facturas con sesion
                try
                {
                    await page.EvaluateFunctionAsync(@"() => {
                        try { if (typeof cambiarContainer==='function') cambiarContainer('factura_publico'); } catch(e) {}
                        const el = document.querySelector(""a[onclick*='factura_publico']"");
                        if (el) el.click();
                    }");
                }
                catch { /* ignore */ }
                await Task.Delay(2000);
                await CompletarNisYConsultarAsync(page, nisLimpio);
                await Task.Delay(1500);
                try
                {
                    await page.EvaluateFunctionAsync(@"() => {
                      try {
                        if (window.facturaPublicoComponent && typeof facturaPublicoComponent.consultar==='function')
                          facturaPublicoComponent.consultar();
                      } catch(e) {}
                    }");
                }
                catch { /* ignore */ }

                var auth = await ExtraerResultadoDesdePaginaAsync(page, nisLimpio)
                           ?? ParseFromCapturedXhr(xhrBodies, nisLimpio);
                if (auth != null && auth.Facturas.Count > 0)
                {
                    auth.Observaciones = "Montos con sesion ANDE activa (un solo Acceder).";
                    _logger.LogInformation(
                        "ANDE OK NIS {Nis}: titular={Titular}, saldo={Saldo}, facturas={Count}",
                        nisLimpio, auth.Titular, auth.SaldoDeuda, auth.Facturas.Count);
                    return auth;
                }

                _logger.LogWarning("ANDE: con sesion pero sin facturas parseables. XHR={Count}", xhrBodies.Count);
                return null;
                }
                finally
                {
                    page.Response -= OnResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE portal fallo ({Msg}).", ex.Message);
                try
                {
                    if (_sharedBrowser != null && _sharedBrowser.IsClosed)
                    {
                        _sharedBrowser = null;
                        _sharedPage = null;
                        _sesionAndeActiva = false;
                    }
                }
                catch { /* ignore */ }
                return null;
            }
            finally
            {
                _portalLock.Release();
            }
        }

        private static bool? _sharedHeadless;

        private async Task<IPage> EnsurePortalPageAsync(bool headless)
        {
            _profileDir ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Gesco", "ande-chrome-profile");
            Directory.CreateDirectory(_profileDir);

            var browserAlive = _sharedBrowser != null && !_sharedBrowser.IsClosed;
            if (browserAlive && _sharedHeadless != headless)
            {
                try { await _sharedBrowser!.CloseAsync(); } catch { /* ignore */ }
                _sharedBrowser = null;
                _sharedPage = null;
                _sesionAndeActiva = false;
                browserAlive = false;
            }

            if (!browserAlive)
            {
                _sharedBrowser = await AndeChromeLauncher.LaunchAsync(_profileDir, headless, _logger);
                _sharedHeadless = headless;
                _sharedPage = null;
                _sesionAndeActiva = false;
                // Si usó perfil temp, recordar el path no es crítico; el dir preferido queda para la próxima.
            }

            if (_sharedPage == null || _sharedPage.IsClosed)
            {
                var pages = await _sharedBrowser!.PagesAsync();
                _sharedPage = pages.FirstOrDefault() ?? await _sharedBrowser.NewPageAsync();
                await _sharedPage.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                try
                {
                    await _sharedPage.EvaluateFunctionOnNewDocumentAsync(@"() => {
                        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                    }");
                }
                catch { /* ignore */ }
            }

            return _sharedPage!;
        }

        private async Task MinimizeChromeWindowAsync(IPage page)
        {
            try
            {
                var client = await page.Target.CreateCDPSessionAsync();
                var win = await client.SendAsync<System.Text.Json.JsonElement>("Browser.getWindowForTarget");
                if (!win.TryGetProperty("windowId", out var idEl)) return;
                var windowId = idEl.GetInt32();
                await client.SendAsync("Browser.setWindowBounds", new Dictionary<string, object>
                {
                    ["windowId"] = windowId,
                    ["bounds"] = new Dictionary<string, object> { ["windowState"] = "minimized" }
                });
                _logger.LogInformation("ANDE: ventana Chrome minimizada (proceso sigue activo para consultas NIS)");
            }
            catch (Exception ex)
            {
                _logger.LogDebug("ANDE: no se pudo minimizar Chrome ({Msg})", ex.Message);
            }
        }

        private async Task RestoreChromeWindowAsync(IPage page)
        {
            try
            {
                var client = await page.Target.CreateCDPSessionAsync();
                var win = await client.SendAsync<System.Text.Json.JsonElement>("Browser.getWindowForTarget");
                if (!win.TryGetProperty("windowId", out var idEl)) return;
                var windowId = idEl.GetInt32();
                await client.SendAsync("Browser.setWindowBounds", new Dictionary<string, object>
                {
                    ["windowId"] = windowId,
                    ["bounds"] = new Dictionary<string, object> { ["windowState"] = "normal" }
                });
            }
            catch { /* ignore */ }
        }

        private static async Task<bool> PortalEstaLogueadoAsync(IPage page)
        {
            try
            {
                return await page.EvaluateFunctionAsync<bool>(@"() => {
                    const tieneToken = () => {
                      try {
                        const keys = Object.keys(localStorage || {});
                        for (const k of keys) {
                          if (!/kwfSesion|sesionUsuario|miCuenta/i.test(k)) continue;
                          try {
                            const raw = localStorage.getItem(k) || '';
                            if (!raw || raw === 'null') continue;
                            const o = JSON.parse(raw);
                            if (!o) continue;
                            if (o.token || o.JWTtoken || o.jwt || o.JwtToken) return true;
                            if (o.resultado && (o.resultado.nombre || o.resultado.apellido || o.resultado.suministrosList)) return true;
                          } catch (e) {}
                        }
                      } catch (e) {}
                      return false;
                    };
                    if (tieneToken()) return true;

                    const txt = ((document.body && document.body.innerText) || '');
                    const n = txt.normalize('NFD').replace(/[\u0300-\u036f]/g,'');
                    const title = ((document.title || '') + '').toLowerCase();
                    if (title.includes('radware')) return false;

                    const hasPasswordVisible = Array.from(document.querySelectorAll('input[type=password]'))
                      .some(i => !!(i.offsetParent || (i.getClientRects && i.getClientRects().length)));
                    if (/Mis Datos/i.test(n) && !hasPasswordVisible) return true;
                    if (/Mis Suministros/i.test(n) && !hasPasswordVisible) return true;
                    if (/Ultimos Comprobantes/i.test(n)) return true;
                    if (/Cerrar sesion|Salir/i.test(n) && !hasPasswordVisible) return true;
                    return false;
                }");
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> EsperarSesionPortalAsync(IPage page, int segundosMax)
        {
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(2, segundosMax));
            while (DateTime.UtcNow < deadline)
            {
                if (await PortalEstaLogueadoAsync(page))
                    return true;
                await Task.Delay(1000);
            }
            return await PortalEstaLogueadoAsync(page);
        }

        private async Task<bool> AbrirFormularioLoginAsync(IPage page, string portalUrl)
        {
            async Task<bool> TienePasswordAsync()
            {
                try { return await page.EvaluateFunctionAsync<bool>("() => !!document.querySelector('input[type=password]')"); }
                catch { return false; }
            }

            // Entrar siempre por la home de servicios (sin ?op=1). Luego abrir Mi Cuenta desde el menú.
            try
            {
                var urlActual = (page.Url ?? "").ToLowerInvariant();
                var yaEnPortal = urlActual.Contains("ande.gov.py") && !urlActual.Contains("op=1");
                if (!yaEnPortal)
                {
                    await page.GoToAsync(portalUrl.TrimEnd('/') + "/", new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                        Timeout = 90000
                    });
                    await Task.Delay(3000);
                }
                else
                {
                    await Task.Delay(800);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE portal: fallo cargando home servicios ({Msg})", ex.Message);
                return false;
            }

            if (await TienePasswordAsync())
            {
                _logger.LogInformation("ANDE portal: formulario login ya visible en home");
                return true;
            }

            // Preferir menú "Mi Cuenta" (flujo natural del portal)
            var clicks = new[] { "Mi Cuenta", "Acceder", "Ingresar", "Iniciar sesion", "Iniciar sesión" };
            foreach (var texto in clicks)
            {
                try
                {
                    var clicked = await page.EvaluateFunctionAsync<bool>(@"(texto) => {
                        const norm = (s) => (s||'').toString().normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().trim();
                        const target = norm(texto);
                        const els = Array.from(document.querySelectorAll('a,button,span,div,li,p,label,[onclick]'));
                        const el = els.find(e => {
                          const t = norm(e.textContent);
                          return t === target || (t.includes(target) && t.length < 48);
                        });
                        if (!el) return false;
                        el.scrollIntoView({block:'center'});
                        el.click();
                        return true;
                      }", texto);
                    await Task.Delay(2800);
                    if (await TienePasswordAsync())
                    {
                        _logger.LogInformation("ANDE portal: login visible tras click '{Texto}' (sin ?op=1)", texto);
                        return true;
                    }
                    _logger.LogInformation("ANDE portal: click '{Texto}' => {Ok}", texto, clicked);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("ANDE portal: click {Texto} fallo ({Msg})", texto, ex.Message);
                }
            }

            // Último recurso: contenedores conocidos del portal (sin navegar a ?op=1)
            try
            {
                var viaContainer = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const candidates = ['mi_cuenta','micuenta','login','acceso','cuenta'];
                    for (const c of candidates) {
                      try {
                        if (typeof cambiarContainer === 'function') {
                          cambiarContainer(c);
                          return true;
                        }
                      } catch (e) {}
                    }
                    return false;
                  }");
                if (viaContainer)
                {
                    await Task.Delay(3000);
                    if (await TienePasswordAsync())
                    {
                        _logger.LogInformation("ANDE portal: login vía cambiarContainer");
                        return true;
                    }
                }
            }
            catch { /* ignore */ }

            try
            {
                var botones = await page.EvaluateFunctionAsync<string>(@"() => {
                    return Array.from(document.querySelectorAll('a,button'))
                      .map(e => (e.textContent||'').trim().replace(/\s+/g,' ').slice(0,40))
                      .filter(Boolean)
                      .slice(0, 25)
                      .join(' | ');
                  }");
                _logger.LogWarning("ANDE portal: no se vio login tras Mi Cuenta. Enlaces: {Botones}", botones);
            }
            catch { /* ignore */ }

            return await TienePasswordAsync();
        }

        private async Task<bool> AutocompletarLoginConTecladoAsync(IPage page, string tipoDoc, string documento, string password)
        {
            // Preferir seteo por JS: en ANDE muchos inputs no son "clickables" para Puppeteer
            try
            {
                var filledJs = await page.EvaluateFunctionAsync<bool>(@"(tipoDoc, documento, password) => {
                    const norm = (s) => (s||'').toString().normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().trim();
                    const setNative = (el, value) => {
                      if (!el) return false;
                      try { el.scrollIntoView({ block: 'center', inline: 'nearest' }); } catch (e) {}
                      const proto = window.HTMLInputElement.prototype;
                      const setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
                      try { el.focus(); } catch (e) {}
                      if (setter) setter.call(el, value); else el.value = value;
                      el.dispatchEvent(new Event('input', { bubbles: true }));
                      el.dispatchEvent(new Event('change', { bubbles: true }));
                      el.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
                      el.dispatchEvent(new Event('blur', { bubbles: true }));
                      return (el.value || '') === value;
                    };

                    for (const sel of Array.from(document.querySelectorAll('select'))) {
                      const match = Array.from(sel.options||[]).find(o => {
                        const t = norm(o.textContent);
                        return t.includes('c.i') || t.includes('ci civil') || t.includes('cedula') || t === norm(tipoDoc);
                      });
                      if (match) {
                        sel.value = match.value;
                        sel.dispatchEvent(new Event('input', { bubbles: true }));
                        sel.dispatchEvent(new Event('change', { bubbles: true }));
                        break;
                      }
                    }

                    const inputs = Array.from(document.querySelectorAll('input'));
                    const passInput = inputs.find(i => i.type === 'password');
                    let docInput = inputs.find(i => {
                      if (!passInput || i === passInput || i.type === 'password' || i.type === 'hidden' || i.type === 'checkbox' || i.type === 'radio') return false;
                      const tip = norm((i.placeholder||'')+' '+(i.name||'')+' '+(i.id||'')+' '+(i.getAttribute('aria-label')||''));
                      return tip.includes('ci') || tip.includes('ruc') || tip.includes('pasaporte') || tip.includes('documento') || tip.includes('cedula') || tip.includes('numero');
                    });
                    if (!docInput) {
                      docInput = inputs.find(i => i !== passInput && (i.type === 'text' || i.type === 'tel' || i.type === 'number' || !i.type));
                    }

                    const okDoc = setNative(docInput, documento);
                    const okPass = setNative(passInput, password);
                    if (docInput) docInput.setAttribute('data-gesco', 'ande-doc');
                    if (passInput) passInput.setAttribute('data-gesco', 'ande-pass');
                    return !!(okDoc && okPass);
                  }", tipoDoc, documento, password);

                if (filledJs) return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE portal: autocompletar JS fallo ({Msg})", ex.Message);
            }

            // Fallback teclado (solo si el nodo es interactuable)
            try
            {
                var hasMarked = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const mark = (el, id) => { if (el) el.setAttribute('data-gesco', id); };
                    const inputs = Array.from(document.querySelectorAll('input'));
                    const pass = inputs.find(i => i.type === 'password');
                    const doc = inputs.find(i => i !== pass && i.type !== 'password' && i.type !== 'hidden' && (i.type === 'text' || i.type === 'tel' || i.type === 'number' || !i.type));
                    mark(doc, 'ande-doc');
                    mark(pass, 'ande-pass');
                    return !!(doc && pass);
                  }");
                if (!hasMarked) return false;

                async Task TryTypeAsync(string sel, string value)
                {
                    try
                    {
                        await page.WaitForSelectorAsync(sel, new WaitForSelectorOptions { Visible = false, Timeout = 5000 });
                        await page.EvaluateFunctionAsync(@"(sel) => {
                            const el = document.querySelector(sel);
                            if (el) { el.style.opacity='1'; el.style.display='block'; el.removeAttribute('readonly'); el.removeAttribute('disabled'); }
                          }", sel);
                        await page.FocusAsync(sel);
                        await page.Keyboard.DownAsync("Control");
                        await page.Keyboard.PressAsync("KeyA");
                        await page.Keyboard.UpAsync("Control");
                        await page.Keyboard.TypeAsync(value, new PuppeteerSharp.Input.TypeOptions { Delay = 20 });
                    }
                    catch
                    {
                        // Ãšltimo recurso: value directo
                        await page.EvaluateFunctionAsync(@"(sel, value) => {
                            const el = document.querySelector(sel);
                            if (!el) return;
                            const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set;
                            if (setter) setter.call(el, value); else el.value = value;
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                            el.dispatchEvent(new Event('change', { bubbles: true }));
                          }", sel, value);
                    }
                }

                await TryTypeAsync("[data-gesco='ande-doc']", documento);
                await TryTypeAsync("[data-gesco='ande-pass']", password);

                return await page.EvaluateFunctionAsync<bool>(@"() => {
                    const doc = document.querySelector('[data-gesco=""ande-doc""]');
                    const pass = document.querySelector('[data-gesco=""ande-pass""]');
                    return !!(doc && pass && doc.value && pass.value);
                  }");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ANDE portal: autocompletar teclado fallo ({Msg})", ex.Message);
                return false;
            }
        }

        private async Task<AndeConsultaResultadoDto?> ConsultarViaEndpointLegacyAsync(string nisLimpio)
        {
            string url = _config["AndeConfig:ConsultaScrapingUrl"]
                ?? "http://201.217.43.238:9080/consulta/consulta_02.php";
            string origin = _config["AndeConfig:HeadersOrigin"]
                ?? "http://201.217.43.238:9080";

            Exception? lastError = null;
            for (int intento = 1; intento <= 2; intento++)
            {
                try
                {
                    _logger.LogInformation("Consultando ANDE legacy para NIS: {Nis} via {Url} (intento {Intento})", nisLimpio, url, intento);

                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.TryAddWithoutValidation("Origin", origin);
                    request.Headers.TryAddWithoutValidation("Referer", origin + "/");
                    request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("name", nisLimpio)
                    });

                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(80));
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    var body = await response.Content.ReadAsStringAsync(cts.Token);

                    _logger.LogInformation("ANDE legacy respondiÃ³ HTTP {Status} ({Len} chars) para NIS {Nis}",
                        (int)response.StatusCode, body?.Length ?? 0, nisLimpio);

                    if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
                    {
                        lastError = new Exception($"HTTP {(int)response.StatusCode}");
                        continue;
                    }

                    var parsed = ParseHtmlConsultaAnde(body, nisLimpio);
                    if (parsed != null && (parsed.Facturas.Count > 0 || parsed.SaldoDeuda > 0 || !string.IsNullOrWhiteSpace(parsed.Titular)))
                    {
                        parsed.EsSimulado = false;
                        _logger.LogInformation(
                            "ANDE legacy parse OK NIS {Nis}: titular={Titular}, saldo={Saldo}, facturas={Count}",
                            nisLimpio, parsed.Titular, parsed.SaldoDeuda, parsed.Facturas.Count);
                        return parsed;
                    }

                    _logger.LogWarning("ANDE legacy respondiÃ³ pero no se pudieron parsear facturas para NIS {Nis}. Preview: {Preview}",
                        nisLimpio, body.Length > 400 ? body[..400] : body);
                    return null;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogWarning("Consulta HTTP ANDE legacy fallÃ³ intento {Intento} ({Msg}).", intento, ex.Message);
                    if (intento < 2)
                        await Task.Delay(1500);
                }
            }

            if (lastError != null)
                _logger.LogWarning("Consulta ANDE legacy agotÃ³ reintentos: {Msg}", lastError.Message);

            return null;
        }

        private static AndeConsultaResultadoDto? ParseHtmlConsultaAnde(string html, string nis)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            // Limpiar ruido tÃ­pico del HTML ANDE
            var clean = System.Text.RegularExpressions.Regex.Replace(html, @"<br\s*/?>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"&nbsp;?", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ");

            var dto = new AndeConsultaResultadoDto { Nis = nis };

            // Titular / nombre (formato portal Mi ANDE)
            var titularMatch =
                System.Text.RegularExpressions.Regex.Match(clean, @"(?:Name|Nombre|Titular|Cliente)\s*:?\s*([A-Za-z][A-Za-z\s\.]{3,80})", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                is { Success: true } m1 ? m1 :
                System.Text.RegularExpressions.Regex.Match(clean, @"<b[^>]*>\s*([A-Z][A-Z\s\.]{5,80})\s*</b>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (titularMatch.Success)
                dto.Titular = titularMatch.Groups[1].Value.Trim();

            // Documento / RUC / CI (portal muestra "RUC/CI: 709545")
            var docMatch = System.Text.RegularExpressions.Regex.Match(clean,
                @"(?:RUC\s*/\s*CI|RUC|C\.?I\.?|Documento|Cedula)\s*:?\s*(\d{4,12}(?:-\d)?)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (docMatch.Success)
                dto.Documento = docMatch.Groups[1].Value.Trim();

            // Extraer bloques/filas con monto + estado + fechas
            var facturas = new List<AndeFacturaItemDto>();

            // Formato portal / app: Gs. 594.000 + Pagado/Pendiente + Emision + Vencimiento
            var cardRegex = new System.Text.RegularExpressions.Regex(
                @"Gs\.?\s*(\d{1,3}(?:\.\d{3})+|\d{4,9})\s*(?:</?[^>]+>\s*)*(Pendiente(?:\s+de\s+Pago)?|Pagado|Paga(?:da)?|Cancelado)\s*(?:</?[^>]+>\s*)*(?:Emisi[oó]n\s*:?\s*)?(\d{1,2}/\d{1,2}/\d{2,4})?\s*(?:</?[^>]+>\s*)*(?:[-–]?\s*)?(?:Vencimiento\s*:?\s*)?(\d{1,2}/\d{1,2}/\d{2,4})?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in cardRegex.Matches(clean))
            {
                if (!decimal.TryParse(LimpiarMonto(match.Groups[1].Value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
                    continue;

                var estadoRaw = match.Groups[2].Value.Trim();
                var estado = estadoRaw.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)
                    ? "Pendiente de Pago"
                    : "Pagado";

                facturas.Add(new AndeFacturaItemDto
                {
                    Monto = monto,
                    Estado = estado,
                    Emision = match.Groups[3].Success ? match.Groups[3].Value : "",
                    Vencimiento = match.Groups[4].Success ? match.Groups[4].Value : ""
                });
            }

            // PatrÃ³n alterno sin prefijo Gs.
            if (facturas.Count == 0)
            {
                var rowRegex = new System.Text.RegularExpressions.Regex(
                    @"(?:Gs\.?\s*)?(\d{1,3}(?:\.\d{3})+|\d{4,9})\s*(?:</?[^>]+>\s*)*(Pendiente(?:\s+de\s+Pago)?|Pagado|Paga(?:da)?|Cancelado)\s*(?:</?[^>]+>\s*)*(?:Emisi[oó]n\s*:?\s*)?(\d{1,2}/\d{1,2}/\d{2,4})?\s*(?:</?[^>]+>\s*)*(?:Vencimiento\s*:?\s*)?(\d{1,2}/\d{1,2}/\d{2,4})?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match match in rowRegex.Matches(clean))
                {
                    if (!decimal.TryParse(LimpiarMonto(match.Groups[1].Value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
                        continue;

                    var estadoRaw = match.Groups[2].Value.Trim();
                    var estado = estadoRaw.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)
                        ? "Pendiente de Pago"
                        : "Pagado";

                    facturas.Add(new AndeFacturaItemDto
                    {
                        Monto = monto,
                        Estado = estado,
                        Emision = match.Groups[3].Success ? match.Groups[3].Value : "",
                        Vencimiento = match.Groups[4].Success ? match.Groups[4].Value : ""
                    });
                }
            }

            // Fallback: filas de tabla <td>
            if (facturas.Count == 0)
            {
                var tdRows = System.Text.RegularExpressions.Regex.Matches(
                    clean,
                    @"<tr[^>]*>(.*?)</tr>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                foreach (System.Text.RegularExpressions.Match row in tdRows)
                {
                    var cells = System.Text.RegularExpressions.Regex.Matches(row.Groups[1].Value, @"<t[dh][^>]*>(.*?)</t[dh]>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline)
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(c => System.Text.RegularExpressions.Regex.Replace(c.Groups[1].Value, "<.*?>", "").Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();

                    if (cells.Count < 2) continue;

                    decimal monto = 0;
                    string estado = "";
                    string emision = "";
                    string venc = "";

                    foreach (var cell in cells)
                    {
                        if (monto == 0 && System.Text.RegularExpressions.Regex.IsMatch(cell, @"^\d{1,3}(\.\d{3})+$|^\d{4,9}$"))
                            decimal.TryParse(LimpiarMonto(cell), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out monto);
                        else if (string.IsNullOrEmpty(estado) && System.Text.RegularExpressions.Regex.IsMatch(cell, @"pendiente|pagad", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            estado = cell.Contains("Pendiente", StringComparison.OrdinalIgnoreCase) ? "Pendiente de Pago" : "Pagado";
                        else if (System.Text.RegularExpressions.Regex.IsMatch(cell, @"^\d{1,2}/\d{1,2}/\d{2,4}$"))
                        {
                            if (string.IsNullOrEmpty(emision)) emision = cell;
                            else if (string.IsNullOrEmpty(venc)) venc = cell;
                        }
                    }

                    if (monto > 0 && !string.IsNullOrEmpty(estado))
                    {
                        facturas.Add(new AndeFacturaItemDto
                        {
                            Monto = monto,
                            Estado = estado,
                            Emision = emision,
                            Vencimiento = venc
                        });
                    }
                }
            }

            // Fallback final: sumar montos "Pendiente"
            if (facturas.Count == 0)
            {
                var pendientes = System.Text.RegularExpressions.Regex.Matches(
                    clean,
                    @"(\d{1,3}(?:\.\d{3})+|\d{4,9})[^\d]{0,40}Pendiente",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match p in pendientes)
                {
                    if (decimal.TryParse(LimpiarMonto(p.Groups[1].Value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) && monto > 0)
                    {
                        facturas.Add(new AndeFacturaItemDto
                        {
                            Monto = monto,
                            Estado = "Pendiente de Pago"
                        });
                    }
                }
            }

            // Fallback: etiquetas Importe / Total / Saldo
            if (facturas.Count == 0)
            {
                var labeled = System.Text.RegularExpressions.Regex.Matches(
                    clean,
                    @"(?:Importe|Monto|Total(?:\s+a\s+Pagar)?|Saldo(?:\s+Deudor)?|Deuda)\s*:?\s*(?:Gs\.?\s*)?(\d{1,3}(?:\.\d{3})+|\d{4,9})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match p in labeled)
                {
                    if (decimal.TryParse(LimpiarMonto(p.Groups[1].Value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) && monto > 0)
                    {
                        facturas.Add(new AndeFacturaItemDto
                        {
                            Monto = monto,
                            Estado = "Pendiente de Pago"
                        });
                    }
                }
            }

            // Deduplicar por monto+estado+emision
            dto.Facturas = facturas
                .GroupBy(f => $"{f.Monto}|{f.Estado}|{f.Emision}|{f.Vencimiento}")
                .Select(g => g.First())
                .OrderByDescending(f => f.Estado.StartsWith("Pendiente", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.Emision)
                .ToList();

            // Si solo hay un monto etiquetado como total, usarlo como saldo aunque no haya filas
            if (dto.Facturas.Count == 0)
            {
                var sole = System.Text.RegularExpressions.Regex.Match(
                    clean,
                    @"(?:Total|Saldo|Deuda)[^\d]{0,25}(\d{1,3}(?:\.\d{3})+|\d{4,9})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (sole.Success &&
                    decimal.TryParse(LimpiarMonto(sole.Groups[1].Value), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var soleMonto) &&
                    soleMonto > 0)
                {
                    dto.Facturas.Add(new AndeFacturaItemDto
                    {
                        Monto = soleMonto,
                        Estado = "Pendiente de Pago"
                    });
                }
            }

            dto.SaldoDeuda = dto.Facturas.Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase)).Sum(f => f.Monto);
            dto.FacturasPendientes = dto.Facturas.Count(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase));
            dto.Vencimiento = dto.Facturas.FirstOrDefault(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))?.Vencimiento
                ?? dto.Facturas.FirstOrDefault()?.Vencimiento
                ?? "";

            // Consumo kW si aparece
            var kwMatch = System.Text.RegularExpressions.Regex.Match(clean, @"(\d{2,5})\s*k\s*w\s*h?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (kwMatch.Success && int.TryParse(kwMatch.Groups[1].Value, out var kw))
                dto.ConsumoKw = kw;

            if (string.IsNullOrWhiteSpace(dto.Titular))
                dto.Titular = "TITULAR ANDE";

            return dto.Facturas.Count > 0 || dto.SaldoDeuda > 0 ? dto : null;
        }

        private static string LimpiarMonto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return "0";
            var v = valor.Trim()
                .Replace("Gs.", "", StringComparison.OrdinalIgnoreCase)
                .Replace("GS.", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "");

            // Formato PY: 594.000 -> 594000
            if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^\d{1,3}(\.\d{3})+$"))
                return v.Replace(".", "");

            if (v.Contains('.') && v.Contains(','))
                v = v.Replace(".", "").Replace(",", ".");
            else if (v.Count(c => c == '.') > 1)
                v = v.Replace(".", "");

            return v;
        }

        private const string AndeRecaptchaSiteKey = "6LcND9ohAAAAALizDIFfkfpSjHm3Xtr0DwLOLc60";

        public async Task<byte[]?> DescargarFacturaPdfOficialPortalAsync(
            string nis,
            string? pdfUrlCompleta = null,
            string? secNis = null,
            string? secRec = null,
            string? fFact = null,
            bool electronica = true,
            string? kwfxToken = null,
            string? origen = null)
        {
            var nisLimpio = new string((nis ?? "").Where(char.IsDigit).ToArray());
            if (nisLimpio.Length == 0) return null;

            const string openBase = "https://prod2.ande.gov.py:8581/sigaWs/api/open/";
            string url;
            if (!string.IsNullOrWhiteSpace(pdfUrlCompleta))
            {
                url = pdfUrlCompleta;
            }
            else if (!string.IsNullOrWhiteSpace(secNis) && !string.IsNullOrWhiteSpace(secRec) && !string.IsNullOrWhiteSpace(fFact))
            {
                var f = fFact.Trim();
                if (f.Contains('-') && f.Length >= 8)
                {
                    var p = f.Split('-', '/', '.');
                    if (p.Length == 3 && p[0].Length == 4)
                        f = $"{p[2].PadLeft(2, '0')}/{p[1].PadLeft(2, '0')}/{p[0]}";
                }
                var qs = $"nro_nis={nisLimpio}&sec_nis={secNis}&sec_rec={secRec}&f_fact={f}";
                url = openBase + (electronica ? "v5/suministro/facturaElectronicaPdf?" : "v4/suministro/facturaPdf?") + qs;
            }
            else if (string.Equals(origen, "penultima", StringComparison.OrdinalIgnoreCase))
            {
                url = openBase + $"v5/suministro/penultimaFacturaElectronicaPendientePdf?nis={nisLimpio}";
            }
            else
            {
                url = openBase + $"v5/suministro/ultimaFacturaElectronicaPendientePdf?nis={nisLimpio}";
            }

            if (!url.Contains("clientKey=", StringComparison.OrdinalIgnoreCase))
                url += (url.Contains('?') ? "&" : "?") + "clientKey=iBLQWFskMfSF5oGhD2a1UYNZyuYo0tdh";

            await _portalLock.WaitAsync();
            try
            {
                // PDF: headless por defecto (sin ventana). Si ya hay Chrome del Acceder/Consultar,
                // no lo reinicies en otro modo para no cerrar esa sesión.
                var headless = true;
                if (bool.TryParse(_config["AndeConfig:PdfHeadless"], out var pdfHeadless))
                    headless = pdfHeadless;
                if (_sharedBrowser != null && !_sharedBrowser.IsClosed && _sharedHeadless.HasValue)
                    headless = _sharedHeadless.Value;

                IPage page;
                try
                {
                    page = await EnsurePortalPageAsync(headless);
                }
                catch (ProcessException)
                {
                    // Perfil trabado / Chromium roto: forzar relaunch limpio
                    try { if (_sharedBrowser != null) await _sharedBrowser.CloseAsync(); } catch { /* ignore */ }
                    _sharedBrowser = null;
                    _sharedPage = null;
                    _sesionAndeActiva = false;
                    AndeChromeLauncher.ClearProfileLocks(_profileDir ?? "");
                    page = await EnsurePortalPageAsync(headless);
                }
                var portalUrl = _config["AndeConfig:PortalUrl"] ?? "https://www.ande.gov.py/servicios/";

                if (!(page.Url ?? "").Contains("ande.gov.py", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await page.GoToAsync(portalUrl, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                            Timeout = 60000
                        });
                    }
                    catch (Exception exNav)
                    {
                        _logger.LogWarning("ANDE portal PDF: navegación lenta ({Msg})", exNav.Message);
                    }
                }

                if (!string.IsNullOrWhiteSpace(kwfxToken))
                {
                    await page.EvaluateFunctionAsync(@"(tok) => {
                        try {
                            const key = 'kwfSesionUsuarioMiCuenta_produccion';
                            const prev = JSON.parse(localStorage.getItem(key) || '{}');
                            prev.token = tok;
                            localStorage.setItem(key, JSON.stringify(prev));
                        } catch (e) {}
                    }", kwfxToken);
                }

                var tieneGrecaptcha = false;
                try
                {
                    await page.WaitForFunctionAsync(
                        "() => typeof grecaptcha !== 'undefined' && typeof grecaptcha.execute === 'function'",
                        new WaitForFunctionOptions { Timeout = 12000 });
                    tieneGrecaptcha = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ANDE portal PDF: grecaptcha no cargó; se inyecta el script");
                }

                var claveUsada = await page.EvaluateFunctionAsync<string>(
                    $"() => (typeof CLAVE_SITIO_WEB === 'string' && CLAVE_SITIO_WEB) ? CLAVE_SITIO_WEB : '{AndeRecaptchaSiteKey}'");

                if (!tieneGrecaptcha)
                {
                    try
                    {
                        await page.EvaluateFunctionAsync(@"(key) => new Promise((resolve, reject) => {
                            if (window.grecaptcha && grecaptcha.execute) return resolve(true);
                            var s = document.createElement('script');
                            s.src = 'https://www.google.com/recaptcha/api.js?render=' + encodeURIComponent(key);
                            s.onload = () => resolve(true);
                            s.onerror = () => reject(new Error('recaptcha script'));
                            document.head.appendChild(s);
                        })", claveUsada);
                        await page.WaitForFunctionAsync(
                            "() => typeof grecaptcha !== 'undefined' && typeof grecaptcha.execute === 'function'",
                            new WaitForFunctionOptions { Timeout = 12000 });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ANDE portal PDF: no se pudo inyectar grecaptcha");
                        return null;
                    }
                }

                var token = await page.EvaluateFunctionAsync<string>(@"async (siteKey) => {
                    await new Promise(r => grecaptcha.ready(r));
                    return await grecaptcha.execute(siteKey, { action: 'submit' });
                }", claveUsada);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("ANDE portal PDF: reCAPTCHA vacío");
                    return null;
                }

                var urlFull = url;
                if (!string.IsNullOrWhiteSpace(kwfxToken) && !urlFull.Contains("kwfxtoken=", StringComparison.OrdinalIgnoreCase))
                    urlFull += (urlFull.Contains('?') ? "&" : "?") + "kwfxtoken=" + Uri.EscapeDataString(kwfxToken);
                urlFull += (urlFull.Contains('?') ? "&" : "?") + "recapt=" + Uri.EscapeDataString(token);
                _logger.LogInformation("ANDE portal PDF XHR: {Url} siteKey={Key}", url, claveUsada);

                var b64 = await page.EvaluateFunctionAsync<string>(@"(u) => new Promise((resolve) => {
                    try {
                        var x = new XMLHttpRequest();
                        x.open('GET', u, true);
                        x.responseType = 'arraybuffer';
                        x.onload = function () {
                            var bytes = new Uint8Array(x.response || []);
                            if (bytes.length < 5) return resolve('EMPTY:' + x.status);
                            if (bytes[0] !== 0x25 || bytes[1] !== 0x50) {
                                var head = new TextDecoder().decode(bytes.subarray(0, Math.min(120, bytes.length)));
                                return resolve('NOTPDF:' + x.status + ':' + head);
                            }
                            var s = '', chunk = 0x8000;
                            for (var i = 0; i < bytes.length; i += chunk) {
                                s += String.fromCharCode.apply(null, bytes.subarray(i, Math.min(i + chunk, bytes.length)));
                            }
                            resolve(btoa(s));
                        };
                        x.onerror = function () { resolve('XHRERR'); };
                        x.send();
                    } catch (e) {
                        resolve('EX:' + (e && e.message ? e.message : e));
                    }
                })", urlFull);

                if (string.IsNullOrWhiteSpace(b64) || b64.StartsWith("ERR:") || b64.StartsWith("NOTPDF:") || b64.StartsWith("EX:") || b64.StartsWith("EMPTY") || b64 == "XHRERR")
                {
                    _logger.LogWarning("ANDE portal PDF falló: {Preview}", b64 is { Length: > 200 } ? b64[..200] : b64);
                    return null;
                }

                var bytes = Convert.FromBase64String(b64);
                _logger.LogInformation("ANDE portal PDF OK ({Len} bytes)", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ANDE portal PDF error NIS {Nis}", nisLimpio);
                return null;
            }
            finally
            {
                _portalLock.Release();
            }
        }

        public async Task<AndeConsultaResultadoDto> ConsultarEstadoAndeAsync(string nis)
        {
            if (string.IsNullOrWhiteSpace(nis) || nis == "N/A" || nis == "SIN NIS" || nis == "—")
            {
                return new AndeConsultaResultadoDto
                {
                    Nis = nis ?? "SIN NIS",
                    Titular = "NO ASIGNADO",
                    Direccion = "N/A",
                    SaldoDeuda = 0,
                    ConsumoKw = 0,
                    FacturasPendientes = 0,
                    TieneDesconexion = false,
                    EstadoConexion = "MEDIDOR NO REGISTRADO",
                    EstadoPago = "SIN DEUDA",
                    Observaciones = "El n\u00famero de NIS ingresado no es v\u00e1lido o no cuenta con servicio activo.",
                    EsSimulado = true
                };
            }

            string nisLimpio = new string(nis.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(nisLimpio)) nisLimpio = nis.Trim();

            // 1. Enriquecer con datos locales del duplex (solo direccion/contexto)
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<IApplicationDbContext>();
            
            string direccionLocal = "";
            if (db != null)
            {
                try
                {
                    var unidad = await db.UnidadesCondominio
                        .FirstOrDefaultAsync(u => u.NisAnde == nisLimpio || u.NisAnde == nis.Trim());

                    if (unidad != null)
                        direccionLocal = $"{unidad.NombreCondominio} - {unidad.CodigoUnidad}";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error consultando BD local para NIS {Nis}: {Msg}", nisLimpio, ex.Message);
                }
            }

            // 2. Consulta real ANDE (montos/facturas oficiales)
            var ande = await ConsultarFacturasRealesAsync(nisLimpio);

            if (ande != null)
            {
                if (!string.IsNullOrWhiteSpace(direccionLocal))
                    ande.Direccion = direccionLocal;
                else if (string.IsNullOrWhiteSpace(ande.Direccion))
                    ande.Direccion = "Asunci\u00f3n / Gran Asunci\u00f3n - Paraguay";

                ande.TieneDesconexion = ande.SaldoDeuda >= 5_000_000m;
                ande.EstadoConexion = ande.TieneDesconexion
                    ? "ORDEN DE DESCONEXI\u00d3N POR MOROSIDAD (AVISO DE CORTE ANDE)"
                    : (ande.SaldoDeuda > 0
                        ? "CONECTADO - SERVICIO ACTIVO (DEUDA PENDIENTE)"
                        : "CONECTADO - SIN DEUDA (AL D\u00cdA)");
                ande.EstadoPago = ande.SaldoDeuda > 0
                    ? (ande.TieneDesconexion ? $"MOROSO - DEUDA Gs. {ande.SaldoDeuda:N0}" : "PENDIENTE DE PAGO")
                    : "AL D\u00cdA - ABONADO";
                ande.Observaciones = string.IsNullOrWhiteSpace(ande.Observaciones)
                    ? "Montos y facturas obtenidos en l\u00ednea desde el servicio comercial ANDE."
                    : ande.Observaciones;
                ande.FechaConsulta = DateTime.Now;
                ande.EsSimulado = false;
                return ande;
            }

            // 3. Sin respuesta real: no inventar montos
            var obs = "No se pudo obtener el saldo real de ANDE. Abra Consultar ANDE, complete captcha si pide, pulse Acceder y espere la consulta del NIS.";

            return new AndeConsultaResultadoDto
            {
                Nis = nisLimpio,
                Titular = "NO DISPONIBLE",
                Documento = "",
                Direccion = string.IsNullOrWhiteSpace(direccionLocal) ? "Asunci\u00f3n / Gran Asunci\u00f3n - Paraguay" : direccionLocal,
                SaldoDeuda = 0,
                ConsumoKw = 0,
                FacturasPendientes = 0,
                TieneDesconexion = false,
                EstadoConexion = "SIN RESPUESTA ANDE",
                EstadoPago = "NO CONSULTADO",
                FechaConsulta = DateTime.Now,
                Vencimiento = "",
                Observaciones = obs,
                EsSimulado = true,
                Facturas = new List<AndeFacturaItemDto>()
            };
        }
    }
}
