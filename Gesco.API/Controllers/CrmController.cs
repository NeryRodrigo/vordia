using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrmController : ControllerBase
    {
        private readonly ICrmService _crm;

        public CrmController(ICrmService crm)
        {
            _crm = crm;
        }

        [HttpGet("estados")]
        public IActionResult GetEstados() => Ok(CrmService.EstadosPipeline);

        [HttpGet("clientes")]
        public async Task<IActionResult> Listar(
            [FromQuery] int empresaId,
            [FromQuery] string? estado = null,
            [FromQuery] string? q = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var list = await _crm.ListarClientesAsync(empresaId, estado, q);
            return Ok(list);
        }

        [HttpGet("clientes/{clienteId:int}")]
        public async Task<IActionResult> Obtener(int clienteId, [FromQuery] int empresaId)
        {
            var item = await _crm.ObtenerClienteAsync(clienteId, empresaId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("clientes")]
        public async Task<IActionResult> Crear([FromBody] CrmClienteUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RazonSocial))
                return BadRequest(new { mensaje = "RazonSocial es obligatoria" });
            var created = await _crm.CrearClienteAsync(request);
            return Ok(created);
        }

        [HttpPut("clientes/{clienteId:int}")]
        public async Task<IActionResult> Actualizar(int clienteId, [FromBody] CrmClienteUpsertRequest request)
        {
            var updated = await _crm.ActualizarClienteAsync(clienteId, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("clientes/{clienteId:int}")]
        public async Task<IActionResult> Eliminar(int clienteId, [FromQuery] int empresaId)
        {
            var ok = await _crm.EliminarClienteAsync(clienteId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPatch("clientes/{clienteId:int}/estado")]
        public async Task<IActionResult> CambiarEstado(
            int clienteId,
            [FromQuery] int empresaId,
            [FromBody] CrmCambiarEstadoRequest request)
        {
            try
            {
                var updated = await _crm.CambiarEstadoAsync(clienteId, empresaId, request);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("clientes/{clienteId:int}/historial")]
        public async Task<IActionResult> Historial(int clienteId, [FromQuery] int empresaId)
        {
            return Ok(await _crm.ListarHistorialAsync(clienteId, empresaId));
        }

        [HttpPost("clientes/{clienteId:int}/historial")]
        public async Task<IActionResult> AgregarHistorial(int clienteId, [FromBody] CrmHistorialCreateRequest request)
        {
            try
            {
                var h = await _crm.AgregarHistorialAsync(clienteId, request);
                return Ok(h);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> Pipeline([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _crm.ObtenerPipelineAsync(empresaId));
        }
    }
}
