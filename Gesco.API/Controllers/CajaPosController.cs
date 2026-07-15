using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaPosController : ControllerBase
    {
        private readonly ICajaPosService _cajaPosService;

        public CajaPosController(ICajaPosService cajaPosService)
        {
            _cajaPosService = cajaPosService;
        }

        [HttpPost("empresa/{empresaId}/venta")]
        public async Task<ActionResult> RealizarVenta(int empresaId, [FromBody] RealizarVentaRequest request)
        {
            try
            {
                var id = await _cajaPosService.RealizarVentaAsync(empresaId, request);
                return Ok(new { ventaId = id, mensaje = "Venta procesada con éxito y stock descontado." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("empresa/{empresaId}/siguiente-factura")]
        public async Task<ActionResult> GetSiguienteFactura(int empresaId)
        {
            try
            {
                var siguiente = await _cajaPosService.GetSiguienteFacturaAsync(empresaId);
                return Ok(new { siguienteFactura = siguiente });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("venta/{ventaId}/pdf")]
        public async Task<IActionResult> DescargarFacturaPdf(int ventaId)
        {
            try
            {
                var bytes = await _cajaPosService.GenerarFacturaPdfAsync(ventaId);
                return File(bytes, "application/pdf", $"Factura_GESCO_Venta_{ventaId}.pdf");
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("compra/{compraId}/pdf")]
        public async Task<IActionResult> DescargarCompraPdf(int compraId)
        {
            try
            {
                var bytes = await _cajaPosService.GenerarCompraPdfAsync(compraId);
                return File(bytes, "application/pdf", $"Comprobante_Egreso_GESCO_{compraId}.pdf");
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
