using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Gesco.Application.Services;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AndeController : ControllerBase
    {
        private readonly IAndeScrapingService _andeScrapingService;
        private readonly IAndeJwtConsultaService _andeJwtConsultaService;
        private readonly IAndeCredentialsStore _creds;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;

        public AndeController(
            IAndeScrapingService andeScrapingService,
            IAndeJwtConsultaService andeJwtConsultaService,
            IAndeCredentialsStore creds,
            IConfiguration config,
            IHttpClientFactory httpFactory)
        {
            _andeScrapingService = andeScrapingService;
            _andeJwtConsultaService = andeJwtConsultaService;
            _creds = creds;
            _config = config;
            _httpFactory = httpFactory;
        }

        /// <summary>Indica si hay sesión ANDE activa en memoria (esta corrida / hasta logout).</summary>
        [HttpGet("credenciales/estado")]
        public IActionResult CredencialesEstado()
        {
            return Ok(new
            {
                configurado = _creds.EstaConfigurado(),
                documentoEnmascarado = _creds.DocumentoEnmascarado()
            });
        }

        /// <summary>
        /// Guarda usuario/clave ANDE solo en memoria de esta sesión y valida el login.
        /// Se borran al cerrar sesión Gesco (DELETE credenciales).
        /// </summary>
        [HttpPost("credenciales")]
        public async Task<IActionResult> GuardarCredenciales(
            [FromBody] AndeCredencialesRequest? body,
            CancellationToken ct)
        {
            try
            {
                var (ok, mensaje) = await _creds.GuardarYValidarAsync(
                    body?.Documento ?? "",
                    body?.Password ?? "",
                    body?.TipoDocumento,
                    ct);

                if (!ok)
                    return BadRequest(new { ok = false, mensaje, requiereCredenciales = true });

                return Ok(new
                {
                    ok = true,
                    mensaje,
                    documentoEnmascarado = _creds.DocumentoEnmascarado()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        /// <summary>Limpia CI/clave ANDE de sesión e invalida JWT (llamar al logout Gesco).</summary>
        [HttpDelete("credenciales")]
        [HttpPost("credenciales/cerrar")]
        public IActionResult CerrarCredencialesSesion()
        {
            _creds.LimpiarSesion();
            return Ok(new { ok = true, mensaje = "Sesión ANDE limpiada." });
        }

        /// <summary>
        /// PDF oficial ANDE. En GCP el WAF de ANDE bloquea el KUDE: se pide a un worker en Paraguay.
        /// </summary>
        [HttpGet("factura-pdf/{nis}")]
        public async Task<IActionResult> GetFacturaPdf(
            string nis,
            [FromQuery] string inquilino = "",
            [FromQuery] string duplex = "",
            [FromQuery] string? secNis = null,
            [FromQuery] string? secRec = null,
            [FromQuery] string? fFact = null,
            [FromQuery] bool? electronica = null,
            [FromQuery] string? origen = null,
            [FromHeader(Name = "X-Ande-Recapt")] string? recapt = null)
        {
            try
            {
                if (!_creds.EstaConfigurado())
                    return StatusCode(401, new
                    {
                        ok = false,
                        requiereCredenciales = true,
                        mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta."
                    });

                await _andeJwtConsultaService.AsegurarJwtAsync();

                var workerUrl = (_config["AndeConfig:PdfWorkerUrl"] ?? "").Trim().TrimEnd('/');
                var workerKey = (_config["AndeConfig:PdfWorkerKey"] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(workerUrl) && !string.IsNullOrWhiteSpace(workerKey))
                {
                    var pdfWorker = await PedirKudeAlWorkerAsync(
                        workerUrl, workerKey, nis, secNis, secRec, fFact, electronica, origen);
                    if (pdfWorker is { Length: > 0 })
                        return File(pdfWorker, "application/pdf", $"Factura_ANDE_{nis}.pdf");
                }

                var pdfBytes = await _andeJwtConsultaService.DescargarFacturaPdfOficialAsync(
                    nis, secNis, secRec, fFact, electronica, origen, recapt);

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return StatusCode(503, new
                    {
                        ok = false,
                        andeConFallas = true,
                        mensaje = "Consultá la página de ANDE con el NIS correspondiente para descargar la factura consultada."
                    });

                return File(pdfBytes, "application/pdf", $"Factura_ANDE_{nis}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar a la ANDE: {ex.Message}");
            }
        }

        /// <summary>
        /// Worker en Paraguay: GET del KUDE oficial. No es un puente a la PC del desarrollador.
        /// </summary>
        [HttpGet("worker/factura-pdf/{nis}")]
        public async Task<IActionResult> WorkerFacturaPdf(
            string nis,
            [FromQuery] string? secNis = null,
            [FromQuery] string? secRec = null,
            [FromQuery] string? fFact = null,
            [FromQuery] bool? electronica = null,
            [FromQuery] string? origen = null,
            [FromHeader(Name = "X-Ande-Worker-Key")] string? workerKey = null,
            [FromHeader(Name = "X-Ande-Kwfx")] string? kwfx = null,
            [FromHeader(Name = "X-Ande-Jwt")] string? jwt = null)
        {
            var esperada = (_config["AndeConfig:PdfWorkerKey"] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(esperada)
                || !string.Equals(esperada, (workerKey ?? "").Trim(), StringComparison.Ordinal))
                return Unauthorized(new { ok = false, mensaje = "Worker key inválida." });

            var pedido = _andeJwtConsultaService.ArmarPedidoComprobanteOficial(
                nis, secNis, secRec, fFact, electronica, origen);
            var url = pedido.Url;
            if (string.IsNullOrWhiteSpace(url))
                return StatusCode(503, new { ok = false, andeConFallas = true, mensaje = "Consultá la página de ANDE con el NIS correspondiente para descargar la factura consultada." });

            var pdf = await _andeJwtConsultaService.BajarKudePorGetAsync(
                url, kwfx ?? _andeJwtConsultaService.GetKwfxTokenActual(),
                jwt ?? _andeJwtConsultaService.GetJwtActual());
            if (pdf == null || pdf.Length == 0)
                return StatusCode(503, new { ok = false, andeConFallas = true, mensaje = "Consultá la página de ANDE con el NIS correspondiente para descargar la factura consultada." });

            return File(pdf, "application/pdf", $"Factura_ANDE_{nis}.pdf");
        }

        private async Task<byte[]?> PedirKudeAlWorkerAsync(
            string workerBase,
            string workerKey,
            string nis,
            string? secNis,
            string? secRec,
            string? fFact,
            bool? electronica,
            string? origen)
        {
            try
            {
                var qs = new List<string>();
                if (!string.IsNullOrWhiteSpace(secNis)) qs.Add("secNis=" + Uri.EscapeDataString(secNis));
                if (!string.IsNullOrWhiteSpace(secRec)) qs.Add("secRec=" + Uri.EscapeDataString(secRec));
                if (!string.IsNullOrWhiteSpace(fFact)) qs.Add("fFact=" + Uri.EscapeDataString(fFact));
                if (electronica == false) qs.Add("electronica=false");
                if (!string.IsNullOrWhiteSpace(origen)) qs.Add("origen=" + Uri.EscapeDataString(origen));

                var url = $"{workerBase}/api/Ande/worker/factura-pdf/{Uri.EscapeDataString(nis)}";
                if (qs.Count > 0)
                    url += "?" + string.Join("&", qs);

                var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(90);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("X-Ande-Worker-Key", workerKey);
                var kwfx = _andeJwtConsultaService.GetKwfxTokenActual();
                var jwt = _andeJwtConsultaService.GetJwtActual();
                if (!string.IsNullOrWhiteSpace(kwfx))
                    req.Headers.TryAddWithoutValidation("X-Ande-Kwfx", kwfx);
                if (!string.IsNullOrWhiteSpace(jwt))
                    req.Headers.TryAddWithoutValidation("X-Ande-Jwt", jwt);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                using var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                    return null;
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                return bytes is { Length: >= 5 } && bytes[0] == (byte)'%' && bytes[1] == (byte)'P'
                    ? bytes
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Datos para pedir el KUDE desde el navegador del usuario (IP de Paraguay),
        /// cuando el servidor no puede bajar el PDF.
        /// </summary>
        [HttpGet("comprobante-oficial/{nis}")]
        public async Task<IActionResult> GetComprobanteOficial(
            string nis,
            [FromQuery] string? secNis = null,
            [FromQuery] string? secRec = null,
            [FromQuery] string? fFact = null,
            [FromQuery] bool? electronica = null,
            [FromQuery] string? origen = null)
        {
            if (!_creds.EstaConfigurado())
                return StatusCode(401, new
                {
                    ok = false,
                    requiereCredenciales = true,
                    mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta."
                });

            await _andeJwtConsultaService.AsegurarJwtAsync();
            return Ok(_andeJwtConsultaService.ArmarPedidoComprobanteOficial(
                nis, secNis, secRec, fFact, electronica, origen));
        }

        [HttpGet("consumo/{nis}")]
        public async Task<IActionResult> GetConsumoReal(string nis)
        {
            try
            {
                var resultado = await _andeScrapingService.ConsultarConsumoAsync(nis);
                return Ok(new { Nis = nis, Monto = resultado.Deuda, Kw = resultado.Kw });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener consumo: {ex.Message}");
            }
        }

        [HttpPost("asegurar-sesion")]
        [HttpGet("asegurar-sesion")]
        public async Task<IActionResult> AsegurarSesion()
        {
            try
            {
                if (!_creds.EstaConfigurado())
                    return StatusCode(401, new
                    {
                        ok = false,
                        requiereCredenciales = true,
                        mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta."
                    });

                var (ok, mensaje, yaEstaba) = await _andeScrapingService.AsegurarSesionAsync();
                if (!ok)
                    return StatusCode(408, new { ok = false, mensaje, yaEstabaLogueado = yaEstaba });

                return Ok(new { ok = true, mensaje, yaEstabaLogueado = yaEstaba });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = $"Error al abrir login ANDE: {ex.Message}" });
            }
        }

        /// <summary>Login HTTP ANDE (JWT/kwfxtoken). No usa el flujo Acceder/Puppeteer de NIS.</summary>
        [HttpPost("jwt/asegurar")]
        [HttpGet("jwt/asegurar")]
        public async Task<IActionResult> AsegurarJwt()
        {
            try
            {
                if (!_creds.EstaConfigurado())
                    return StatusCode(401, new
                    {
                        ok = false,
                        requiereCredenciales = true,
                        mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta.",
                        renovado = false
                    });

                var (ok, mensaje, renovado) = await _andeJwtConsultaService.AsegurarJwtAsync();
                if (!ok)
                    return StatusCode(401, new { ok = false, mensaje, renovado, requiereCredenciales = !_creds.EstaConfigurado() });
                return Ok(new { ok = true, mensaje, renovado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = ex.Message });
            }
        }

        /// <summary>Consulta saldo NIS vía sesión JWT/miCuenta. No toca Puppeteer NIS.</summary>
        [HttpGet("jwt/consultar/{nis}")]
        public async Task<IActionResult> ConsultarViaJwt(string nis)
        {
            try
            {
                if (!_creds.EstaConfigurado())
                    return StatusCode(401, new
                    {
                        ok = false,
                        requiereCredenciales = true,
                        mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta."
                    });

                var resultado = await _andeJwtConsultaService.ConsultarSaldoAsync(nis);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = $"Error consulta JWT ANDE: {ex.Message}" });
            }
        }

        /// <summary>
        /// Consulta varios NIS con la misma sesión JWT (secuencial).
        /// Usado por la grilla "Consulta de Medidores ANDE" del panel.
        /// </summary>
        [HttpPost("jwt/consultar-lote")]
        public async Task<IActionResult> ConsultarLoteViaJwt([FromBody] AndeConsultarLoteRequest? body)
        {
            try
            {
                if (!_creds.EstaConfigurado())
                    return StatusCode(401, new
                    {
                        ok = false,
                        requiereCredenciales = true,
                        mensaje = "Faltan credenciales ANDE. Configurá usuario y contraseña de Mi Cuenta."
                    });

                var nisList = (body?.NisList ?? new List<string>())
                    .Select(n => new string((n ?? "").Where(char.IsDigit).ToArray()))
                    .Where(n => n.Length > 0)
                    .Distinct()
                    .ToList();

                if (nisList.Count == 0)
                    return BadRequest(new { ok = false, mensaje = "Enviá al menos un NIS." });

                var (okSesion, mensajeSesion, renovado) = await _andeJwtConsultaService.AsegurarJwtAsync();
                if (!okSesion)
                    return StatusCode(401, new
                    {
                        ok = false,
                        mensaje = mensajeSesion,
                        renovado,
                        requiereCredenciales = !_creds.EstaConfigurado()
                    });

                var resultados = new List<object>();
                foreach (var nis in nisList)
                {
                    var r = await _andeJwtConsultaService.ConsultarSaldoAsync(nis);
                    resultados.Add(r);
                }

                return Ok(new
                {
                    ok = true,
                    mensaje = $"Consulta lote OK ({resultados.Count} NIS). {mensajeSesion}",
                    renovado,
                    cantidad = resultados.Count,
                    resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, mensaje = $"Error consulta lote JWT ANDE: {ex.Message}" });
            }
        }

        [HttpGet("consultar-estado/{nis}")]
        public async Task<IActionResult> GetEstadoAnde(string nis)
        {
            try
            {
                var resultado = await _andeScrapingService.ConsultarEstadoAndeAsync(nis);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al consultar estado en ANDE: {ex.Message}");
            }
        }
    }

    public class AndeConsultarLoteRequest
    {
        public List<string>? NisList { get; set; }
    }

    public class AndeCredencialesRequest
    {
        public string? Documento { get; set; }
        public string? Password { get; set; }
        public string? TipoDocumento { get; set; }
    }
}
