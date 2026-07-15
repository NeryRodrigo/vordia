using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfiguracionesController : ControllerBase
    {
        private readonly IConfiguracionTimbradoService _configuracionService;

        public ConfiguracionesController(IConfiguracionTimbradoService configuracionService)
        {
            _configuracionService = configuracionService;
        }

        // GET: api/Configuraciones/empresa/5
        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<ConfiguracionTimbrado>>> GetConfiguracionesPorEmpresa(int empresaId)
        {
            var configuraciones = await _configuracionService.GetAllByEmpresaAsync(empresaId);
            return Ok(configuraciones);
        }

        // GET: api/Configuraciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ConfiguracionTimbrado>> GetConfiguracion(int id)
        {
            var configuracion = await _configuracionService.GetByIdAsync(id);

            if (configuracion == null)
            {
                return NotFound();
            }

            return Ok(configuracion);
        }

        // POST: api/Configuraciones
        [HttpPost]
        public async Task<ActionResult<ConfiguracionTimbrado>> PostConfiguracion(ConfiguracionTimbrado configuracion)
        {
            var nuevaConfiguracion = await _configuracionService.CreateAsync(configuracion);
            return CreatedAtAction(nameof(GetConfiguracion), new { id = nuevaConfiguracion.TimbradoId }, nuevaConfiguracion);
        }

        // PUT: api/Configuraciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConfiguracion(int id, ConfiguracionTimbrado configuracion)
        {
            if (id != configuracion.TimbradoId)
            {
                return BadRequest();
            }

            await _configuracionService.UpdateAsync(configuracion);
            return NoContent();
        }

        // DELETE: api/Configuraciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConfiguracion(int id)
        {
            await _configuracionService.DeleteAsync(id);
            return NoContent();
        }
    }
}
