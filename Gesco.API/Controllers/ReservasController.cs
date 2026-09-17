using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _reservaService;

        public ReservasController(IReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<Reserva>>> GetReservasPorEmpresa(int empresaId)
        {
            var reservas = await _reservaService.GetAllByEmpresaAsync(empresaId);
            return Ok(reservas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Reserva>> GetReserva(int id)
        {
            var reserva = await _reservaService.GetByIdAsync(id);
            if (reserva == null) return NotFound();
            return Ok(reserva);
        }

        [HttpPost]
        public async Task<ActionResult<Reserva>> PostReserva([FromBody] Reserva reserva)
        {
            if (reserva == null) return BadRequest(new { mensaje = "Body vacío" });

            reserva.Empresa = null;
            reserva.Habitacion = null;
            reserva.Huesped = null;
            reserva.Usuario = null;
            reserva.ReservaId = 0;

            if (reserva.EmpresaId <= 0 || reserva.HabitacionId <= 0 || reserva.HuespedId <= 0)
                return BadRequest(new { mensaje = "EmpresaId, HabitacionId y HuespedId son obligatorios" });
            if (string.IsNullOrWhiteSpace(reserva.Estado))
                reserva.Estado = "Pendiente";

            ModelState.Clear();
            var nuevaReserva = await _reservaService.CreateAsync(reserva);
            return CreatedAtAction(nameof(GetReserva), new { id = nuevaReserva.ReservaId }, nuevaReserva);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReserva(int id, [FromBody] Reserva reserva)
        {
            if (reserva == null || id != reserva.ReservaId)
                return BadRequest(new { mensaje = "Id inválido" });

            reserva.Empresa = null;
            reserva.Habitacion = null;
            reserva.Huesped = null;
            reserva.Usuario = null;
            ModelState.Clear();
            await _reservaService.UpdateAsync(reserva);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            await _reservaService.DeleteAsync(id);
            return NoContent();
        }
    }
}
