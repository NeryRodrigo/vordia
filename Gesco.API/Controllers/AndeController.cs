using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        public AndeController(
            IAndeScrapingService andeScrapingService,
            IAndeJwtConsultaService andeJwtConsultaService,
            IAndeCredentialsStore creds)
        {
            _andeScrapingService = andeScrapingService;
            _andeJwtConsultaService = andeJwtConsultaService;
            _creds = creds;
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
        /// PDF oficial ANDE. Login JWT (como relogin) + Chrome con CLAVE_SITIO_WEB correcta + XHR.
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
                var pdfBytes = await _andeJwtConsultaService.DescargarFacturaPdfOficialAsync(
                    nis, secNis, secRec, fFact, electronica, origen, recapt);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    var kwfx = _andeJwtConsultaService.GetKwfxTokenActual();
                    pdfBytes = await _andeScrapingService.DescargarFacturaPdfOficialPortalAsync(
                        nis, null, secNis, secRec, fFact, electronica ?? true, kwfx, origen);
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return StatusCode(503, new
                    {
                        ok = false,
                        andeConFallas = true,
                        mensaje = "ANDE con fallas"
                    });

                return File(pdfBytes, "application/pdf", $"Factura_ANDE_{nis}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar a la ANDE: {ex.Message}");
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
