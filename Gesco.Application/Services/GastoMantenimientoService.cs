using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;

namespace Gesco.Application.Services
{
    public class GastoMantenimientoService : IGastoMantenimientoService
    {
        private static readonly HashSet<string> TiposServicio = new(StringComparer.OrdinalIgnoreCase)
        {
            "Luz", "Agua", "Basura"
        };

        private readonly IApplicationDbContext _context;

        public GastoMantenimientoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<GastoServicioMantenimientoDto>> GetServiciosByUnidadAsync(int unidadId, string? tipoServicio = null)
        {
            var q = _context.GastosMantenimiento.AsNoTracking().Where(g => g.UnidadId == unidadId && TiposServicio.Contains(g.Descripcion));
            if (!string.IsNullOrWhiteSpace(tipoServicio) && TiposServicio.Contains(tipoServicio))
            {
                var tipoNorm = NormalizeTipo(tipoServicio);
                q = q.Where(g => g.Descripcion == tipoNorm);
            }

            var list = await q.OrderByDescending(g => g.Fecha).ThenByDescending(g => g.GastoId).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<GastoServicioMantenimientoDto?> GetServicioByIdAsync(int gastoId)
        {
            var g = await _context.GastosMantenimiento.AsNoTracking().FirstOrDefaultAsync(x => x.GastoId == gastoId);
            if (g == null || !TiposServicio.Contains(g.Descripcion))
                return null;
            return ToDto(g);
        }

        public async Task<GastoServicioMantenimientoDto> CreateServicioAsync(GastoServicioMantenimientoRequest request)
        {
            if (!TiposServicio.Contains(request.TipoServicio))
                throw new ArgumentException("TipoServicio debe ser Luz, Agua o Basura.");

            var unidad = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.UnidadId == request.UnidadId);
            if (unidad == null)
                throw new InvalidOperationException("La unidad no existe.");

            if (request.TipoServicio.Equals("Luz", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.IdentificadorServicio))
            {
                unidad.NisAnde = request.IdentificadorServicio;
            }
            else if (request.TipoServicio.Equals("Agua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.IdentificadorServicio))
            {
                unidad.MedidorAgua = request.IdentificadorServicio;
            }

            var entity = new GastoMantenimiento
            {
                UnidadId = request.UnidadId,
                Descripcion = NormalizeTipo(request.TipoServicio),
                Monto = request.Monto,
                Fecha = request.Fecha,
                Proveedor = request.Proveedor,
                IdentificadorServicio = request.IdentificadorServicio,
                NroComprobante = request.NroComprobante,
                MesReferencia = request.MesReferencia,
                AnioReferencia = request.AnioReferencia,
                MetodoPago = request.MetodoPago ?? "Efectivo"
            };

            _context.GastosMantenimiento.Add(entity);
            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<GastoServicioMantenimientoDto?> UpdateServicioAsync(int gastoId, GastoServicioMantenimientoRequest request)
        {
            if (!TiposServicio.Contains(request.TipoServicio))
                throw new ArgumentException("TipoServicio debe ser Luz, Agua o Basura.");

            var entity = await _context.GastosMantenimiento.FirstOrDefaultAsync(g => g.GastoId == gastoId);
            if (entity == null || !TiposServicio.Contains(entity.Descripcion))
                return null;

            var unidad = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.UnidadId == request.UnidadId);
            if (unidad == null)
                throw new InvalidOperationException("La unidad no existe.");

            if (request.TipoServicio.Equals("Luz", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.IdentificadorServicio))
            {
                unidad.NisAnde = request.IdentificadorServicio;
            }
            else if (request.TipoServicio.Equals("Agua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(request.IdentificadorServicio))
            {
                unidad.MedidorAgua = request.IdentificadorServicio;
            }

            entity.UnidadId = request.UnidadId;
            entity.Descripcion = NormalizeTipo(request.TipoServicio);
            entity.Monto = request.Monto;
            entity.Fecha = request.Fecha;
            entity.Proveedor = request.Proveedor;
            entity.IdentificadorServicio = request.IdentificadorServicio;
            entity.NroComprobante = request.NroComprobante;
            entity.MesReferencia = request.MesReferencia;
            entity.AnioReferencia = request.AnioReferencia;
            entity.MetodoPago = request.MetodoPago ?? "Efectivo";

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<bool> DeleteServicioAsync(int gastoId)
        {
            var entity = await _context.GastosMantenimiento.FirstOrDefaultAsync(g => g.GastoId == gastoId);
            if (entity == null || !TiposServicio.Contains(entity.Descripcion))
                return false;

            _context.GastosMantenimiento.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static string NormalizeTipo(string tipo) =>
            TiposServicio.First(t => t.Equals(tipo, StringComparison.OrdinalIgnoreCase));

        private static GastoServicioMantenimientoDto ToDto(GastoMantenimiento g) => new()
        {
            GastoId = g.GastoId,
            UnidadId = g.UnidadId,
            TipoServicio = g.Descripcion,
            IdentificadorServicio = g.IdentificadorServicio,
            NroComprobante = g.NroComprobante,
            MesReferencia = g.MesReferencia,
            AnioReferencia = g.AnioReferencia,
            MetodoPago = g.MetodoPago,
            Monto = g.Monto,
            Fecha = g.Fecha,
            Proveedor = g.Proveedor
        };
    }
}
