using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GastosMantenimientoController : ControllerBase
    {
        private readonly IGastoMantenimientoService _service;

        public GastosMantenimientoController(IGastoMantenimientoService service)
        {
            _service = service;
        }

        /// <summary>Gastos de luz, agua y basura por unidad. Filtro opcional: tipo=Luz|Agua|Basura</summary>
        [HttpGet("unidad/{unidadId}/servicios")]
        public async Task<ActionResult<IReadOnlyList<GastoServicioMantenimientoDto>>> GetServiciosPorUnidad(int unidadId, [FromQuery] string? tipo = null)
        {
            var list = await _service.GetServiciosByUnidadAsync(unidadId, tipo);
            return Ok(list);
        }

        [HttpGet("servicios/{id:int}")]
        public async Task<ActionResult<GastoServicioMantenimientoDto>> GetServicio(int id)
        {
            var dto = await _service.GetServicioByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost("servicios")]
        public async Task<ActionResult<GastoServicioMantenimientoDto>> PostServicio([FromBody] GastoServicioMantenimientoRequest body)
        {
            try
            {
                var created = await _service.CreateServicioAsync(body);
                return CreatedAtAction(nameof(GetServicio), new { id = created.GastoId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("servicios/{id:int}")]
        public async Task<ActionResult<GastoServicioMantenimientoDto>> PutServicio(int id, [FromBody] GastoServicioMantenimientoRequest body)
        {
            try
            {
                var updated = await _service.UpdateServicioAsync(id, body);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("servicios/{id:int}")]
        public async Task<IActionResult> DeleteServicio(int id)
        {
            var ok = await _service.DeleteServicioAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
