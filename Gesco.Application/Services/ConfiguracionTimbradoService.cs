using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class ConfiguracionTimbradoService : IConfiguracionTimbradoService
    {
        private readonly IApplicationDbContext _context;

        public ConfiguracionTimbradoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConfiguracionTimbrado>> GetAllByEmpresaAsync(int empresaId)
        {
            return await _context.ConfiguracionesTimbrado
                .Include(c => c.Empresa)
                .Where(c => c.EmpresaId == empresaId)
                .OrderByDescending(c => c.EstaActivo)
                .ThenByDescending(c => c.ValidezHasta)
                .ToListAsync();
        }

        public async Task<ConfiguracionTimbrado?> GetByIdAsync(int id)
        {
            return await _context.ConfiguracionesTimbrado.FindAsync(id);
        }

        public async Task<ConfiguracionTimbrado> CreateAsync(ConfiguracionTimbrado configuracion)
        {
            _context.ConfiguracionesTimbrado.Add(configuracion);
            await _context.SaveChangesAsync();
            return configuracion;
        }

        public async Task UpdateAsync(ConfiguracionTimbrado configuracion)
        {
            _context.ConfiguracionesTimbrado.Update(configuracion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var configuracion = await _context.ConfiguracionesTimbrado.FindAsync(id);
            if (configuracion != null)
            {
                _context.ConfiguracionesTimbrado.Remove(configuracion);
                await _context.SaveChangesAsync();
            }
        }
    }
}
