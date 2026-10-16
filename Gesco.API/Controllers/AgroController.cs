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
        public async Task<IActionResult> Animales([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null, [FromQuery] string? estado = null, [FromQuery] string? etapa = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarAnimalesAsync(empresaId, establecimientoId, estado, etapa));
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
    
        /* ───────────── Rediseño por módulos ───────────── */

        [HttpGet("configuracion")]
        public async Task<IActionResult> Configuracion([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ObtenerConfiguracionAsync(empresaId));
        }

        [HttpPut("configuracion")]
        public async Task<IActionResult> GuardarConfiguracion([FromBody] AgroConfiguracionRequest request)
        {
            if (request.EmpresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.GuardarConfiguracionAsync(request));
        }

        /// <summary>Dashboard ejecutivo: panorama de toda la empresa ganadera.</summary>
        [HttpGet("ejecutivo")]
        public async Task<IActionResult> Ejecutivo([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var dto = await _agro.EjecutivoAsync(empresaId, establecimientoId);
            return dto == null ? NotFound(new { mensaje = "Sin establecimiento Agro" }) : Ok(dto);
        }

        /// <summary>Dashboard VORDIA Feed — Engorde / Confinamiento.</summary>
        [HttpGet("feed")]
        public async Task<IActionResult> Feed([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null, [FromQuery] int dias = 70)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var dto = await _agro.FeedAsync(empresaId, establecimientoId, dias);
            return dto == null ? NotFound(new { mensaje = "Sin establecimiento Agro" }) : Ok(dto);
        }

        /// <summary>Dashboard principal de VORDIA Agro (todo en una llamada).</summary>
        [HttpGet("principal")]
        public async Task<IActionResult> Principal([FromQuery] int empresaId, [FromQuery] int dias = 30)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var dto = await _agro.PrincipalAsync(empresaId, dias);
            return dto == null ? NotFound(new { mensaje = "Sin establecimiento Agro" }) : Ok(dto);
        }

        [HttpGet("inventario")]
        public async Task<IActionResult> Inventario([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarInventarioAsync(empresaId));
        }

        [HttpPost("inventario")]
        public async Task<IActionResult> CrearInsumo([FromBody] AgroInsumoRequest request)
        {
            try { return Ok(await _agro.CrearInsumoAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("inventario/{insumoId:int}/movimientos")]
        public async Task<IActionResult> Movimientos(int insumoId, [FromQuery] int empresaId) =>
            Ok(await _agro.ListarMovimientosAsync(empresaId, insumoId));

        [HttpPost("inventario/{insumoId:int}/movimientos")]
        public async Task<IActionResult> RegistrarMovimiento(int insumoId, [FromBody] AgroMovimientoRequest request)
        {
            try { return Ok(await _agro.RegistrarMovimientoAsync(insumoId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("lotes")]
        public async Task<IActionResult> CrearLote([FromBody] AgroLoteRequest request)
        {
            try { return Ok(await _agro.GuardarLoteAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPut("lotes/{loteId:int}")]
        public async Task<IActionResult> ActualizarLote(int loteId, [FromBody] AgroLoteRequest request)
        {
            try { return Ok(await _agro.GuardarLoteAsync(request, loteId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("sanidad")]
        public async Task<IActionResult> Sanidad([FromQuery] int empresaId, [FromQuery] int? establecimientoId = null, [FromQuery] string? estado = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _agro.ListarSanidadAsync(empresaId, establecimientoId, estado));
        }

        [HttpPost("sanidad")]
        public async Task<IActionResult> RegistrarSanidad([FromBody] AgroSanidadCreateRequest request)
        {
            try { return Ok(await _agro.RegistrarSanidadAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("sanidad/{sanidadId:int}/aplicar")]
        public async Task<IActionResult> AplicarSanidad(int sanidadId, [FromQuery] int empresaId)
        {
            var ok = await _agro.AplicarSanidadAsync(sanidadId, empresaId);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        [HttpPost("animales/{animalId:int}/etapa")]
        public async Task<IActionResult> CambiarEtapa(int animalId, [FromBody] AgroCambioEtapaRequest request)
        {
            try { return Ok(await _agro.CambiarEtapaAsync(animalId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("animales/{animalId:int}/ficha")]
        public async Task<IActionResult> Ficha(int animalId, [FromQuery] int empresaId)
        {
            var ficha = await _agro.FichaAnimalAsync(empresaId, animalId);
            return ficha == null ? NotFound(new { mensaje = "Animal no encontrado" }) : Ok(ficha);
        }
    }
}
