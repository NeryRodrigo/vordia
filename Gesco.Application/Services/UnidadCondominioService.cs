using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class UnidadCondominioService : IUnidadCondominioService
    {
        private readonly IApplicationDbContext _context;

        public UnidadCondominioService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UnidadCondominio>> GetAllByEmpresaAsync(int empresaId)
        {
            return await _context.UnidadesCondominio
                .Where(u => u.EmpresaId == empresaId)
                .Include(u => u.Contratos)
                    .ThenInclude(c => c.Inquilino)
                .Include(u => u.Contratos)
                    .ThenInclude(c => c.Pagos)
                .OrderBy(u => u.NombreCondominio)
                .ThenBy(u => u.CodigoUnidad)
                .ToListAsync();
        }

        public async Task<UnidadCondominio?> GetByIdAsync(int unidadId)
        {
            return await _context.UnidadesCondominio
                .Include(u => u.Contratos)
                .Include(u => u.Gastos)
                .FirstOrDefaultAsync(u => u.UnidadId == unidadId);
        }

        public async Task<UnidadCondominio> CreateAsync(UnidadCondominio unidad)
        {
            _context.UnidadesCondominio.Add(unidad);
            await _context.SaveChangesAsync();
            return unidad;
        }

        public async Task UpdateAsync(UnidadCondominio unidad)
        {
            _context.UnidadesCondominio.Update(unidad);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int unidadId)
        {
            var unidad = await _context.UnidadesCondominio.FindAsync(unidadId);
            if (unidad != null)
            {
                _context.UnidadesCondominio.Remove(unidad);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SeedAguzDuplexesAsync(int empresaId)
        {
            // Aguz 1 (5 duplex)
            for (int i = 1; i <= 5; i++)
            {
                var code = $"D-{i:D2}";
                if (!await _context.UnidadesCondominio.AnyAsync(u => u.EmpresaId == empresaId && u.NombreCondominio == "Aguz 1" && u.CodigoUnidad == code))
                {
                    _context.UnidadesCondominio.Add(new UnidadCondominio
                    {
                        EmpresaId = empresaId,
                        NombreCondominio = "Aguz 1",
                        CodigoUnidad = code,
                        Estado = "Disponible",
                        Observaciones = "Carga inicial automática"
                    });
                }
            }

            // Aguz 2 (6 duplex)
            for (int i = 1; i <= 6; i++)
            {
                var code = $"D-{i:D2}";
                if (!await _context.UnidadesCondominio.AnyAsync(u => u.EmpresaId == empresaId && u.NombreCondominio == "Aguz 2" && u.CodigoUnidad == code))
                {
                    _context.UnidadesCondominio.Add(new UnidadCondominio
                    {
                        EmpresaId = empresaId,
                        NombreCondominio = "Aguz 2",
                        CodigoUnidad = code,
                        Estado = "Disponible",
                        Observaciones = "Carga inicial automática"
                    });
                }
            }

            // Aguz 3 (4 duplex)
            for (int i = 1; i <= 4; i++)
            {
                var code = $"D-{i:D2}";
                if (!await _context.UnidadesCondominio.AnyAsync(u => u.EmpresaId == empresaId && u.NombreCondominio == "Aguz 3" && u.CodigoUnidad == code))
                {
                    _context.UnidadesCondominio.Add(new UnidadCondominio
                    {
                        EmpresaId = empresaId,
                        NombreCondominio = "Aguz 3",
                        CodigoUnidad = code,
                        Estado = "Disponible",
                        Observaciones = "Carga inicial automática"
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
