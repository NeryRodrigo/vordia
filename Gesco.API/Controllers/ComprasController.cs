using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly IComprasService _comprasService;

        public ComprasController(IComprasService comprasService)
        {
            _comprasService = comprasService;
        }

        [HttpPost("empresa/{empresaId}/factura")]
        public async Task<ActionResult> RegistrarFacturaCompra(int empresaId, [FromBody] RegistrarCompraRequest request)
        {
            try
            {
                var id = await _comprasService.RegistrarCompraAsync(empresaId, request);
                return Ok(new { compraId = id, mensaje = "Factura de proveedor registrada y stock actualizado con éxito." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("empresa/{empresaId}/proveedores")]
        public async Task<ActionResult> GetProveedores(int empresaId)
        {
            var proveedores = await _comprasService.GetProveedoresAsync(empresaId);
            return Ok(proveedores);
        }

        [HttpGet("empresa/{empresaId}/historial")]
        public async Task<ActionResult> GetHistorialCompras(int empresaId)
        {
            var compras = await _comprasService.GetComprasAsync(empresaId);
            return Ok(compras);
        }
    }
}
