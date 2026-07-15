using System;
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

        public AndeController(IAndeScrapingService andeScrapingService)
        {
            _andeScrapingService = andeScrapingService;
        }

        [HttpGet("factura-pdf/{nis}")]
        public async Task<IActionResult> GetFacturaPdf(string nis, [FromQuery] string inquilino = "", [FromQuery] string duplex = "")
        {
            try
            {
                var pdfBytes = await _andeScrapingService.DescargarFacturaPdfAsync(nis, inquilino, duplex);
                
                if (pdfBytes == null || pdfBytes.Length == 0)
                    return NotFound("No se pudo obtener el PDF oficial de la ANDE para este NIS.");

                return File(pdfBytes, "application/pdf", $"Factura_ANDE_{nis}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar a la ANDE: {ex.Message}");
            }
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
}
