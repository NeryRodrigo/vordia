using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/Crm")]
    [ApiController]
    public class CrmFinanzasController : ControllerBase
    {
        private readonly ICrmFinanzasService _finanzas;

        public CrmFinanzasController(ICrmFinanzasService finanzas)
        {
            _finanzas = finanzas;
        }

        [HttpGet("contratos")]
        public async Task<IActionResult> ListarContratos([FromQuery] int empresaId, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _finanzas.ListarContratosAsync(empresaId, estado));
        }

        [HttpGet("contratos/{contratoId:int}")]
        public async Task<IActionResult> ObtenerContrato(int contratoId, [FromQuery] int empresaId)
        {
            var item = await _finanzas.ObtenerContratoAsync(contratoId, empresaId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("contratos")]
        public async Task<IActionResult> CrearContrato([FromBody] CrmContratoUpsertRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SistemaServicio))
                    return BadRequest(new { mensaje = "SistemaServicio es obligatorio" });
                return Ok(await _finanzas.CrearContratoAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("contratos/{contratoId:int}")]
        public async Task<IActionResult> ActualizarContrato(int contratoId, [FromBody] CrmContratoUpsertRequest request)
        {
            try
            {
                var updated = await _finanzas.ActualizarContratoAsync(contratoId, request);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("contratos/{contratoId:int}")]
        public async Task<IActionResult> EliminarContrato(int contratoId, [FromQuery] int empresaId)
        {
            var ok = await _finanzas.EliminarContratoAsync(contratoId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("pagos")]
        public async Task<IActionResult> ListarPagos([FromQuery] int empresaId, [FromQuery] int? contratoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _finanzas.ListarPagosAsync(empresaId, contratoId));
        }

        [HttpPost("pagos")]
        public async Task<IActionResult> RegistrarPago([FromBody] CrmPagoCreateRequest request)
        {
            try
            {
                if (request.Monto <= 0)
                    return BadRequest(new { mensaje = "Monto debe ser mayor a 0" });
                return Ok(await _finanzas.RegistrarPagoAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("ingresos")]
        public async Task<IActionResult> Ingresos([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _finanzas.ObtenerIngresosAsync(empresaId));
        }
    }
}
