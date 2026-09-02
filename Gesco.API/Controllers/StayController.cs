using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// VORDIA STAY — API del vertical de alojamiento.
    /// Reutiliza Habitaciones/Reservas existentes; no modifica Habitat.
    /// </summary>
    [ApiController]
    [Route("api/Stay")]
    public class StayController : ControllerBase
    {
        private readonly IStayService _stay;

        public StayController(IStayService stay)
        {
            _stay = stay;
        }

        // --- Tarifas ---
        [HttpGet("tarifas")]
        public async Task<IActionResult> GetTarifas([FromQuery] int empresaId, [FromQuery] bool soloActivas = true)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _stay.GetTarifasAsync(empresaId, soloActivas));
        }

        [HttpPost("tarifas")]
        public async Task<IActionResult> UpsertTarifa([FromBody] StayTarifa body)
        {
            if (body == null || body.EmpresaId <= 0) return BadRequest(new { mensaje = "EmpresaId requerido" });
            try
            {
                var saved = await _stay.UpsertTarifaAsync(body);
                return Ok(saved);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("tarifas/{id}")]
        public async Task<IActionResult> DesactivarTarifa(int id, [FromQuery] int empresaId)
        {
            var ok = await _stay.DesactivarTarifaAsync(id, empresaId);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        // --- Pagos ---
        [HttpGet("pagos")]
        public async Task<IActionResult> GetPagos([FromQuery] int empresaId, [FromQuery] int reservaId)
        {
            if (empresaId <= 0 || reservaId <= 0) return BadRequest(new { mensaje = "empresaId y reservaId requeridos" });
            return Ok(await _stay.GetPagosByReservaAsync(empresaId, reservaId));
        }

        [HttpPost("pagos")]
        public async Task<IActionResult> RegistrarPago([FromBody] StayPago body)
        {
            if (body == null || body.EmpresaId <= 0 || body.ReservaId <= 0)
                return BadRequest(new { mensaje = "EmpresaId y ReservaId requeridos" });
            try
            {
                var pago = await _stay.RegistrarPagoAsync(body);
                return Ok(pago);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // --- Bloqueos / calendario ---
        [HttpGet("bloqueos")]
        public async Task<IActionResult> GetBloqueos(
            [FromQuery] int empresaId,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? habitacionId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _stay.GetBloqueosAsync(empresaId, desde, hasta, habitacionId));
        }

        [HttpPost("bloqueos")]
        public async Task<IActionResult> CrearBloqueo([FromBody] StayBloqueo body)
        {
            if (body == null || body.EmpresaId <= 0 || body.HabitacionId <= 0)
                return BadRequest(new { mensaje = "EmpresaId y HabitacionId requeridos" });
            try
            {
                return Ok(await _stay.CrearBloqueoAsync(body));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("bloqueos/{id}")]
        public async Task<IActionResult> DesactivarBloqueo(int id, [FromQuery] int empresaId)
        {
            var ok = await _stay.DesactivarBloqueoAsync(id, empresaId);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        // --- Huéspedes ---
        [HttpGet("huespedes")]
        public async Task<IActionResult> GetHuespedes([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _stay.GetHuespedesAsync(empresaId));
        }

        [HttpPost("huespedes")]
        public async Task<IActionResult> UpsertHuesped([FromBody] StayHuesped body)
        {
            if (body == null || body.EmpresaId <= 0 || body.PersonaId <= 0)
                return BadRequest(new { mensaje = "EmpresaId y PersonaId requeridos" });
            try
            {
                return Ok(await _stay.UpsertHuespedAsync(body));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // --- Disponibilidad ---
        [HttpGet("disponibilidad")]
        public async Task<IActionResult> GetDisponibilidad(
            [FromQuery] int empresaId,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            try
            {
                return Ok(await _stay.GetDisponibilidadAsync(empresaId, desde, hasta));
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // --- Check-in / Check-out ---
        [HttpPost("reservas/{reservaId}/check-in")]
        public async Task<IActionResult> CheckIn(int reservaId, [FromQuery] int empresaId)
        {
            try
            {
                var r = await _stay.CheckInAsync(empresaId, reservaId);
                if (r == null) return NotFound(new { mensaje = "Reserva no encontrada" });
                return Ok(r);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("reservas/{reservaId}/check-out")]
        public async Task<IActionResult> CheckOut(int reservaId, [FromQuery] int empresaId)
        {
            try
            {
                var r = await _stay.CheckOutAsync(empresaId, reservaId);
                if (r == null) return NotFound(new { mensaje = "Reserva no encontrada" });
                return Ok(r);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>Health / mapa del producto para pruebas.</summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                producto = "VORDIA STAY",
                version = "1.0-base",
                areas = new[]
                {
                    "Habitaciones (reutiliza /api/Habitaciones)",
                    "Reservas (reutiliza /api/Reservas)",
                    "Huéspedes (/api/Stay/huespedes + Persona Core)",
                    "Disponibilidad (/api/Stay/disponibilidad)",
                    "Check-in/out (/api/Stay/reservas/{id}/check-in|check-out)",
                    "Tarifas (/api/Stay/tarifas)",
                    "Pagos (/api/Stay/pagos)"
                },
                compatibleCon = new[] { "Fontana (app/fontana)", "Core Empresa/Usuario/LicenciaModulo" },
                noModifica = new[] { "app/vordia (HQ)", "app/condominio (Habitat)" }
            });
        }
    }
}
