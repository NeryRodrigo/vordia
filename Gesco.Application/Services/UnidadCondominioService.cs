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
            var existing = await _context.UnidadesCondominio.FindAsync(unidad.UnidadId);
            if (existing == null)
                throw new KeyNotFoundException($"Unidad {unidad.UnidadId} no encontrada");

            // Solo escalares: evita pisar el grafo (Contratos/Gastos) y corrupción de NIS.
            if (unidad.EmpresaId > 0)
                existing.EmpresaId = unidad.EmpresaId;
            if (!string.IsNullOrWhiteSpace(unidad.CodigoUnidad))
                existing.CodigoUnidad = unidad.CodigoUnidad.Trim();
            if (!string.IsNullOrWhiteSpace(unidad.NombreCondominio))
                existing.NombreCondominio = unidad.NombreCondominio.Trim();
            if (!string.IsNullOrWhiteSpace(unidad.Estado))
                existing.Estado = unidad.Estado.Trim();

            existing.NisAnde = NormalizeNis(unidad.NisAnde);
            existing.MedidorAgua = string.IsNullOrWhiteSpace(unidad.MedidorAgua)
                ? null
                : unidad.MedidorAgua.Trim();
            if (unidad.Observaciones != null)
                existing.Observaciones = unidad.Observaciones;

            await _context.SaveChangesAsync();
        }

        private static string? NormalizeNis(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            // Quita comillas accidentales al guardar / reenviar JSON
            while ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                s = s.Substring(1, s.Length - 2).Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
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
