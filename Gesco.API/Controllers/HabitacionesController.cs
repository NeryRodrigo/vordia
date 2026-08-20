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

        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<Habitacion>>> GetHabitacionesPorEmpresa(int empresaId)
        {
            var habitaciones = await _habitacionService.GetAllByEmpresaAsync(empresaId);
            return Ok(habitaciones);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Habitacion>> GetHabitacion(int id)
        {
            var habitacion = await _habitacionService.GetByIdAsync(id);
            if (habitacion == null) return NotFound();
            return Ok(habitacion);
        }

        [HttpPost]
        public async Task<ActionResult<Habitacion>> PostHabitacion([FromBody] Habitacion habitacion)
        {
            if (habitacion == null)
                return BadRequest(new { mensaje = "Body vacío" });

            // Evitar validación/ciclo de navegaciones en POST
            habitacion.Empresa = null;
            habitacion.HabitacionId = 0;

            if (habitacion.EmpresaId <= 0)
                return BadRequest(new { mensaje = "EmpresaId es obligatorio" });
            if (string.IsNullOrWhiteSpace(habitacion.Nombre))
                return BadRequest(new { mensaje = "Nombre es obligatorio" });
            if (string.IsNullOrWhiteSpace(habitacion.Tipo))
                habitacion.Tipo = "Habitación";
            if (string.IsNullOrWhiteSpace(habitacion.Estado))
                habitacion.Estado = "Disponible";

            ModelState.Clear();
            var nuevaHabitacion = await _habitacionService.CreateAsync(habitacion);
            return CreatedAtAction(nameof(GetHabitacion), new { id = nuevaHabitacion.HabitacionId }, nuevaHabitacion);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutHabitacion(int id, [FromBody] Habitacion habitacion)
        {
            if (habitacion == null || id != habitacion.HabitacionId)
                return BadRequest(new { mensaje = "Id inválido" });

            habitacion.Empresa = null;
            ModelState.Clear();
            await _habitacionService.UpdateAsync(habitacion);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHabitacion(int id)
        {
            try
            {
                var exists = await _habitacionService.GetByIdAsync(id);
                if (exists == null) return NotFound(new { mensaje = "Habitación no encontrada" });

                await _habitacionService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "No se pudo eliminar la habitación", detalle = ex.Message });
            }
        }
    }
}
