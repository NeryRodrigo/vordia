using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gesco.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Gesco.Application.Services
{
    public interface IAndeJwtConsultaService
    {
        Task<(bool Ok, string Mensaje, bool Renovado)> AsegurarJwtAsync(CancellationToken ct = default);
        Task<AndeConsultaResultadoDto> ConsultarSaldoAsync(string nis, CancellationToken ct = default);
        /// <summary>Token kwfx de la sesión HTTP actual (si vigente).</summary>
        string? GetKwfxTokenActual();
        /// <summary>Descarga el KUDE/PDF oficial de ANDE. Opcionalmente con params de un comprobante concreto.</summary>
        Task<byte[]?> DescargarFacturaPdfOficialAsync(
            string nis,
            string? secNis = null,
            string? secRec = null,
            string? fFact = null,
            bool? electronica = null,
            string? origen = null,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Login + consulta saldo ANDE 100% HTTP (sin Chrome).
    /// Login: registroUnico/v2/acceder → token/JWT.
    /// Saldo: miCuenta/v3/suministro/situacionActual con kwfxtoken (+ cookies del portal).
    /// PDF: open/v5/suministro/facturaElectronicaPdf (mismo formato que el portal ANDE).
    /// </summary>
    public class AndeJwtConsultaService : IAndeJwtConsultaService
    {
        private const string ClientKey = "iBLQWFskMfSF5oGhD2a1UYNZyuYo0tdh";
        /// <summary>Site key reCAPTCHA v3 del portal ANDE (CLAVE_SITIO_WEB). NO usar la key comentada "mio" del JS.</summary>
        private const string AndeRecaptchaSiteKey = "6LcND9ohAAAAALizDIFfkfpSjHm3Xtr0DwLOLc60";
        private const string AccederUrl = "https://prod2.ande.gov.py:8581/sigaWs/api/registroUnico/v2/acceder";
        private const string SituacionUrl = "https://prod2.ande.gov.py:8581/sigaWs/api/miCuenta/v3/suministro/situacionActual";
        private const string UltimasUrl = "https://prod2.ande.gov.py:8581/sigaWs/api/miCuenta/v3/suministro/ultimasFacturas";
        private const string MiCuentaBase = "https://prod2.ande.gov.py:8581/sigaWs/api/miCuenta/";
        private const string OpenApiBase = "https://prod2.ande.gov.py:8581/sigaWs/api/open/";
        private const string PortalUrl = "https://www.ande.gov.py/servicios/";

        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static readonly CookieContainer Cookies = new();
        private static readonly HttpClient AndeHttp = CrearHttpClient();

        private static string? _kwfxToken;
        private static string? _jwt;
        private static DateTimeOffset _tokenExpUtc = DateTimeOffset.MinValue;
        private static DateTimeOffset _lastWarmupUtc = DateTimeOffset.MinValue;

        private readonly IConfiguration _config;
        private readonly ILogger<AndeJwtConsultaService> _logger;
        private readonly IAndeCredentialsStore _creds;

        public AndeJwtConsultaService(
            IConfiguration config,
            ILogger<AndeJwtConsultaService> logger,
            IAndeCredentialsStore creds)
        {
            _config = config;
            _logger = logger;
            _creds = creds;
        }

        // Compatibilidad con AddHttpClient&lt;IAndeJwtConsultaService, ...&gt; (HttpClient inyectado se ignora: usamos jar de cookies estático).
        public AndeJwtConsultaService(
            HttpClient _,
            IConfiguration config,
            ILogger<AndeJwtConsultaService> logger,
            IAndeCredentialsStore creds)
            : this(config, logger, creds)
        {
        }

        /// <summary>Invalida token JWT/kwfx en memoria (p. ej. al cambiar credenciales).</summary>
        public static void InvalidarSesionEstatica()
        {
            _kwfxToken = null;
            _jwt = null;
            _tokenExpUtc = DateTimeOffset.MinValue;
        }

        /// <summary>Prueba login HTTP con credenciales explícitas y deja la sesión lista si OK.</summary>
        public static async Task<(bool Ok, string Mensaje, bool Renovado)> ProbarLoginConCredencialesAsync(
            IConfiguration config,
            string tipoDocumento,
            string documento,
            string password,
            string cedulaRepresentante,
            string tipoSolicitante,
            CancellationToken ct = default)
        {
            await Gate.WaitAsync(ct);
            try
            {
                InvalidarSesionEstatica();

                try
                {
                    using var warm = await AndeHttp.GetAsync(PortalUrl, ct);
                    _ = await warm.Content.ReadAsStringAsync(ct);
                    _lastWarmupUtc = DateTimeOffset.UtcNow;
                }
                catch
                {
                    /* warmup best-effort */
                }

                var tipo = NormalizarTipoDocumento(tipoDocumento);
                using var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["tipoDocumento"] = tipo,
                    ["documentoIdentificacion"] = (documento ?? "").Trim(),
                    ["password"] = password ?? "",
                    ["cedulaRepresentante"] = string.IsNullOrWhiteSpace(cedulaRepresentante) ? "lteor" : cedulaRepresentante,
                    ["tipoSolicitante"] = string.IsNullOrWhiteSpace(tipoSolicitante) ? "Sin registros" : tipoSolicitante,
                    ["clientKey"] = config["AndeConfig:ClientKey"] ?? ClientKey
                });

                using var resp = await AndeHttp.PostAsync(AccederUrl, form, ct);
                var txt = await resp.Content.ReadAsStringAsync(ct);
                if (EsBloqueoRadware(txt))
                    return (false, "ANDE bloqueó el acceso (WAF). Reintentá en unos minutos.", false);

                using var docJson = JsonDocument.Parse(txt);
                var root = docJson.RootElement;
                var loginOk = root.TryGetProperty("login", out var lg) && lg.ValueKind == JsonValueKind.True;
                if (!loginOk)
                    return (false, "Usuario o contraseña incorrectos en Mi Cuenta ANDE.", false);

                _kwfxToken = root.TryGetProperty("token", out var tk) ? tk.GetString() : null;
                _jwt = root.TryGetProperty("JWTtoken", out var jwt) ? jwt.GetString() : null;
                _tokenExpUtc = ExtraerExpJwt(_jwt) ?? DateTimeOffset.UtcNow.AddMinutes(15);

                if (string.IsNullOrWhiteSpace(_kwfxToken))
                    return (false, "ANDE no devolvió token de sesión.", false);

                return (true, "Login ANDE OK.", true);
            }
            catch (Exception ex)
            {
                return (false, $"Error al validar con ANDE: {ex.Message}", false);
            }
            finally
            {
                Gate.Release();
            }
        }

        private static HttpClient CrearHttpClient()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = Cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.All
            };
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://www.ande.gov.py");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.ande.gov.py/servicios/");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/javascript, */*; q=0.01");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "es-PY,es;q=0.9,en;q=0.8");
            http.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            return http;
        }

        public string? GetKwfxTokenActual() => TokenVigente() ? _kwfxToken : null;

        public async Task<(bool Ok, string Mensaje, bool Renovado)> AsegurarJwtAsync(CancellationToken ct = default)
        {
            await Gate.WaitAsync(ct);
            try
            {
                if (TokenVigente())
                    return (true, "JWT/sesión ANDE aún vigente.", false);

                if (!_creds.EstaConfigurado())
                    return (false, "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta.", false);

                var ok = await LoginHttpAsync(ct);
                return ok
                    ? (true, "Login ANDE OK (HTTP, sin Chrome).", true)
                    : (false, "No se pudo autenticar en ANDE. Revisá usuario/contraseña de Mi Cuenta.", false);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<AndeConsultaResultadoDto> ConsultarSaldoAsync(string nis, CancellationToken ct = default)
        {
            var nisLimpio = new string((nis ?? "").Where(char.IsDigit).ToArray());
            if (nisLimpio.Length == 0)
            {
                return new AndeConsultaResultadoDto
                {
                    Nis = nis ?? "",
                    Observaciones = "NIS inválido",
                    FechaConsulta = DateTime.Now
                };
            }

            await Gate.WaitAsync(ct);
            try
            {
                await WarmupPortalAsync(ct);

                if (!TokenVigente())
                {
                    if (!await LoginHttpAsync(ct))
                    {
                        return new AndeConsultaResultadoDto
                        {
                            Nis = nisLimpio,
                            Observaciones = "Login ANDE falló (credenciales o red).",
                            FechaConsulta = DateTime.Now
                        };
                    }
                }

                var token = _kwfxToken!;
                var situacionTxt = await PostFormAsync(SituacionUrl, new Dictionary<string, string>
                {
                    ["kwfxtoken"] = token,
                    ["nis"] = nisLimpio
                }, ct);

                if (EsBloqueoRadware(situacionTxt))
                {
                    _logger.LogWarning("ANDE JWT: situacionActual bloqueada, renovando sesión…");
                    await WarmupPortalAsync(ct, forzar: true);
                    if (await LoginHttpAsync(ct))
                    {
                        token = _kwfxToken!;
                        situacionTxt = await PostFormAsync(SituacionUrl, new Dictionary<string, string>
                        {
                            ["kwfxtoken"] = token,
                            ["nis"] = nisLimpio
                        }, ct);
                    }
                }

                if (EsBloqueoRadware(situacionTxt) || string.IsNullOrWhiteSpace(situacionTxt))
                {
                    return new AndeConsultaResultadoDto
                    {
                        Nis = nisLimpio,
                        Observaciones = "ANDE bloqueó la consulta HTTP. Reintentá en unos segundos.",
                        FechaConsulta = DateTime.Now
                    };
                }

                string? ultimasTxt = null;
                try
                {
                    ultimasTxt = await PostFormAsync(UltimasUrl, new Dictionary<string, string>
                    {
                        ["kwfxtoken"] = token,
                        ["nis"] = nisLimpio,
                        ["cantidad"] = "15"
                    }, ct);
                    if (EsBloqueoRadware(ultimasTxt))
                        ultimasTxt = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ANDE JWT: ultimasFacturas opcional falló");
                }

                return MapearResultado(nisLimpio, situacionTxt, ultimasTxt);
            }
            finally
            {
                Gate.Release();
            }
        }

        private static readonly SemaphoreSlim PdfBrowserLock = new(1, 1);
        private static IBrowser? _pdfBrowser;
        private static IPage? _pdfPage;
        private static bool? _pdfHeadless;

        public async Task<byte[]?> DescargarFacturaPdfOficialAsync(
            string nis,
            string? secNis = null,
            string? secRec = null,
            string? fFact = null,
            bool? electronica = null,
            string? origen = null,
            CancellationToken ct = default)
        {
            var nisLimpio = new string((nis ?? "").Where(char.IsDigit).ToArray());
            if (nisLimpio.Length == 0)
                return null;

            var origenNorm = (origen ?? "").Trim().ToLowerInvariant();
            var esPenultima = origenNorm == "penultima";

            await Gate.WaitAsync(ct);
            var urls = new List<string>();
            string? kwfx = null;
            try
            {
                await WarmupPortalAsync(ct);

                if (!TokenVigente())
                {
                    if (!await LoginHttpAsync(ct))
                    {
                        _logger.LogWarning("ANDE PDF: login JWT falló para NIS {Nis}", nisLimpio);
                        return null;
                    }
                }

                kwfx = _kwfxToken;

                var situacionTxt = await PostFormAsync(SituacionUrl, new Dictionary<string, string>
                {
                    ["kwfxtoken"] = _kwfxToken!,
                    ["nis"] = nisLimpio
                }, ct);

                if (!EsBloqueoRadware(situacionTxt) && !string.IsNullOrWhiteSpace(situacionTxt))
                {
                    CompletarParamsDesdeSituacion(situacionTxt, origenNorm, ref secNis, ref secRec, ref fFact);
                    urls.AddRange(ConstruirUrlsPdfDesdeSituacion(nisLimpio, situacionTxt));
                }

                urls.InsertRange(0, ConstruirUrlsPdfComprobante(nisLimpio, secNis, secRec, fFact, electronica ?? true, esPenultima));
            }
            finally
            {
                Gate.Release();
            }

            // GET open/ y Chrome XHR reciben "Make sure Captcha Page" (Radware).
            // situacionActual ya funciona por POST + kwfxtoken: el PDF va por el mismo canal.
            var form = new Dictionary<string, string>
            {
                ["kwfxtoken"] = kwfx ?? "",
                ["nis"] = nisLimpio,
                ["nro_nis"] = nisLimpio
            };
            if (!string.IsNullOrWhiteSpace(secNis))
                form["sec_nis"] = secNis!;
            if (!string.IsNullOrWhiteSpace(secRec))
                form["sec_rec"] = secRec!;
            if (!string.IsNullOrWhiteSpace(fFact))
                form["f_fact"] = FormatearFechaFacturaAnde(fFact);

            var postUrls = new List<string>();
            if (esPenultima)
            {
                postUrls.Add(MiCuentaBase + "v5/suministro/penultimaFacturaElectronicaPendientePdf");
                postUrls.Add(MiCuentaBase + "v3/suministro/penultimaFacturaElectronicaPendientePdf");
            }
            if (!string.IsNullOrWhiteSpace(secNis) && !string.IsNullOrWhiteSpace(secRec))
            {
                postUrls.Add(MiCuentaBase + "v5/suministro/facturaElectronicaPdf");
                postUrls.Add(MiCuentaBase + "v3/suministro/facturaElectronicaPdf");
            }
            postUrls.Add(MiCuentaBase + "v5/suministro/ultimaFacturaElectronicaPendientePdf");
            postUrls.Add(MiCuentaBase + "v3/suministro/ultimaFacturaElectronicaPendientePdf");

            foreach (var postUrl in postUrls.Distinct())
            {
                _logger.LogInformation("ANDE PDF POST: {Url}", postUrl);
                var pdfPost = await TryDescargarPdfPostAsync(postUrl, form, ct);
                if (pdfPost != null) return pdfPost;
            }

            _logger.LogInformation("ANDE PDF: sin resultado para NIS {Nis}", nisLimpio);
            return null;
        }

        private async Task<byte[]?> TryDescargarPdfAsync(string url, CancellationToken ct, string? conRecapt)
        {
            try
            {
                var sep = url.Contains('?') ? "&" : "?";
                var urlFinal = url;
                if (!string.IsNullOrWhiteSpace(_kwfxToken) && !urlFinal.Contains("kwfxtoken=", StringComparison.OrdinalIgnoreCase))
                    urlFinal += $"{sep}kwfxtoken={Uri.EscapeDataString(_kwfxToken)}";
                if (!string.IsNullOrWhiteSpace(conRecapt))
                {
                    sep = urlFinal.Contains('?') ? "&" : "?";
                    urlFinal += $"{sep}recapt={Uri.EscapeDataString(conRecapt)}";
                }

                using var req = new HttpRequestMessage(HttpMethod.Get, urlFinal);
                req.Headers.TryAddWithoutValidation("Accept", "application/pdf,*/*");
                req.Headers.TryAddWithoutValidation("Referer", "https://www.ande.gov.py/servicios/");
                req.Headers.TryAddWithoutValidation("Origin", "https://www.ande.gov.py");
                if (!string.IsNullOrWhiteSpace(_jwt))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);

                using var resp = await AndeHttp.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogDebug("ANDE PDF GET {Status} {Url}", (int)resp.StatusCode, url);
                    return null;
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                if (!EsPdfBytes(bytes))
                {
                    var preview = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 160));
                    _logger.LogWarning("ANDE PDF GET no-PDF {Status} ({Len}): {Preview}", (int)resp.StatusCode, bytes.Length, preview);
                    return null;
                }

                _logger.LogInformation("ANDE PDF oficial OK ({Len} bytes) desde {Url}", bytes.Length, url);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ANDE PDF fallo GET {Url}", url);
                return null;
            }
        }

        /// <summary>
        /// Replica el flujo del portal: grecaptcha.execute → abrir PDF con &amp;recapt=...
        /// Inyecta cookies JWT en Chrome para que ANDE acepte la descarga.
        /// </summary>
        private async Task<byte[]?> DescargarPdfConRecaptchaBrowserAsync(string pdfUrl, string? kwfxToken, CancellationToken ct)
        {
            await PdfBrowserLock.WaitAsync(ct);
            try
            {
                var page = await EnsurePdfPortalPageAsync(ct);
                await InyectarCookiesHttpEnPaginaAsync(page);

                // Dejar el token JWT visible para el JS del portal (como Mi Cuenta)
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

                var (token, claveUsada) = await EjecutarRecaptchaEnPaginaAsync(page);
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("ANDE PDF: token reCAPTCHA vacío");
                    return null;
                }

                _logger.LogInformation("ANDE PDF: reCAPTCHA OK (len={Len}, siteKey={Key})", token.Length, claveUsada);

                // Igual que factura_publico / ampliarFactura: kwfxtoken opcional + recapt obligatorio.
                var urlFull = pdfUrl;
                if (!string.IsNullOrWhiteSpace(kwfxToken) && !urlFull.Contains("kwfxtoken=", StringComparison.OrdinalIgnoreCase))
                    urlFull += (urlFull.Contains('?') ? "&" : "?") + "kwfxtoken=" + Uri.EscapeDataString(kwfxToken);
                urlFull += (urlFull.Contains('?') ? "&" : "?") + "recapt=" + Uri.EscapeDataString(token);

                // XHR como je.descargarPdf del portal (fetch suele dar "Failed to fetch"/CORS).
                var b64 = await page.EvaluateFunctionAsync<string>(@"(url) => new Promise((resolve) => {
                    try {
                        var x = new XMLHttpRequest();
                        x.open('GET', url, true);
                        x.responseType = 'arraybuffer';
                        x.onload = function () {
                            var bytes = new Uint8Array(x.response || []);
                            if (bytes.length < 5) return resolve('EMPTY:' + x.status);
                            if (bytes[0] !== 0x25 || bytes[1] !== 0x50 || bytes[2] !== 0x44 || bytes[3] !== 0x46) {
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

                if (!string.IsNullOrWhiteSpace(b64) && !b64.StartsWith("ERR:") && !b64.StartsWith("NOTPDF:") && !b64.StartsWith("EX:") && !b64.StartsWith("EMPTY") && b64 != "XHRERR")
                {
                    var bytesPdf = Convert.FromBase64String(b64);
                    if (EsPdfBytes(bytesPdf))
                    {
                        _logger.LogInformation("ANDE PDF oficial OK ({Len} bytes)", bytesPdf.Length);
                        return bytesPdf;
                    }
                }

                _logger.LogWarning("ANDE PDF XHR falló: {Preview}. Probando navegación CDP…",
                    b64 is { Length: > 200 } ? b64[..200] : b64);

                // 2) Igual que el portal (<object data=url>): navegar y capturar el body PDF
                var pdfViaNav = await DescargarPdfNavegandoAsync(page, urlFull, ct);
                if (pdfViaNav != null)
                    return pdfViaNav;

                return null;
            }
            finally
            {
                PdfBrowserLock.Release();
            }
        }

        /// <summary>Navega a la URL del PDF (como el &lt;object&gt; del portal) y captura bytes vía CDP.</summary>
        private async Task<byte[]?> DescargarPdfNavegandoAsync(IPage page, string urlFull, CancellationToken ct)
        {
            try
            {
                var tcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
                async void OnResponse(object? sender, ResponseCreatedEventArgs e)
                {
                    try
                    {
                        var u = e.Response.Url ?? "";
                        if (!u.Contains("factura", StringComparison.OrdinalIgnoreCase)
                            && !u.Contains("Pdf", StringComparison.OrdinalIgnoreCase)
                            && !u.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                            return;

                        var buf = await e.Response.BufferAsync();
                        if (EsPdfBytes(buf))
                            tcs.TrySetResult(buf);
                    }
                    catch
                    {
                        /* ignorar respuestas intermedias */
                    }
                }

                page.Response += OnResponse;
                try
                {
                    await page.GoToAsync(urlFull, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                        Timeout = 60000
                    });

                    // A veces el PDF ya está en el buffer de la navegación
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    linked.CancelAfter(TimeSpan.FromSeconds(15));
                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(15000, linked.Token));
                    if (completed == tcs.Task && tcs.Task.IsCompletedSuccessfully && tcs.Task.Result != null)
                    {
                        _logger.LogInformation("ANDE PDF oficial OK vía navegación ({Len} bytes)", tcs.Task.Result.Length);
                        return tcs.Task.Result;
                    }
                }
                finally
                {
                    page.Response -= OnResponse;
                    // Volver al portal para el próximo intento / grecaptcha
                    try
                    {
                        await page.GoToAsync(PortalUrl, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                            Timeout = 45000
                        });
                        await Task.Delay(1500, ct);
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ANDE PDF navegación CDP falló");
            }

            return null;
        }

        private async Task InyectarCookiesHttpEnPaginaAsync(IPage page)
        {
            try
            {
                var hosts = new[]
                {
                    new Uri("https://prod2.ande.gov.py/"),
                    new Uri("https://www.ande.gov.py/"),
                    new Uri("https://prod1.ande.gov.py/")
                };
                var cookies = new List<CookieParam>();
                foreach (var host in hosts)
                {
                    foreach (System.Net.Cookie c in Cookies.GetCookies(host))
                    {
                        if (string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Value))
                            continue;
                        cookies.Add(new CookieParam
                        {
                            Name = c.Name,
                            Value = c.Value,
                            Domain = string.IsNullOrWhiteSpace(c.Domain) ? host.Host : c.Domain.TrimStart('.'),
                            Path = string.IsNullOrWhiteSpace(c.Path) ? "/" : c.Path,
                            HttpOnly = c.HttpOnly,
                            Secure = c.Secure
                        });
                    }
                }
                if (cookies.Count > 0)
                    await page.SetCookieAsync(cookies.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ANDE PDF: no se pudieron inyectar cookies en Chrome");
            }
        }

        private async Task<IPage> EnsurePdfPortalPageAsync(CancellationToken ct)
        {
            // Headless por defecto: misma UX que consulta JWT (sin ventana Chrome).
            // AndeConfig:PdfHeadless=false solo si ANDE vuelve a rechazar tokens headless.
            var headless = true;
            if (bool.TryParse(_config["AndeConfig:PdfHeadless"], out var pdfHeadless))
                headless = pdfHeadless;
            var profileDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Gesco", "ande-chrome-profile-pdf");
            Directory.CreateDirectory(profileDir);

            var alive = _pdfBrowser != null && !_pdfBrowser.IsClosed;
            if (alive && _pdfHeadless != headless)
            {
                try { await _pdfBrowser!.CloseAsync(); } catch { /* ignore */ }
                _pdfBrowser = null;
                _pdfPage = null;
                alive = false;
            }

            if (!alive)
            {
                _pdfBrowser = await AndeChromeLauncher.LaunchAsync(profileDir, headless, _logger);
                _pdfHeadless = headless;
                _pdfPage = null;
            }

            if (_pdfPage == null || _pdfPage.IsClosed)
            {
                var pages = await _pdfBrowser!.PagesAsync();
                _pdfPage = pages.FirstOrDefault() ?? await _pdfBrowser.NewPageAsync();
                await _pdfPage.SetUserAgentAsync(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                try
                {
                    await _pdfPage.EvaluateFunctionOnNewDocumentAsync(@"() => {
                        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                    }");
                }
                catch { /* ignore */ }
            }

            var url = (_pdfPage.Url ?? "").ToLowerInvariant();
            if (!url.Contains("ande.gov.py"))
            {
                try
                {
                    await _pdfPage.GoToAsync(PortalUrl, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                        Timeout = 20000
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("ANDE PDF: portal lento ({Msg})", ex.Message);
                }
            }

            return _pdfPage;
        }

        /// <summary>Token reCAPTCHA en la página del portal. Inyecta el script si ANDE no lo cargó.</summary>
        private async Task<(string? Token, string SiteKey)> EjecutarRecaptchaEnPaginaAsync(IPage page)
        {
            var tieneGrecaptcha = false;
            try
            {
                await page.WaitForFunctionAsync(
                    "() => typeof grecaptcha !== 'undefined' && typeof grecaptcha.execute === 'function'",
                    new WaitForFunctionOptions { Timeout = 8000 });
                tieneGrecaptcha = true;
            }
            catch
            {
                /* WAF / headless: el portal a veces no carga grecaptcha */
            }

            var siteKey = AndeRecaptchaSiteKey;
            try
            {
                var portalKey = await page.EvaluateFunctionAsync<string>(
                    "() => (typeof CLAVE_SITIO_WEB === 'string' && CLAVE_SITIO_WEB.length > 10) ? CLAVE_SITIO_WEB : ''");
                if (!string.IsNullOrWhiteSpace(portalKey))
                    siteKey = portalKey;
            }
            catch { /* usar key conocida */ }

            if (!tieneGrecaptcha)
            {
                _logger.LogInformation("ANDE PDF: inyectando script reCAPTCHA (siteKey={Key})", siteKey);
                await page.EvaluateFunctionAsync(@"(key) => new Promise((resolve, reject) => {
                    if (window.grecaptcha && grecaptcha.execute) return resolve(true);
                    var s = document.createElement('script');
                    s.src = 'https://www.google.com/recaptcha/api.js?render=' + encodeURIComponent(key);
                    s.onload = () => resolve(true);
                    s.onerror = () => reject(new Error('recaptcha script'));
                    document.head.appendChild(s);
                })", siteKey);
                await page.WaitForFunctionAsync(
                    "() => typeof grecaptcha !== 'undefined' && typeof grecaptcha.execute === 'function'",
                    new WaitForFunctionOptions { Timeout = 10000 });
            }

            var token = await page.EvaluateFunctionAsync<string>(@"async (siteKey) => {
                await new Promise(r => grecaptcha.ready(r));
                return await grecaptcha.execute(siteKey, { action: 'submit' });
            }", siteKey);

            return (string.IsNullOrWhiteSpace(token) ? null : token, siteKey);
        }

        private static bool EsPdfBytes(byte[]? bytes) =>
            bytes is { Length: >= 5 }
            && bytes[0] == (byte)'%'
            && bytes[1] == (byte)'P'
            && bytes[2] == (byte)'D'
            && bytes[3] == (byte)'F';

        private static byte[]? ExtraerPdfDesdeJson(byte[] bytes)
        {
            if (bytes.Length < 2 || bytes[0] != (byte)'{')
                return null;
            try
            {
                using var doc = JsonDocument.Parse(bytes);
                foreach (var key in new[] { "pdf", "archivo", "contenido", "base64", "kude", "factura", "data" })
                {
                    if (!doc.RootElement.TryGetProperty(key, out var el))
                        continue;
                    var pdf = BytesDesdeNodoJson(el);
                    if (pdf != null) return pdf;
                }
                if (doc.RootElement.TryGetProperty("resultado", out var res) && res.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "pdf", "archivo", "contenido", "base64", "kude" })
                    {
                        if (!res.TryGetProperty(key, out var el))
                            continue;
                        var pdf = BytesDesdeNodoJson(el);
                        if (pdf != null) return pdf;
                    }
                }
            }
            catch
            {
                /* no es JSON útil */
            }
            return null;
        }

        private static byte[]? BytesDesdeNodoJson(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.String)
                return null;
            var s = (el.GetString() ?? "").Trim();
            if (s.Length < 20)
                return null;
            var comma = s.IndexOf(',');
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                s = s[(comma + 1)..];
            try
            {
                var raw = Convert.FromBase64String(s);
                return EsPdfBytes(raw) ? raw : null;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> ConstruirUrlsPdfComprobante(
            string nis,
            string? secNis,
            string? secRec,
            string? fFact,
            bool electronica,
            bool esPenultima)
        {
            if (!string.IsNullOrWhiteSpace(secNis) && !string.IsNullOrWhiteSpace(secRec) && !string.IsNullOrWhiteSpace(fFact))
            {
                var f = FormatearFechaFacturaAnde(fFact);
                var qs = $"nro_nis={nis}&sec_nis={secNis}&sec_rec={secRec}&f_fact={f}";
                yield return MiCuentaBase + (electronica ? "v5/suministro/facturaElectronicaPdf?" : "v3/suministro/facturaPdf?") + qs;
                yield return MiCuentaBase + (!electronica ? "v5/suministro/facturaElectronicaPdf?" : "v3/suministro/facturaPdf?") + qs;
                yield return OpenApiBase + (electronica ? "v5/suministro/facturaElectronicaPdf?" : "v4/suministro/facturaPdf?") + qs;
            }

            if (esPenultima)
            {
                yield return MiCuentaBase + $"v5/suministro/penultimaFacturaElectronicaPendientePdf?nis={nis}";
                yield return MiCuentaBase + $"v3/suministro/penultimaFacturaElectronicaPendientePdf?nis={nis}";
                yield return OpenApiBase + $"v5/suministro/penultimaFacturaElectronicaPendientePdf?nis={nis}";
                yield return OpenApiBase + $"v4/suministro/penultimaFacturaPendientePdf?nis={nis}";
            }

            yield return MiCuentaBase + $"v5/suministro/ultimaFacturaElectronicaPendientePdf?nis={nis}";
            yield return MiCuentaBase + $"v3/suministro/ultimaFacturaElectronicaPendientePdf?nis={nis}";
            yield return OpenApiBase + $"v5/suministro/ultimaFacturaElectronicaPendientePdf?nis={nis}";
            yield return OpenApiBase + $"v4/suministro/ultimaFacturaPendientePdf?nis={nis}";
        }

        private static void CompletarParamsDesdeSituacion(
            string situacionTxt,
            string origen,
            ref string? secNis,
            ref string? secRec,
            ref string? fFact)
        {
            if (!string.IsNullOrWhiteSpace(secNis) && !string.IsNullOrWhiteSpace(secRec) && !string.IsNullOrWhiteSpace(fFact))
                return;

            try
            {
                using var doc = JsonDocument.Parse(situacionTxt);
                if (!doc.RootElement.TryGetProperty("resultado", out var res))
                    return;

                var props = string.Equals(origen, "penultima", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "facturaPenultima", "facturaDatos", "facturaPagada" }
                    : new[] { "facturaDatos", "facturaPenultima", "facturaPagada" };

                foreach (var prop in props)
                {
                    if (!res.TryGetProperty(prop, out var fact) || fact.ValueKind != JsonValueKind.Object)
                        continue;
                    if (!fact.EnumerateObject().Any())
                        continue;

                    JsonElement? recibo = fact.TryGetProperty("recibo", out var rec) && rec.ValueKind == JsonValueKind.Object
                        ? rec
                        : null;
                    var a = TextoCampoJson(fact, "sec_nis", "secNis", "secnis")
                        ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_nis", "secNis") : null);
                    var b = TextoCampoJson(fact, "sec_rec", "secRec", "secrec", "nroRecibo")
                        ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_rec", "secRec", "nroRecibo") : null);
                    var c = TextoCampoJson(fact, "fechaFacturacion", "f_fact", "fechaEmision", "emision")
                        ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "fechaFacturacion", "f_fact", "emision") : null);

                    if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                        continue;

                    secNis ??= a;
                    secRec ??= b;
                    if (string.IsNullOrWhiteSpace(fFact))
                        fFact = c;
                    return;
                }
            }
            catch
            {
                /* situacion no parseable */
            }
        }

        private async Task<byte[]?> TryDescargarPdfPostAsync(
            string url,
            Dictionary<string, string> data,
            CancellationToken ct)
        {
            try
            {
                using var form = new FormUrlEncodedContent(data);
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
                req.Headers.TryAddWithoutValidation("Accept", "application/pdf,*/*");
                req.Headers.TryAddWithoutValidation("Referer", "https://www.ande.gov.py/servicios/");
                if (!string.IsNullOrWhiteSpace(_jwt))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);

                using var resp = await AndeHttp.SendAsync(req, ct);
                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                if (EsPdfBytes(bytes))
                {
                    _logger.LogInformation("ANDE PDF POST OK ({Len} bytes) {Url}", bytes.Length, url);
                    return bytes;
                }

                var preview = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 200));
                var desdeJson = ExtraerPdfDesdeJson(bytes);
                if (desdeJson != null)
                {
                    _logger.LogInformation("ANDE PDF POST JSON OK ({Len} bytes) {Url}", desdeJson.Length, url);
                    return desdeJson;
                }

                _logger.LogWarning("ANDE PDF POST no-PDF {Status} ({Len}): {Preview}", (int)resp.StatusCode, bytes.Length, preview);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ANDE PDF POST falló {Url}", url);
                return null;
            }
        }

        private static IEnumerable<string> ConstruirUrlsPdfDesdeSituacion(string nis, string situacionTxt)
        {
            using var doc = JsonDocument.Parse(situacionTxt);
            if (!doc.RootElement.TryGetProperty("resultado", out var res))
                yield break;

            foreach (var prop in new[] { "facturaDatos", "facturaPagada", "facturaPenultima" })
            {
                if (!res.TryGetProperty(prop, out var fact) || fact.ValueKind != JsonValueKind.Object)
                    continue;
                if (!fact.EnumerateObject().Any())
                    continue;

                foreach (var url in ArmarUrlsPdfFactura(nis, fact))
                    yield return url;
            }
        }

        private static IEnumerable<string> ArmarUrlsPdfFactura(string nis, JsonElement fact)
        {
            JsonElement? recibo = fact.TryGetProperty("recibo", out var rec) && rec.ValueKind == JsonValueKind.Object
                ? rec
                : null;

            var secNis = TextoCampoJson(fact, "sec_nis", "secNis", "secnis", "nroSecNis")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_nis", "secNis") : null);
            var secRec = TextoCampoJson(fact, "sec_rec", "secRec", "secrec", "nroRecibo", "nro_recibo")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_rec", "secRec", "nroRecibo") : null);
            var fFactRaw = TextoCampoJson(fact, "fechaFacturacion", "f_fact", "fechaEmision", "emision")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "fechaFacturacion", "f_fact", "fechaEmision", "emision") : null);

            if (string.IsNullOrWhiteSpace(secNis) || string.IsNullOrWhiteSpace(secRec) || string.IsNullOrWhiteSpace(fFactRaw))
                yield break;

            var fFact = FormatearFechaFacturaAnde(fFactRaw);
            // Portal ANDE no encodea f_fact (deja los /); sec_* y nis sí van crudos en JS.
            var qs = $"nro_nis={nis}&sec_nis={secNis}&sec_rec={secRec}&f_fact={fFact}";
            yield return OpenApiBase + "v5/suministro/facturaElectronicaPdf?" + qs;
            yield return OpenApiBase + "v4/suministro/facturaPdf?" + qs;
        }

        private static bool EsJsonVacio(JsonElement v) =>
            v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || (v.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(v.GetString()));

        private static string? TextoCampoJson(JsonElement el, params string[] keys)
        {
            if (el.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var key in keys)
            {
                if (el.TryGetProperty(key, out var exact) && !EsJsonVacio(exact))
                {
                    var s = exact.ValueKind == JsonValueKind.String ? exact.GetString() : exact.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s.Trim();
                }

                var keyNorm = key.Replace("_", "");
                foreach (var p in el.EnumerateObject())
                {
                    if (!string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(p.Name.Replace("_", ""), keyNorm, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (EsJsonVacio(p.Value))
                        continue;
                    var s = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s.Trim();
                }
            }

            return null;
        }

        /// <summary>ANDE espera f_fact como dd/MM/yyyy (desde yyyy-MM-dd u otros).</summary>
        private static string FormatearFechaFacturaAnde(string raw)
        {
            var s = (raw ?? "").Trim();
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
                || DateTime.TryParse(s, CultureInfo.GetCultureInfo("es-PY"), DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            // yyyy-MM-dd
            var parts = s.Split('-', '/', '.');
            if (parts.Length == 3 && parts[0].Length == 4)
                return $"{parts[2].PadLeft(2, '0')}/{parts[1].PadLeft(2, '0')}/{parts[0]}";

            return s;
        }

        private bool TokenVigente() =>
            !string.IsNullOrWhiteSpace(_kwfxToken) &&
            DateTimeOffset.UtcNow < _tokenExpUtc.AddSeconds(-60);

        private async Task WarmupPortalAsync(CancellationToken ct, bool forzar = false)
        {
            if (!forzar && DateTimeOffset.UtcNow - _lastWarmupUtc < TimeSpan.FromMinutes(10))
                return;

            try
            {
                using var resp = await AndeHttp.GetAsync(PortalUrl, ct);
                _ = await resp.Content.ReadAsStringAsync(ct);
                _lastWarmupUtc = DateTimeOffset.UtcNow;
                _logger.LogDebug("ANDE JWT: warmup portal OK ({Status})", (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ANDE JWT: warmup portal falló");
            }
        }

        private async Task<bool> LoginHttpAsync(CancellationToken ct)
        {
            var tipo = NormalizarTipoDocumento(_creds.GetTipoDocumento());
            var doc = _creds.GetDocumento();
            var pass = _creds.GetPassword();

            if (string.IsNullOrWhiteSpace(doc) || string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogError("AndeConfig:Documento/Password no configurados.");
                return false;
            }

            await WarmupPortalAsync(ct);

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["tipoDocumento"] = tipo,
                ["documentoIdentificacion"] = doc.Trim(),
                ["password"] = pass,
                ["cedulaRepresentante"] = _creds.GetCedulaRepresentante(),
                ["tipoSolicitante"] = _creds.GetTipoSolicitante(),
                ["clientKey"] = _config["AndeConfig:ClientKey"] ?? ClientKey
            });

            using var resp = await AndeHttp.PostAsync(AccederUrl, form, ct);
            var txt = await resp.Content.ReadAsStringAsync(ct);
            if (EsBloqueoRadware(txt))
            {
                _logger.LogWarning("ANDE login HTTP bloqueado por WAF.");
                return false;
            }

            using var docJson = JsonDocument.Parse(txt);
            var root = docJson.RootElement;
            var loginOk = root.TryGetProperty("login", out var lg) && lg.ValueKind == JsonValueKind.True;
            if (!loginOk)
            {
                _logger.LogWarning("ANDE login HTTP rechazado: {Preview}", Preview(txt));
                _kwfxToken = null;
                _jwt = null;
                _tokenExpUtc = DateTimeOffset.MinValue;
                return false;
            }

            _kwfxToken = root.TryGetProperty("token", out var tk) ? tk.GetString() : null;
            _jwt = root.TryGetProperty("JWTtoken", out var jwt) ? jwt.GetString() : null;
            _tokenExpUtc = ExtraerExpJwt(_jwt) ?? DateTimeOffset.UtcNow.AddMinutes(15);

            _logger.LogInformation(
                "ANDE JWT login OK (sin Chrome). tokenLen={Len} expUtc={Exp}",
                _kwfxToken?.Length ?? 0, _tokenExpUtc);
            return !string.IsNullOrWhiteSpace(_kwfxToken);
        }

        private static async Task<string> PostFormAsync(
            string url,
            Dictionary<string, string> data,
            CancellationToken ct)
        {
            using var form = new FormUrlEncodedContent(data);
            using var resp = await AndeHttp.PostAsync(url, form, ct);
            return await resp.Content.ReadAsStringAsync(ct);
        }

        private static string NormalizarTipoDocumento(string? raw)
        {
            var t = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(t)) return "TD001";
            if (t.StartsWith("TD", StringComparison.OrdinalIgnoreCase)) return t.ToUpperInvariant();

            var n = t.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(n.Length);
            foreach (var ch in n)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var key = sb.ToString().ToUpperInvariant();

            if (key.Contains("C.I") || key.Contains("CI CIVIL") || key.Contains("CEDULA") || key.Contains("CÉDULA") || key == "CI")
                return "TD001";
            if (key.Contains("RUC") || key.Contains("ENTIDAD"))
                return "TD002";
            if (key.Contains("PASAPORTE"))
                return "TD003";

            return "TD001";
        }

        private static DateTimeOffset? ExtraerExpJwt(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var unix))
                    return DateTimeOffset.FromUnixTimeSeconds(unix);
            }
            catch { /* ignore */ }
            return null;
        }

        private static bool EsBloqueoRadware(string? txt) =>
            !string.IsNullOrEmpty(txt) &&
            (txt.Contains("Radware", StringComparison.OrdinalIgnoreCase)
             || txt.Contains("ShieldSquare", StringComparison.OrdinalIgnoreCase)
             || txt.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
             || txt.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase));

        private static string Preview(string s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= 240 ? s : s[..240] + "...");

        private AndeConsultaResultadoDto MapearResultado(string nis, string situacionTxt, string? ultimasTxt)
        {
            var dto = new AndeConsultaResultadoDto
            {
                Nis = nis,
                FechaConsulta = DateTime.Now,
                Observaciones = "Consulta vía JWT/HTTP (sin Chrome ni Acceder manual)."
            };

            try
            {
                using var doc = JsonDocument.Parse(situacionTxt);
                var root = doc.RootElement;
                if (!root.TryGetProperty("resultado", out var res))
                {
                    dto.Observaciones = "Respuesta ANDE sin resultado.";
                    return dto;
                }

                var nombre = res.TryGetProperty("nombre", out var n) ? n.GetString() ?? "" : "";
                var apellido = res.TryGetProperty("apellido", out var a) ? a.GetString() ?? "" : "";
                dto.Titular = $"{nombre} {apellido}".Trim();

                var tieneNis = res.TryGetProperty("tieneNis", out var tn) &&
                               (tn.ValueKind == JsonValueKind.True || string.Equals(tn.ToString(), "True", StringComparison.OrdinalIgnoreCase));
                if (!tieneNis)
                {
                    dto.Observaciones = "NIS no existe o no asociado a la cuenta.";
                    return dto;
                }

                var tieneDeuda = res.TryGetProperty("tieneDeuda", out var td) &&
                                 (td.ValueKind == JsonValueKind.True || string.Equals(td.ToString(), "True", StringComparison.OrdinalIgnoreCase));

                if (res.TryGetProperty("calculoConsumo", out var calc) && calc.ValueKind == JsonValueKind.Object)
                {
                    if (calc.TryGetProperty("consumo", out var cons) && TryDecimal(cons, out var kw) && kw is >= 0 and < 500_000)
                        dto.ConsumoKw = (int)Math.Round(kw);
                }

                AgregarFacturaSiExiste(dto, res, "facturaDatos", pendientePreferido: true);
                AgregarFacturaSiExiste(dto, res, "facturaPagada", pendientePreferido: false);
                AgregarFacturaSiExiste(dto, res, "facturaPenultima", pendientePreferido: true);

                if (!string.IsNullOrWhiteSpace(ultimasTxt))
                {
                    try
                    {
                        using var uDoc = JsonDocument.Parse(ultimasTxt);
                        if (uDoc.RootElement.TryGetProperty("resultado", out var uRes)
                            && uRes.TryGetProperty("lista", out var lista)
                            && lista.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in lista.EnumerateArray())
                                MapearItemLista(dto, item);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "No se pudo parsear ultimasFacturas");
                    }
                }

                dto.Facturas = dto.Facturas
                    .GroupBy(f =>
                    {
                        var fecha = FormatearFechaFacturaAnde(
                            string.IsNullOrWhiteSpace(f.Vencimiento) ? f.Emision : f.Vencimiento);
                        var estado = (f.Estado ?? "").Contains("Pendiente", StringComparison.OrdinalIgnoreCase)
                            ? "P"
                            : "C";
                        return $"{fecha}|{f.Monto:0}|{estado}";
                    })
                    .Select(g => g
                        .OrderByDescending(x =>
                            !string.IsNullOrWhiteSpace(x.SecNis) && !string.IsNullOrWhiteSpace(x.SecRec))
                        .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.FechaFacturacion))
                        .First())
                    .ToList();

                dto.SaldoDeuda = dto.Facturas
                    .Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))
                    .Sum(f => f.Monto);

                // Si ANDE marca tieneDeuda pero no pudimos clasificar pendientes, usar importe de situacion
                // y forzar la factura más reciente / que coincida con la deuda.
                if (dto.SaldoDeuda == 0 && tieneDeuda)
                {
                    if (TryFindImporte(res, out var imp) && imp > 0)
                        dto.SaldoDeuda = imp;

                    var candidata = dto.Facturas
                        .OrderByDescending(f => f.Emision)
                        .FirstOrDefault(f => dto.SaldoDeuda > 0 && Math.Abs(f.Monto - dto.SaldoDeuda) < 1)
                        ?? dto.Facturas.OrderByDescending(f => f.Emision).FirstOrDefault();

                    if (candidata != null && !candidata.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))
                    {
                        candidata.Estado = "Pendiente de Pago";
                        if (dto.SaldoDeuda <= 0) dto.SaldoDeuda = candidata.Monto;
                    }
                    else if (dto.SaldoDeuda == 0 && candidata != null)
                    {
                        candidata.Estado = "Pendiente de Pago";
                        dto.SaldoDeuda = candidata.Monto;
                    }
                }

                dto.FacturasPendientes = dto.Facturas.Count(f =>
                    f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase));
                dto.EstadoPago = dto.SaldoDeuda > 0 || dto.FacturasPendientes > 0 ? "PENDIENTE DE PAGO" : "AL DÍA";
                dto.EstadoConexion = "OK";
                dto.Vencimiento = dto.Facturas
                    .Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Vencimiento)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                    ?? dto.Facturas.Select(f => f.Vencimiento).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                    ?? "";

                // Si no hay consumo en calculoConsumo, usar el de la última factura mapeada
                if (dto.ConsumoKw == 0)
                {
                    var kwFact = dto.Facturas
                        .Where(f => f.Estado.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))
                        .Select(f => f.ConsumoKw)
                        .FirstOrDefault(k => k.HasValue && k > 0)
                        ?? dto.Facturas.FirstOrDefault(f => f.ConsumoKw.HasValue && f.ConsumoKw > 0)?.ConsumoKw;
                    if (kwFact.HasValue) dto.ConsumoKw = kwFact.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error mapeando situacionActual JWT");
                dto.Observaciones = "Error interpretando respuesta ANDE.";
            }

            return dto;
        }

        private static void AgregarFacturaSiExiste(
            AndeConsultaResultadoDto dto,
            JsonElement res,
            string prop,
            bool pendientePreferido)
        {
            if (!res.TryGetProperty(prop, out var el)) return;
            if (el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return;
            if (el.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(el.GetString())) return;
            if (el.ValueKind != JsonValueKind.Object) return;
            if (!el.EnumerateObject().Any()) return;

            MapearItemLista(dto, el, pendientePreferido);
        }

        private static void MapearItemLista(
            AndeConsultaResultadoDto dto,
            JsonElement item,
            bool? forzarPendiente = null)
        {
            // ANDE anida montos/fechas: otrosImportes.deudaTotal, recibo.*, lectura.consumo
            JsonElement? recibo = item.TryGetProperty("recibo", out var rec) && rec.ValueKind == JsonValueKind.Object
                ? rec
                : null;
            JsonElement? otros = item.TryGetProperty("otrosImportes", out var oi) && oi.ValueKind == JsonValueKind.Object
                ? oi
                : null;
            JsonElement? lectura = null;
            if (item.TryGetProperty("lectura", out var lec))
            {
                if (lec.ValueKind == JsonValueKind.Object)
                    lectura = lec;
                else if (lec.ValueKind == JsonValueKind.Array && lec.GetArrayLength() > 0)
                    lectura = lec[0];
            }

            decimal monto = 0;
            if (otros.HasValue)
            {
                foreach (var key in new[] { "deudaTotal", "totalconComision", "totalConComision", "importe", "monto" })
                {
                    if (otros.Value.TryGetProperty(key, out var v) && TryDecimal(v, out monto) && monto > 0)
                        break;
                }
            }
            if (monto <= 0)
            {
                foreach (var key in new[] { "importe", "monto", "total", "impTotal", "importeTotal", "saldo", "importeFacturado" })
                {
                    if (item.TryGetProperty(key, out var v) && TryDecimal(v, out monto) && monto > 0)
                        break;
                }
            }
            if (monto <= 0 && recibo.HasValue)
            {
                foreach (var key in new[] { "importe", "monto", "total", "importeTotal" })
                {
                    if (recibo.Value.TryGetProperty(key, out var v) && TryDecimal(v, out monto) && monto > 0)
                        break;
                }
            }

            var estadoRaw = "";
            var codigoEstado = "";
            void LeerEstadoDesde(JsonElement el)
            {
                if (string.IsNullOrWhiteSpace(estadoRaw))
                {
                    foreach (var key in new[] { "estado", "descEstado", "estadoPago", "situacion" })
                    {
                        if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                        {
                            var s = (v.GetString() ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(s) && !s.StartsWith("ER", StringComparison.OrdinalIgnoreCase))
                            {
                                estadoRaw = s;
                                break;
                            }
                            // Si solo viene el código en "estado", usarlo como codigoEstado
                            if (!string.IsNullOrWhiteSpace(s) && s.StartsWith("ER", StringComparison.OrdinalIgnoreCase)
                                && string.IsNullOrWhiteSpace(codigoEstado))
                                codigoEstado = s;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(codigoEstado))
                {
                    foreach (var key in new[] { "codigoEstado", "codEstado", "codigo_estado" })
                    {
                        if (el.TryGetProperty(key, out var v))
                        {
                            var s = v.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                codigoEstado = s;
                                break;
                            }
                        }
                    }
                }
            }

            if (recibo.HasValue) LeerEstadoDesde(recibo.Value);
            LeerEstadoDesde(item);

            var estado = NormalizarEstadoAnde(estadoRaw, codigoEstado, forzarPendiente);

            string emision = "", venc = "", fechaPago = "";
            if (recibo.HasValue)
            {
                foreach (var key in new[] { "emision", "fechaEmision", "fechaFacturacion" })
                {
                    if (recibo.Value.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        emision = v.ToString();
                        break;
                    }
                }
                foreach (var key in new[] { "fechaVencimiento", "vencimiento", "vto" })
                {
                    if (recibo.Value.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        venc = v.ToString();
                        break;
                    }
                }
                foreach (var key in new[] { "fechaPago", "fechaCobro", "f_pago", "fechaCancelacion", "fechaCancel", "fechaAbono" })
                {
                    if (recibo.Value.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        fechaPago = v.ToString();
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(emision))
            {
                foreach (var key in new[] { "fechaFacturacion", "f_fact", "fechaEmision", "emision" })
                {
                    if (item.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        emision = v.ToString();
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(venc))
            {
                foreach (var key in new[] { "fechaVencimiento", "f_venc", "vencimiento", "vto" })
                {
                    if (item.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        venc = v.ToString();
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(fechaPago))
            {
                foreach (var key in new[] { "fechaPago", "fechaCobro", "f_pago", "fechaCancelacion", "fechaCancel", "fechaAbono" })
                {
                    if (item.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.ToString()))
                    {
                        fechaPago = v.ToString();
                        break;
                    }
                }
            }

            int? kw = null;
            if (lectura.HasValue)
            {
                foreach (var key in new[] { "consumo", "consumoFacturado", "kw", "kwh" })
                {
                    if (lectura.Value.TryGetProperty(key, out var v) && TryDecimal(v, out var d) && d is >= 0 and < 500_000)
                    {
                        kw = (int)Math.Round(d);
                        break;
                    }
                }
            }
            if (!kw.HasValue)
            {
                foreach (var key in new[] { "consumo", "kw", "kwh", "consumoKwh", "consumoFacturado" })
                {
                    if (item.TryGetProperty(key, out var v) && TryDecimal(v, out var d) && d is >= 0 and < 500_000)
                    {
                        kw = (int)Math.Round(d);
                        break;
                    }
                }
            }

            if (monto <= 0 && string.IsNullOrWhiteSpace(emision) && string.IsNullOrWhiteSpace(venc))
                return;

            string? secNis = TextoCampoJson(item, "sec_nis", "secNis", "secnis", "nroSecNis", "secuenciaNis")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_nis", "secNis", "secnis") : null);
            string? secRec = TextoCampoJson(item, "sec_rec", "secRec", "secrec", "nroRecibo", "nro_recibo", "nroRec")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "sec_rec", "secRec", "nroRecibo", "nro_recibo") : null);
            string? fFact = TextoCampoJson(item, "fechaFacturacion", "f_fact", "fechaEmision", "emision")
                ?? (recibo.HasValue ? TextoCampoJson(recibo.Value, "fechaFacturacion", "f_fact", "fechaEmision", "emision") : null);
            if (string.IsNullOrWhiteSpace(fFact))
                fFact = emision;

            var electronica = true;
            if (item.TryGetProperty("facturaElectronica", out var feEl))
            {
                electronica = feEl.ValueKind == JsonValueKind.True
                    || string.Equals(feEl.ToString(), "true", StringComparison.OrdinalIgnoreCase)
                    || feEl.ToString() == "1";
            }

            dto.Facturas.Add(new AndeFacturaItemDto
            {
                Monto = monto,
                Estado = estado,
                Emision = emision,
                Vencimiento = venc,
                FechaPago = fechaPago,
                ConsumoKw = kw,
                SecNis = secNis,
                SecRec = secRec,
                FechaFacturacion = fFact,
                FacturaElectronica = electronica
            });

            if (kw.HasValue && dto.ConsumoKw == 0)
                dto.ConsumoKw = kw.Value;
        }

        /// <summary>
        /// Códigos de estado ANDE (misma lógica que facturaComponent en el portal).
        /// </summary>
        private static string NormalizarEstadoAnde(string estadoRaw, string codigoEstado, bool? forzarPendiente)
        {
            var code = (codigoEstado ?? "").Trim().ToUpperInvariant();
            // Pendientes de cobro (ER0xx–ER2xx típicos del portal)
            if (!string.IsNullOrEmpty(code) &&
                (code.StartsWith("ER0") || code.StartsWith("ER1") || code.StartsWith("ER2")
                 || code is "ER620"))
                return "Pendiente de Pago";

            // Cobrados (ER3xx)
            if (!string.IsNullOrEmpty(code) && code.StartsWith("ER3"))
                return "Pagado";

            var e = (estadoRaw ?? "").Trim();
            if (e.Contains("Cobrad", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Pagad", StringComparison.OrdinalIgnoreCase))
                return "Pagado";
            if (e.Contains("Pend", StringComparison.OrdinalIgnoreCase)
                || e.Contains("Deud", StringComparison.OrdinalIgnoreCase))
                return "Pendiente de Pago";

            if (forzarPendiente == true) return "Pendiente de Pago";
            if (forzarPendiente == false) return "Pagado";

            // Ambiguo: no asumir Pagado (era el bug del desglose)
            return string.IsNullOrWhiteSpace(e) ? "Pendiente de Pago" : e;
        }

        private static bool TryFindImporte(JsonElement res, out decimal importe)
        {
            importe = 0;
            foreach (var key in new[] { "importe", "monto", "saldo", "deuda", "importeDeuda" })
            {
                if (res.TryGetProperty(key, out var v) && TryDecimal(v, out importe) && importe > 0)
                    return true;
            }

            foreach (var prop in new[] { "facturaDatos", "facturaPagada", "facturaPenultima" })
            {
                if (!res.TryGetProperty(prop, out var fact) || fact.ValueKind != JsonValueKind.Object)
                    continue;
                if (fact.TryGetProperty("otrosImportes", out var otros) && otros.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "deudaTotal", "totalconComision", "totalConComision" })
                    {
                        if (otros.TryGetProperty(key, out var v) && TryDecimal(v, out importe) && importe > 0)
                            return true;
                    }
                }
            }
            return false;
        }

        private static bool TryDecimal(JsonElement el, out decimal value)
        {
            value = 0;
            switch (el.ValueKind)
            {
                case JsonValueKind.Number:
                    return el.TryGetDecimal(out value);
                case JsonValueKind.String:
                    var s = (el.GetString() ?? "")
                        .Replace("Gs.", "", StringComparison.OrdinalIgnoreCase)
                        .Replace(" ", "")
                        .Trim();
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("es-PY"), out value))
                        return true;
                    return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }
    }
}
