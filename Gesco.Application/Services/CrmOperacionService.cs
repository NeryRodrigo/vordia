using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class CrmOperacionService : ICrmOperacionService
    {
        private static readonly string[] EstadosTarea = { "Pendiente", "En curso", "Hecha", "Cancelada" };
        private static readonly string[] Prioridades = { "Baja", "Media", "Alta" };

        private readonly IApplicationDbContext _context;
        private readonly ICrmFinanzasService _finanzas;

        public CrmOperacionService(IApplicationDbContext context, ICrmFinanzasService finanzas)
        {
            _context = context;
            _finanzas = finanzas;
        }

        public async Task<List<CrmTareaDto>> ListarTareasAsync(int empresaId, string? estado = null)
        {
            var query = _context.CrmTareas.AsNoTracking()
                .Include(t => t.Cliente)
                .Where(t => t.EmpresaId == empresaId && t.Activo);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(t => t.Estado == estado.Trim());

            var list = await query
                .OrderBy(t => t.Estado == "Hecha" || t.Estado == "Cancelada")
                .ThenBy(t => t.FechaVencimiento ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Prioridad == "Alta")
                .ToListAsync();

            return list.Select(MapTarea).ToList();
        }

        public async Task<CrmTareaDto> CrearTareaAsync(CrmTareaUpsertRequest request)
        {
            var entity = new CrmTarea
            {
                EmpresaId = request.EmpresaId,
                Titulo = request.Titulo.Trim(),
                Descripcion = request.Descripcion,
                FechaVencimiento = request.FechaVencimiento?.Date,
                Estado = Normalizar(request.Estado, EstadosTarea, "Pendiente"),
                Prioridad = Normalizar(request.Prioridad, Prioridades, "Media"),
                ClienteId = request.ClienteId,
                ContratoId = request.ContratoId,
                AsignadoUsuarioId = request.AsignadoUsuarioId,
                AsignadoNombre = request.AsignadoNombre?.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                FechaCompletada = string.Equals(request.Estado, "Hecha", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.UtcNow : null
            };

            _context.CrmTareas.Add(entity);
            await _context.SaveChangesAsync();

            if (entity.ClienteId.HasValue)
            {
                entity = await _context.CrmTareas.AsNoTracking()
                    .Include(t => t.Cliente)
                    .FirstAsync(t => t.TareaId == entity.TareaId);
            }

            return MapTarea(entity);
        }

        public async Task<CrmTareaDto?> ActualizarTareaAsync(int tareaId, CrmTareaUpsertRequest request)
        {
            var entity = await _context.CrmTareas
                .FirstOrDefaultAsync(t => t.TareaId == tareaId && t.EmpresaId == request.EmpresaId && t.Activo);
            if (entity == null) return null;

            var estadoNuevo = Normalizar(request.Estado, EstadosTarea, entity.Estado);
            entity.Titulo = request.Titulo.Trim();
            entity.Descripcion = request.Descripcion;
            entity.FechaVencimiento = request.FechaVencimiento?.Date;
            entity.Prioridad = Normalizar(request.Prioridad, Prioridades, entity.Prioridad);
            entity.ClienteId = request.ClienteId;
            entity.ContratoId = request.ContratoId;
            entity.AsignadoUsuarioId = request.AsignadoUsuarioId;
            entity.AsignadoNombre = request.AsignadoNombre?.Trim();

            if (estadoNuevo != entity.Estado)
            {
                entity.Estado = estadoNuevo;
                entity.FechaCompletada = estadoNuevo == "Hecha" ? DateTime.UtcNow : null;
            }

            await _context.SaveChangesAsync();

            var loaded = await _context.CrmTareas.AsNoTracking()
                .Include(t => t.Cliente)
                .FirstAsync(t => t.TareaId == tareaId);
            return MapTarea(loaded);
        }

        public async Task<CrmTareaDto?> CambiarEstadoTareaAsync(int tareaId, int empresaId, string estado)
        {
            var entity = await _context.CrmTareas
                .FirstOrDefaultAsync(t => t.TareaId == tareaId && t.EmpresaId == empresaId && t.Activo);
            if (entity == null) return null;

            var estadoNuevo = Normalizar(estado, EstadosTarea, entity.Estado);
            entity.Estado = estadoNuevo;
            entity.FechaCompletada = estadoNuevo == "Hecha" ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            var loaded = await _context.CrmTareas.AsNoTracking()
                .Include(t => t.Cliente)
                .FirstAsync(t => t.TareaId == tareaId);
            return MapTarea(loaded);
        }

        public async Task<bool> EliminarTareaAsync(int tareaId, int empresaId)
        {
            var entity = await _context.CrmTareas
                .FirstOrDefaultAsync(t => t.TareaId == tareaId && t.EmpresaId == empresaId && t.Activo);
            if (entity == null) return false;
            entity.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmSocioDto>> ListarSociosAsync(int empresaId)
        {
            return await _context.CrmSocios.AsNoTracking()
                .Where(s => s.EmpresaId == empresaId && s.Activo)
                .OrderByDescending(s => s.Porcentaje)
                .ThenBy(s => s.Nombre)
                .Select(s => new CrmSocioDto
                {
                    SocioId = s.SocioId,
                    EmpresaId = s.EmpresaId,
                    Nombre = s.Nombre,
                    Email = s.Email,
                    Porcentaje = s.Porcentaje,
                    UsuarioId = s.UsuarioId,
                    Activo = s.Activo
                })
                .ToListAsync();
        }

        public async Task<CrmSocioDto> UpsertSocioAsync(CrmSocioUpsertRequest request, int? socioId = null)
        {
            CrmSocio entity;
            if (socioId.HasValue)
            {
                entity = await _context.CrmSocios
                    .FirstOrDefaultAsync(s => s.SocioId == socioId && s.EmpresaId == request.EmpresaId && s.Activo)
                    ?? throw new InvalidOperationException("Socio no encontrado.");
                entity.Nombre = request.Nombre.Trim();
                entity.Email = request.Email?.Trim();
                entity.Porcentaje = request.Porcentaje;
                entity.UsuarioId = request.UsuarioId;
            }
            else
            {
                entity = new CrmSocio
                {
                    EmpresaId = request.EmpresaId,
                    Nombre = request.Nombre.Trim(),
                    Email = request.Email?.Trim(),
                    Porcentaje = request.Porcentaje,
                    UsuarioId = request.UsuarioId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CrmSocios.Add(entity);
            }

            await _context.SaveChangesAsync();
            return new CrmSocioDto
            {
                SocioId = entity.SocioId,
                EmpresaId = entity.EmpresaId,
                Nombre = entity.Nombre,
                Email = entity.Email,
                Porcentaje = entity.Porcentaje,
                UsuarioId = entity.UsuarioId,
                Activo = entity.Activo
            };
        }

        public async Task<bool> EliminarSocioAsync(int socioId, int empresaId)
        {
            var entity = await _context.CrmSocios
                .FirstOrDefaultAsync(s => s.SocioId == socioId && s.EmpresaId == empresaId && s.Activo);
            if (entity == null) return false;
            entity.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CrmFlujoCajaDto> ObtenerFlujoCajaAsync(int empresaId)
        {
            var ingresos = await _finanzas.ObtenerIngresosAsync(empresaId);
            var socios = await ListarSociosAsync(empresaId);
            var pagos = await _finanzas.ListarPagosAsync(empresaId);

            var meses = pagos
                .GroupBy(p => p.FechaPago.ToString("yyyy-MM"))
                .OrderByDescending(g => g.Key)
                .Take(6)
                .Select(g => new CrmFlujoMesDto
                {
                    Periodo = g.Key,
                    Ingresos = g.Sum(x => x.Monto),
                    CantidadPagos = g.Count()
                })
                .OrderBy(m => m.Periodo)
                .ToList();

            var pendiente = ingresos.Recurrentes
                .Where(c => c.EstadoPago == "Pendiente" || c.EstadoPago == "Vencido")
                .Sum(c => c.FeeMensual);

            var reparto = socios.Select(s => new CrmRepartoSocioDto
            {
                SocioId = s.SocioId,
                Nombre = s.Nombre,
                Porcentaje = s.Porcentaje,
                MontoMes = Math.Round(ingresos.CobradoMesActual * (s.Porcentaje / 100m), 0),
                MontoTotal = Math.Round(ingresos.CobradoTotal * (s.Porcentaje / 100m), 0),
                MontoMrrProyectado = Math.Round(ingresos.MrrActivo * (s.Porcentaje / 100m), 0)
            }).ToList();

            return new CrmFlujoCajaDto
            {
                CobradoMesActual = ingresos.CobradoMesActual,
                CobradoTotal = ingresos.CobradoTotal,
                MrrActivo = ingresos.MrrActivo,
                ArrProyectado = ingresos.ArrProyectado,
                PendienteCobro = pendiente,
                SumaPorcentajesSocios = socios.Sum(s => s.Porcentaje),
                UltimosMeses = meses,
                Reparto = reparto,
                Socios = socios
            };
        }

        private static string Normalizar(string? value, string[] allowed, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase)) ?? fallback;
        }

        private static CrmTareaDto MapTarea(CrmTarea t)
        {
            var vencida = t.FechaVencimiento.HasValue
                && t.FechaVencimiento.Value.Date < DateTime.Today
                && t.Estado != "Hecha"
                && t.Estado != "Cancelada";

            return new CrmTareaDto
            {
                TareaId = t.TareaId,
                EmpresaId = t.EmpresaId,
                Titulo = t.Titulo,
                Descripcion = t.Descripcion,
                FechaVencimiento = t.FechaVencimiento,
                Estado = t.Estado,
                Prioridad = t.Prioridad,
                ClienteId = t.ClienteId,
                ClienteNombre = t.Cliente?.RazonSocial,
                ContratoId = t.ContratoId,
                AsignadoUsuarioId = t.AsignadoUsuarioId,
                AsignadoNombre = t.AsignadoNombre,
                FechaCreacion = t.FechaCreacion,
                FechaCompletada = t.FechaCompletada,
                Vencida = vencida
            };
        }
    }
}
