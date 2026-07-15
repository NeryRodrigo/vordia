using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfiguracionMultasController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public ConfiguracionMultasController(IApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ConfiguracionMultas/empresa/7
        [HttpGet("empresa/{empresaId:int}")]
        public async Task<ActionResult<ConfiguracionMulta>> GetConfiguracionMulta(int empresaId)
        {
            var config = await _context.ConfiguracionesMultas
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

            if (config == null)
            {
                // Devolvemos un objeto por defecto pero no lo guardamos aún en base de datos.
                config = new ConfiguracionMulta
                {
                    EmpresaId = empresaId,
                    MontoMulta = 0,
                    TipoAplicacion = "FinMesAtrasado",
                    DiasGracia = 5
                };
            }

            return Ok(config);
        }

        // POST/PUT: api/ConfiguracionMultas
        [HttpPost]
        public async Task<ActionResult<ConfiguracionMulta>> SaveConfiguracionMulta([FromBody] ConfiguracionMulta config)
        {
            var dbConfig = await _context.ConfiguracionesMultas
                .FirstOrDefaultAsync(c => c.EmpresaId == config.EmpresaId);

            if (dbConfig == null)
            {
                _context.ConfiguracionesMultas.Add(config);
            }
            else
            {
                dbConfig.MontoMulta = config.MontoMulta;
                dbConfig.TipoAplicacion = config.TipoAplicacion;
                dbConfig.DiasGracia = config.DiasGracia;
            }

            await _context.SaveChangesAsync();
            return Ok(dbConfig ?? config);
        }
    }
}
