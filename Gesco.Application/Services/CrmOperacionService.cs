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

        // --- Roadmap: Ideas & Alianzas ---
        private static readonly string[] TiposRoadmap = { "Idea", "Alianza" };
        private static readonly string[] EstadosRoadmap = { "Borrador", "En evaluacion", "Priorizado", "En curso", "Convertido", "Descartado" };
        private static readonly string[] PrioridadesRoadmap = { "P0", "P1", "P2" };
        private static readonly string[] ConvertidosA = { "Producto", "CRM", "Proyecto" };

        public async Task<List<CrmRoadmapDto>> ListarRoadmapAsync(int empresaId, string? tipo = null, string? estado = null)
        {
            var query = _context.CrmRoadmap.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo);

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(r => r.Tipo == tipo.Trim());
            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(r => r.Estado == estado.Trim());

            var list = await query
                .OrderBy(r => r.Estado == "Convertido" || r.Estado == "Descartado")
                .ThenBy(r => r.Prioridad == "P0" ? 0 : r.Prioridad == "P1" ? 1 : 2)
                .ThenBy(r => r.FechaProximoPaso ?? DateTime.MaxValue)
                .ThenByDescending(r => r.FechaRegistro)
                .ToListAsync();

            return list.Select(r => MapRoadmap(r, null)).ToList();
        }

        public async Task<CrmRoadmapDto?> ObtenerRoadmapAsync(int roadmapId, int empresaId)
        {
            var entity = await _context.CrmRoadmap.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoadmapId == roadmapId && r.EmpresaId == empresaId && r.Activo);
            if (entity == null) return null;

            var hist = await _context.CrmRoadmapHistorial.AsNoTracking()
                .Where(h => h.RoadmapId == roadmapId)
                .OrderByDescending(h => h.Fecha)
                .Take(50)
                .ToListAsync();

            return MapRoadmap(entity, hist);
        }

        public async Task<CrmRoadmapDto> CrearRoadmapAsync(CrmRoadmapUpsertRequest request)
        {
            var entity = new CrmRoadmap
            {
                EmpresaId = request.EmpresaId,
                Tipo = Normalizar(request.Tipo, TiposRoadmap, "Idea"),
                Titulo = request.Titulo.Trim(),
                Descripcion = request.Descripcion,
                Estado = Normalizar(request.Estado, EstadosRoadmap, "Borrador"),
                Prioridad = Normalizar(request.Prioridad, PrioridadesRoadmap, "P2"),
                ResponsableUsuarioId = request.ResponsableUsuarioId,
                ResponsableNombre = request.ResponsableNombre?.Trim(),
                FechaRegistro = (request.FechaRegistro ?? DateTime.UtcNow).Date,
                FechaMeta = request.FechaMeta?.Date,
                ProximoPaso = request.ProximoPaso?.Trim(),
                FechaProximoPaso = request.FechaProximoPaso?.Date,
                ConvertidoA = string.IsNullOrWhiteSpace(request.ConvertidoA)
                    ? null
                    : Normalizar(request.ConvertidoA, ConvertidosA, ConvertidosA[0]),
                Notas = request.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmRoadmap.Add(entity);
            await _context.SaveChangesAsync();

            _context.CrmRoadmapHistorial.Add(new CrmRoadmapHistorial
            {
                RoadmapId = entity.RoadmapId,
                EmpresaId = entity.EmpresaId,
                Tipo = "creacion",
                Titulo = "Registro creado",
                Detalle = $"{entity.Tipo}: {entity.Titulo}",
                EstadoNuevo = entity.Estado,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre,
                Fecha = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return (await ObtenerRoadmapAsync(entity.RoadmapId, entity.EmpresaId))!;
        }

        public async Task<CrmRoadmapDto?> ActualizarRoadmapAsync(int roadmapId, CrmRoadmapUpsertRequest request)
        {
            var entity = await _context.CrmRoadmap
                .FirstOrDefaultAsync(r => r.RoadmapId == roadmapId && r.EmpresaId == request.EmpresaId && r.Activo);
            if (entity == null) return null;

            var estadoAnterior = entity.Estado;
            var estadoNuevo = Normalizar(request.Estado, EstadosRoadmap, entity.Estado);

            entity.Tipo = Normalizar(request.Tipo, TiposRoadmap, entity.Tipo);
            entity.Titulo = request.Titulo.Trim();
            entity.Descripcion = request.Descripcion;
            entity.Prioridad = Normalizar(request.Prioridad, PrioridadesRoadmap, entity.Prioridad);
            entity.ResponsableUsuarioId = request.ResponsableUsuarioId;
            entity.ResponsableNombre = request.ResponsableNombre?.Trim();
            entity.FechaRegistro = (request.FechaRegistro ?? entity.FechaRegistro).Date;
            entity.FechaMeta = request.FechaMeta?.Date;
            entity.ProximoPaso = request.ProximoPaso?.Trim();
            entity.FechaProximoPaso = request.FechaProximoPaso?.Date;
            entity.ConvertidoA = string.IsNullOrWhiteSpace(request.ConvertidoA)
                ? null
                : Normalizar(request.ConvertidoA, ConvertidosA, ConvertidosA[0]);
            entity.Notas = request.Notas;
            entity.FechaActualizacion = DateTime.UtcNow;

            if (estadoNuevo != estadoAnterior)
            {
                entity.Estado = estadoNuevo;
                _context.CrmRoadmapHistorial.Add(new CrmRoadmapHistorial
                {
                    RoadmapId = entity.RoadmapId,
                    EmpresaId = entity.EmpresaId,
                    Tipo = "cambio_estado",
                    Titulo = $"Estado → {estadoNuevo}",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = estadoNuevo,
                    UsuarioId = request.UsuarioId,
                    UsuarioNombre = request.UsuarioNombre,
                    Fecha = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return await ObtenerRoadmapAsync(roadmapId, request.EmpresaId);
        }

        public async Task<CrmRoadmapDto?> CambiarEstadoRoadmapAsync(int roadmapId, int empresaId, string estado, int? usuarioId = null, string? usuarioNombre = null)
        {
            var entity = await _context.CrmRoadmap
                .FirstOrDefaultAsync(r => r.RoadmapId == roadmapId && r.EmpresaId == empresaId && r.Activo);
            if (entity == null) return null;

            var estadoAnterior = entity.Estado;
            var estadoNuevo = Normalizar(estado, EstadosRoadmap, entity.Estado);
            if (estadoNuevo == estadoAnterior) return await ObtenerRoadmapAsync(roadmapId, empresaId);

            entity.Estado = estadoNuevo;
            entity.FechaActualizacion = DateTime.UtcNow;
            _context.CrmRoadmapHistorial.Add(new CrmRoadmapHistorial
            {
                RoadmapId = entity.RoadmapId,
                EmpresaId = entity.EmpresaId,
                Tipo = "cambio_estado",
                Titulo = $"Estado → {estadoNuevo}",
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre,
                Fecha = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return await ObtenerRoadmapAsync(roadmapId, empresaId);
        }

        public async Task<CrmRoadmapDto?> AgregarNotaRoadmapAsync(int roadmapId, CrmRoadmapNotaRequest request)
        {
            var entity = await _context.CrmRoadmap
                .FirstOrDefaultAsync(r => r.RoadmapId == roadmapId && r.EmpresaId == request.EmpresaId && r.Activo);
            if (entity == null) return null;

            _context.CrmRoadmapHistorial.Add(new CrmRoadmapHistorial
            {
                RoadmapId = roadmapId,
                EmpresaId = request.EmpresaId,
                Tipo = "nota",
                Titulo = request.Titulo.Trim(),
                Detalle = request.Detalle,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre,
                Fecha = DateTime.UtcNow
            });
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await ObtenerRoadmapAsync(roadmapId, request.EmpresaId);
        }

        public async Task<bool> EliminarRoadmapAsync(int roadmapId, int empresaId)
        {
            var entity = await _context.CrmRoadmap
                .FirstOrDefaultAsync(r => r.RoadmapId == roadmapId && r.EmpresaId == empresaId && r.Activo);
            if (entity == null) return false;
            entity.Activo = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static CrmRoadmapDto MapRoadmap(CrmRoadmap r, List<CrmRoadmapHistorial>? hist)
        {
            var cerrados = r.Estado == "Convertido" || r.Estado == "Descartado";
            return new CrmRoadmapDto
            {
                RoadmapId = r.RoadmapId,
                EmpresaId = r.EmpresaId,
                Tipo = r.Tipo,
                Titulo = r.Titulo,
                Descripcion = r.Descripcion,
                Estado = r.Estado,
                Prioridad = r.Prioridad,
                ResponsableUsuarioId = r.ResponsableUsuarioId,
                ResponsableNombre = r.ResponsableNombre,
                FechaRegistro = r.FechaRegistro,
                FechaMeta = r.FechaMeta,
                ProximoPaso = r.ProximoPaso,
                FechaProximoPaso = r.FechaProximoPaso,
                ConvertidoA = r.ConvertidoA,
                Notas = r.Notas,
                FechaCreacion = r.FechaCreacion,
                FechaActualizacion = r.FechaActualizacion,
                VencidoProximoPaso = !cerrados && r.FechaProximoPaso.HasValue && r.FechaProximoPaso.Value.Date < DateTime.Today,
                VencidaMeta = !cerrados && r.FechaMeta.HasValue && r.FechaMeta.Value.Date < DateTime.Today,
                Historial = (hist ?? new List<CrmRoadmapHistorial>()).Select(h => new CrmRoadmapHistorialDto
                {
                    HistorialId = h.HistorialId,
                    Tipo = h.Tipo,
                    Titulo = h.Titulo,
                    Detalle = h.Detalle,
                    EstadoAnterior = h.EstadoAnterior,
                    EstadoNuevo = h.EstadoNuevo,
                    UsuarioNombre = h.UsuarioNombre,
                    Fecha = h.Fecha
                }).ToList()
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
