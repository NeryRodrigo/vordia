using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// VORDIA AGRO — prototipo confinamiento agropecuario (cliente demo Agrofield).
    /// No modifica Habitat / Stay / Fontana / HQ.
    /// </summary>
    [ApiController]
    [Route("api/Agro")]
    public class AgroController : ControllerBase
    {
        private readonly IAgroService _agro;

        public AgroController(IAgroService agro)
        {
            _agro = agro;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping() => Ok(await _agro.PingAsync());

        /// <summary>
        /// Empresa Agrofield en GESCO_DB. Permite a usuarios Vordia HQ abrir /agro
        /// sin loguearse con otro correo.
        /// </summary>
        [HttpGet("contexto-agrofield")]
        public async Task<IActionResult> ContextoAgrofield()
        {
            var ctx = await _agro.ResolverContextoAgrofieldAsync();
            if (ctx == null)
                return NotFound(new { mensaje = "No existe empresa Agrofield. Ejecutá Seed_Agrofield_Empresa.sql" });
            return Ok(ctx);
        }

        [HttpGet("establecimientos")]
        public async Task<IActionResult> Establecimientos([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarEstablecimientosAsync(empresaId));
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var dash = await _agro.DashboardAsync(empresaId, establecimientoId);
            if (dash == null) return NotFound(new { mensaje = "Sin establecimiento Agro. Ejecutá Create_Vordia_Agro.sql" });
            return Ok(dash);
        }

        [HttpGet("animales")]
        public async Task<IActionResult> Animales([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarAnimalesAsync(empresaId, establecimientoId, estado));
        }

        [HttpPost("animales")]
        public async Task<IActionResult> CrearAnimal([FromBody] AgroAnimalUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CodigoInterno))
                return BadRequest(new { mensaje = "CodigoInterno obligatorio" });
            try { return Ok(await _agro.UpsertAnimalAsync(request)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPut("animales/{animalId:int}")]
        public async Task<IActionResult> ActualizarAnimal(int animalId, [FromBody] AgroAnimalUpsertRequest request)
        {
            try { return Ok(await _agro.UpsertAnimalAsync(request, animalId)); }
            catch (InvalidOperationException ex) { return NotFound(new { mensaje = ex.Message }); }
        }

        [HttpGet("pesajes")]
        public async Task<IActionResult> Pesajes([FromQuery] int empresaId, [FromQuery] int animalId)
        {
            if (empresaId <= 0 || animalId <= 0) return BadRequest(new { mensaje = "empresaId y animalId requeridos" });
            return Ok(await _agro.ListarPesajesAsync(empresaId, animalId));
        }

        [HttpPost("pesajes")]
        public async Task<IActionResult> RegistrarPesaje([FromBody] AgroPesajeCreateRequest request)
        {
            try { return Ok(await _agro.RegistrarPesajeAsync(request)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("raciones")]
        public async Task<IActionResult> Raciones([FromQuery] int empresaId, [FromQuery] int? loteId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarRacionesAsync(empresaId, loteId));
        }

        [HttpPost("raciones")]
        public async Task<IActionResult> RegistrarRacion([FromBody] AgroRacionCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TipoAlimento) || request.CantidadKg <= 0)
                return BadRequest(new { mensaje = "TipoAlimento y CantidadKg requeridos" });
            try { return Ok(await _agro.RegistrarRacionAsync(request)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("lotes")]
        public async Task<IActionResult> Lotes([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarLotesAsync(empresaId, establecimientoId));
        }

        [HttpGet("dispositivos")]
        public async Task<IActionResult> Dispositivos([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarDispositivosAsync(empresaId, establecimientoId));
        }

        [HttpGet("alertas")]
        public async Task<IActionResult> Alertas([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null, [FromQuery] bool soloAbiertas = true)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarAlertasAsync(empresaId, establecimientoId, soloAbiertas));
        }

        [HttpPost("alertas/{alertaId:int}/resolver")]
        public async Task<IActionResult> ResolverAlerta(int alertaId, [FromQuery] int empresaId)
        {
            var ok = await _agro.ResolverAlertaAsync(alertaId, empresaId);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        [HttpGet("zonas")]
        public async Task<IActionResult> Zonas([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarZonasAsync(empresaId, establecimientoId));
        }

        [HttpPost("simular-ubicacion")]
        public async Task<IActionResult> SimularUbicacion([FromBody] AgroSimularUbicacionRequest request)
        {
            var updated = await _agro.SimularUbicacionAsync(request);
            if (updated == null) return NotFound(new { mensaje = "Animal no encontrado" });
            return Ok(updated);
        }
    }
}
