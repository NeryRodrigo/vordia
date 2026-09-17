using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// Reglas del motor de automatizaciones de Vordia HQ. El mismo motor que corre en
    /// segundo plano se puede disparar desde acá para probar una regla al instante.
    /// </summary>
    [Route("api/Crm")]
    [ApiController]
    public class CrmAutomatizacionesController : ControllerBase
    {
        private readonly ICrmAutomatizacionesService _automatizaciones;
        private readonly ILogger<CrmAutomatizacionesController> _logger;

        public CrmAutomatizacionesController(
            ICrmAutomatizacionesService automatizaciones,
            ILogger<CrmAutomatizacionesController> logger)
        {
            _automatizaciones = automatizaciones;
            _logger = logger;
        }

        [HttpGet("automatizaciones")]
        public async Task<IActionResult> Listar([FromQuery] int empresaId, [FromQuery] bool? soloActivas = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _automatizaciones.ListarAsync(empresaId, soloActivas));
        }

        /// <summary>Disparadores y acciones disponibles, para armar los combos del formulario.</summary>
        [HttpGet("automatizaciones/catalogo")]
        public IActionResult Catalogo() => Ok(_automatizaciones.Catalogo());

        [HttpGet("automatizaciones/ejecuciones")]
        public async Task<IActionResult> Ejecuciones(
            [FromQuery] int empresaId,
            [FromQuery] int? automatizacionId = null,
            [FromQuery] int top = 50)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _automatizaciones.ListarEjecucionesAsync(empresaId, automatizacionId, top));
        }

        [HttpGet("automatizaciones/{automatizacionId:int}")]
        public async Task<IActionResult> Obtener(int automatizacionId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var dto = await _automatizaciones.ObtenerAsync(automatizacionId, empresaId);
            if (dto == null) return NotFound(new { mensaje = "La automatización no existe." });
            return Ok(dto);
        }

        [HttpPost("automatizaciones")]
        public async Task<IActionResult> Crear([FromBody] CrmAutomatizacionUpsertRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { mensaje = "Revisá los datos del formulario." });

            try
            {
                return Ok(await _automatizaciones.CrearAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("automatizaciones/{automatizacionId:int}")]
        public async Task<IActionResult> Actualizar(int automatizacionId, [FromBody] CrmAutomatizacionUpsertRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { mensaje = "Revisá los datos del formulario." });

            try
            {
                var dto = await _automatizaciones.ActualizarAsync(automatizacionId, request);
                if (dto == null) return NotFound(new { mensaje = "La automatización no existe." });
                return Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>Enciende o pausa la regla.</summary>
        [HttpPatch("automatizaciones/{automatizacionId:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int automatizacionId, [FromBody] CrmAutomatizacionEstadoRequest request)
        {
            if (request.EmpresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var dto = await _automatizaciones.CambiarEstadoAsync(automatizacionId, request);
            if (dto == null) return NotFound(new { mensaje = "La automatización no existe." });
            return Ok(dto);
        }

        [HttpDelete("automatizaciones/{automatizacionId:int}")]
        public async Task<IActionResult> Eliminar(int automatizacionId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var ok = await _automatizaciones.EliminarAsync(automatizacionId, empresaId);
            if (!ok) return NotFound(new { mensaje = "La automatización no existe." });
            return NoContent();
        }

        /// <summary>
        /// Corre el motor ahora. Sin automatizacionId toma todas las reglas encendidas;
        /// con simular=true informa qué haría sin enviar nada.
        /// </summary>
        [HttpPost("automatizaciones/ejecutar")]
        public async Task<IActionResult> Ejecutar(
            [FromQuery] int empresaId,
            [FromQuery] int? automatizacionId = null,
            [FromQuery] bool simular = false)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            try
            {
                var corrida = await _automatizaciones.EjecutarAsync(empresaId, automatizacionId, simular);
                _logger.LogInformation("Automatizaciones ejecutadas a pedido: {Resumen}", corrida.Resumen);
                return Ok(corrida);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error corriendo las automatizaciones de la empresa {EmpresaId}", empresaId);
                return StatusCode(500, new { mensaje = $"No se pudo correr el motor. {ex.Message}" });
            }
        }
    }
}
