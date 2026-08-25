using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/Crm")]
    [ApiController]
    public class CrmOperacionController : ControllerBase
    {
        private readonly ICrmOperacionService _ops;

        public CrmOperacionController(ICrmOperacionService ops)
        {
            _ops = ops;
        }

        [HttpGet("tareas")]
        public async Task<IActionResult> ListarTareas([FromQuery] int empresaId, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarTareasAsync(empresaId, estado));
        }

        [HttpPost("tareas")]
        public async Task<IActionResult> CrearTarea([FromBody] CrmTareaUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo))
                return BadRequest(new { mensaje = "Titulo es obligatorio" });
            return Ok(await _ops.CrearTareaAsync(request));
        }

        [HttpPut("tareas/{tareaId:int}")]
        public async Task<IActionResult> ActualizarTarea(int tareaId, [FromBody] CrmTareaUpsertRequest request)
        {
            var updated = await _ops.ActualizarTareaAsync(tareaId, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPatch("tareas/{tareaId:int}/estado")]
        public async Task<IActionResult> CambiarEstadoTarea(int tareaId, [FromQuery] int empresaId, [FromBody] CrmEstadoSimpleRequest body)
        {
            var updated = await _ops.CambiarEstadoTareaAsync(tareaId, empresaId, body.Estado);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("tareas/{tareaId:int}")]
        public async Task<IActionResult> EliminarTarea(int tareaId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarTareaAsync(tareaId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("socios")]
        public async Task<IActionResult> ListarSocios([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ListarSociosAsync(empresaId));
        }

        [HttpPost("socios")]
        public async Task<IActionResult> CrearSocio([FromBody] CrmSocioUpsertRequest request)
        {
            return Ok(await _ops.UpsertSocioAsync(request));
        }

        [HttpPut("socios/{socioId:int}")]
        public async Task<IActionResult> ActualizarSocio(int socioId, [FromBody] CrmSocioUpsertRequest request)
        {
            try
            {
                return Ok(await _ops.UpsertSocioAsync(request, socioId));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("socios/{socioId:int}")]
        public async Task<IActionResult> EliminarSocio(int socioId, [FromQuery] int empresaId)
        {
            var ok = await _ops.EliminarSocioAsync(socioId, empresaId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("flujo-caja")]
        public async Task<IActionResult> FlujoCaja([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ops.ObtenerFlujoCajaAsync(empresaId));
        }
    }
}
