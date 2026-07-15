using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HabitacionesController : ControllerBase
    {
        private readonly IHabitacionService _habitacionService;

        public HabitacionesController(IHabitacionService habitacionService)
        {
            _habitacionService = habitacionService;
        }

        // GET: api/Habitaciones/empresa/5
        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<Habitacion>>> GetHabitacionesPorEmpresa(int empresaId)
        {
            var habitaciones = await _habitacionService.GetAllByEmpresaAsync(empresaId);
            return Ok(habitaciones);
        }

        // GET: api/Habitaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Habitacion>> GetHabitacion(int id)
        {
            var habitacion = await _habitacionService.GetByIdAsync(id);

            if (habitacion == null)
            {
                return NotFound();
            }

            return Ok(habitacion);
        }

        // POST: api/Habitaciones
        [HttpPost]
        public async Task<ActionResult<Habitacion>> PostHabitacion(Habitacion habitacion)
        {
            var nuevaHabitacion = await _habitacionService.CreateAsync(habitacion);
            return CreatedAtAction(nameof(GetHabitacion), new { id = nuevaHabitacion.HabitacionId }, nuevaHabitacion);
        }

        // PUT: api/Habitaciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHabitacion(int id, Habitacion habitacion)
        {
            if (id != habitacion.HabitacionId)
            {
                return BadRequest();
            }

            await _habitacionService.UpdateAsync(habitacion);
            return NoContent();
        }

        // DELETE: api/Habitaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHabitacion(int id)
        {
            await _habitacionService.DeleteAsync(id);
            return NoContent();
        }
    }
}
