using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActividadesController : ControllerBase
    {
        private readonly IActividadService _service;

        public ActividadesController(IActividadService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Actividad>>> GetActividades([FromQuery] int empresaId)
        {
            var list = await _service.ObtenerActividadesAsync(empresaId);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Actividad>> GetActividad(int id)
        {
            var actividad = await _service.ObtenerActividadPorIdAsync(id);
            if (actividad == null) return NotFound();
            return Ok(actividad);
        }

        [HttpPost]
        public async Task<ActionResult<Actividad>> PostActividad([FromBody] Actividad actividad)
        {
            var created = await _service.CrearActividadAsync(actividad);
            return CreatedAtAction(nameof(GetActividad), new { id = created.ActividadId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutActividad(int id, [FromBody] Actividad actividad)
        {
            if (id != actividad.ActividadId)
            {
                return BadRequest("El ID de la URL no coincide con el ID de la actividad.");
            }

            var ok = await _service.ActualizarActividadAsync(actividad);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteActividad(int id)
        {
            var ok = await _service.EliminarActividadAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
