using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly IInventarioService _inventarioService;

        public InventarioController(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<List<ProductoDto>>> GetProductos(int empresaId)
        {
            var productos = await _inventarioService.GetProductosAsync(empresaId);
            return Ok(productos);
        }

        [HttpGet("empresa/{empresaId}/producto/{productoId}")]
        public async Task<ActionResult<ProductoDto>> GetProducto(int empresaId, int productoId)
        {
            try
            {
                var producto = await _inventarioService.GetProductoByIdAsync(empresaId, productoId);
                return Ok(producto);
            }
            catch (System.Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("empresa/{empresaId}")]
        public async Task<ActionResult> CreateProducto(int empresaId, [FromBody] CreateProductoRequest request)
        {
            var id = await _inventarioService.CreateProductoAsync(empresaId, request);
            return CreatedAtAction(nameof(GetProducto), new { empresaId = empresaId, productoId = id }, new { id = id });
        }

        [HttpPut("empresa/{empresaId}/producto/{productoId}")]
        public async Task<ActionResult> UpdateProducto(int empresaId, int productoId, [FromBody] UpdateProductoRequest request)
        {
            if (productoId != request.ProductoId)
                return BadRequest("El ID de la ruta no coincide con el cuerpo de la petición.");

            try
            {
                await _inventarioService.UpdateProductoAsync(empresaId, request);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("empresa/{empresaId}/producto/{productoId}")]
        public async Task<ActionResult> DeleteProducto(int empresaId, int productoId)
        {
            var success = await _inventarioService.DeleteProductoAsync(empresaId, productoId);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("empresa/{empresaId}/producto/{productoId}/ajustar-stock")]
        public async Task<ActionResult> AjustarStock(int empresaId, int productoId, [FromBody] decimal nuevoStock)
        {
            try
            {
                await _inventarioService.AjustarStockAsync(empresaId, productoId, nuevoStock);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
