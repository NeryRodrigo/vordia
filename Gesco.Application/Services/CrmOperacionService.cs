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

        // ---------- Capital / Empleados / Objetivos ----------

        public async Task<CrmCapitalResumenDto> ObtenerCapitalAsync(int empresaId)
        {
            var movs = await _context.CrmCapitalMovimientos.AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && c.Activo)
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.CapitalId)
                .ToListAsync();

            decimal aportes = 0, retiros = 0, saldo = 0;
            foreach (var m in movs)
            {
                if (m.Tipo == "Retiro") { retiros += m.MontoGs; saldo -= m.MontoGs; }
                else { aportes += m.MontoGs; saldo += m.MontoGs; } // Aporte, Ajuste, Valuacion suman
            }

            return new CrmCapitalResumenDto
            {
                SaldoCapitalGs = saldo,
                TotalAportes = aportes,
                TotalRetiros = retiros,
                Movimientos = movs.Select(m => new CrmCapitalDto
                {
                    CapitalId = m.CapitalId,
                    EmpresaId = m.EmpresaId,
                    Tipo = m.Tipo,
                    MontoGs = m.MontoGs,
                    Fecha = m.Fecha,
                    Concepto = m.Concepto,
                    SocioId = m.SocioId,
                    UsuarioNombre = m.UsuarioNombre,
                    Notas = m.Notas
                }).ToList()
            };
        }

        public async Task<CrmCapitalDto> RegistrarCapitalAsync(CrmCapitalUpsertRequest request)
        {
            var tipo = Normalizar(request.Tipo, new[] { "Aporte", "Retiro", "Ajuste", "Valuacion" }, "Aporte");
            var entity = new CrmCapitalMovimiento
            {
                EmpresaId = request.EmpresaId,
                Tipo = tipo,
                MontoGs = Math.Abs(request.MontoGs),
                Fecha = (request.Fecha ?? DateTime.Today).Date,
                Concepto = string.IsNullOrWhiteSpace(request.Concepto) ? tipo : request.Concepto.Trim(),
                SocioId = request.SocioId,
                UsuarioNombre = request.UsuarioNombre,
                Notas = request.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _context.CrmCapitalMovimientos.Add(entity);
            await _context.SaveChangesAsync();
            return new CrmCapitalDto
            {
                CapitalId = entity.CapitalId,
                EmpresaId = entity.EmpresaId,
                Tipo = entity.Tipo,
                MontoGs = entity.MontoGs,
                Fecha = entity.Fecha,
                Concepto = entity.Concepto,
                SocioId = entity.SocioId,
                UsuarioNombre = entity.UsuarioNombre,
                Notas = entity.Notas
            };
        }

        public async Task<bool> EliminarCapitalAsync(int capitalId, int empresaId)
        {
            var e = await _context.CrmCapitalMovimientos.FirstOrDefaultAsync(c => c.CapitalId == capitalId && c.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Vendedores ----------

        private static readonly string[] EstadosVendedor = { "Activo", "Inactivo" };

        public async Task<CrmVendedoresResumenDto> ListarVendedoresAsync(int empresaId, bool soloActivos = false)
        {
            var q = _context.CrmVendedores.AsNoTracking().Where(v => v.EmpresaId == empresaId && v.Activo);
            if (soloActivos) q = q.Where(v => v.Estado == "Activo");

            var vendedores = await q.OrderBy(v => v.Nombre).ToListAsync();

            // Las ventas de cada vendedor son las propuestas que efectivamente cerró.
            var propuestas = await _context.CrmPropuestas.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Activo && p.VendedorId != null)
                .Select(p => new { p.VendedorId, p.Estado, p.MontoGs, p.FechaCierre })
                .ToListAsync();

            var sociosIds = vendedores.Where(v => v.SocioId != null).Select(v => v.SocioId!.Value).ToList();
            var socios = sociosIds.Count == 0
                ? new Dictionary<int, CrmSocio>()
                : await _context.CrmSocios.AsNoTracking()
                    .Where(s => s.EmpresaId == empresaId && sociosIds.Contains(s.SocioId))
                    .ToDictionaryAsync(s => s.SocioId);

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var resumen = new CrmVendedoresResumenDto();

            foreach (var v in vendedores)
            {
                var mias = propuestas.Where(p => p.VendedorId == v.VendedorId).ToList();

                var dto = new CrmVendedorDto
                {
                    VendedorId = v.VendedorId,
                    EmpresaId = v.EmpresaId,
                    Nombre = v.Nombre,
                    Email = v.Email,
                    Telefono = v.Telefono,
                    ComisionPct = v.ComisionPct,
                    MetaMensualGs = v.MetaMensualGs,
                    Estado = v.Estado,
                    FechaIngreso = v.FechaIngreso,
                    Notas = v.Notas,
                    EsSocio = v.SocioId != null,
                    SocioId = v.SocioId,
                    FechaSocioDesde = v.FechaSocioDesde,
                    PorcentajeSocio = v.SocioId != null && socios.TryGetValue(v.SocioId.Value, out var s)
                        ? s.Porcentaje
                        : null
                };

                foreach (var p in mias)
                {
                    switch (p.Estado)
                    {
                        case "Ganada":
                            dto.VentasTotalGs += p.MontoGs;
                            dto.PropuestasGanadas++;
                            if (p.FechaCierre.HasValue && p.FechaCierre.Value >= inicioMes && p.FechaCierre.Value < finMes)
                                dto.VentasMesGs += p.MontoGs;
                            break;
                        case "Perdida":
                            dto.PropuestasPerdidas++;
                            break;
                        default:
                            dto.EnNegociacionGs += p.MontoGs;
                            dto.PropuestasAbiertas++;
                            break;
                    }
                }

                dto.ComisionMesGs = Math.Round(dto.VentasMesGs * (v.ComisionPct / 100m), 0);
                dto.CumplimientoMetaPct = v.MetaMensualGs > 0
                    ? (int)Math.Round(dto.VentasMesGs * 100m / v.MetaMensualGs)
                    : null;

                resumen.Vendedores.Add(dto);

                if (v.Estado == "Activo") resumen.CantidadActivos++;
                if (dto.EsSocio) resumen.CantidadSocios++;
                resumen.VentasMesGs += dto.VentasMesGs;
                resumen.VentasTotalGs += dto.VentasTotalGs;
                resumen.ComisionesMesGs += dto.ComisionMesGs;
                resumen.MetaMensualTotalGs += v.MetaMensualGs;
            }

            return resumen;
        }

        public async Task<CrmVendedorDto> UpsertVendedorAsync(CrmVendedorUpsertRequest request, int? vendedorId = null)
        {
            var estado = Normalizar(request.Estado, EstadosVendedor, "Activo");

            CrmVendedor entity;
            if (vendedorId.HasValue)
            {
                entity = await _context.CrmVendedores
                    .FirstOrDefaultAsync(v => v.VendedorId == vendedorId.Value && v.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Vendedor no encontrado");
            }
            else
            {
                entity = new CrmVendedor
                {
                    EmpresaId = request.EmpresaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CrmVendedores.Add(entity);
            }

            entity.Nombre = (request.Nombre ?? "").Trim();
            entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            entity.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
            entity.ComisionPct = Math.Clamp(request.ComisionPct, 0m, 100m);
            entity.MetaMensualGs = Math.Abs(request.MetaMensualGs);
            entity.Estado = estado;
            entity.FechaIngreso = request.FechaIngreso?.Date;
            entity.Notas = request.Notas;

            await _context.SaveChangesAsync();

            // Si ya es socio, mantener el nombre y el email alineados entre ambas fichas.
            if (entity.SocioId.HasValue)
            {
                var socio = await _context.CrmSocios.FirstOrDefaultAsync(s => s.SocioId == entity.SocioId.Value);
                if (socio != null)
                {
                    socio.Nombre = entity.Nombre;
                    socio.Email = entity.Email;
                    await _context.SaveChangesAsync();
                }
            }

            var resumen = await ListarVendedoresAsync(request.EmpresaId);
            return resumen.Vendedores.First(v => v.VendedorId == entity.VendedorId);
        }

        /// <summary>Los dueños deciden que este vendedor pase a ser socio.</summary>
        public async Task<CrmVendedorDto?> PromoverVendedorASocioAsync(int vendedorId, CrmVendedorPromoverRequest request)
        {
            var v = await _context.CrmVendedores
                .FirstOrDefaultAsync(x => x.VendedorId == vendedorId && x.EmpresaId == request.EmpresaId && x.Activo);
            if (v == null) return null;

            var porcentaje = Math.Clamp(request.Porcentaje, 0m, 100m);

            if (v.SocioId.HasValue)
            {
                // Ya era socio: solo se ajusta la participación.
                var existente = await _context.CrmSocios.FirstOrDefaultAsync(s => s.SocioId == v.SocioId.Value);
                if (existente != null)
                {
                    existente.Porcentaje = porcentaje;
                    existente.Activo = true;
                }
            }
            else
            {
                var socio = new CrmSocio
                {
                    EmpresaId = request.EmpresaId,
                    Nombre = v.Nombre,
                    Email = v.Email,
                    Porcentaje = porcentaje,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CrmSocios.Add(socio);
                await _context.SaveChangesAsync();

                v.SocioId = socio.SocioId;
                v.FechaSocioDesde = DateTime.Today;
            }

            await _context.SaveChangesAsync();

            var resumen = await ListarVendedoresAsync(request.EmpresaId);
            return resumen.Vendedores.FirstOrDefault(x => x.VendedorId == vendedorId);
        }

        /// <summary>Revierte la decisión: deja de ser socio pero sigue vendiendo.</summary>
        public async Task<CrmVendedorDto?> QuitarSocioAVendedorAsync(int vendedorId, int empresaId)
        {
            var v = await _context.CrmVendedores
                .FirstOrDefaultAsync(x => x.VendedorId == vendedorId && x.EmpresaId == empresaId && x.Activo);
            if (v == null) return null;

            if (v.SocioId.HasValue)
            {
                var socio = await _context.CrmSocios.FirstOrDefaultAsync(s => s.SocioId == v.SocioId.Value);
                if (socio != null) socio.Activo = false;
                v.SocioId = null;
                v.FechaSocioDesde = null;
                await _context.SaveChangesAsync();
            }

            var resumen = await ListarVendedoresAsync(empresaId);
            return resumen.Vendedores.FirstOrDefault(x => x.VendedorId == vendedorId);
        }

        public async Task<bool> EliminarVendedorAsync(int vendedorId, int empresaId)
        {
            var v = await _context.CrmVendedores
                .FirstOrDefaultAsync(x => x.VendedorId == vendedorId && x.EmpresaId == empresaId);
            if (v == null) return false;
            v.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Propuestas comerciales ----------

        private static readonly string[] EstadosPropuesta = { "Enviada", "Ganada", "Perdida" };

        private static CrmPropuestaDto ToPropuestaDto(CrmPropuesta p, CrmCliente? cliente, CrmVendedor? vendedor = null) => new()
        {
            PropuestaId = p.PropuestaId,
            EmpresaId = p.EmpresaId,
            ClienteId = p.ClienteId,
            ClienteNombre = cliente?.RazonSocial ?? "",
            VendedorId = p.VendedorId,
            VendedorNombre = vendedor?.Nombre,
            Descripcion = p.Descripcion,
            MontoGs = p.MontoGs,
            Estado = p.Estado,
            FechaEnvio = p.FechaEnvio,
            FechaCierre = p.FechaCierre,
            Notas = p.Notas,
            UsuarioNombre = p.UsuarioNombre,
            EstadoPipelineCliente = cliente?.EstadoPipeline
        };

        public async Task<CrmPropuestasResumenDto> ListarPropuestasAsync(int empresaId, int? clienteId = null, string? estado = null)
        {
            var q = _context.CrmPropuestas.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Activo);
            if (clienteId.HasValue) q = q.Where(p => p.ClienteId == clienteId.Value);
            if (!string.IsNullOrWhiteSpace(estado) && EstadosPropuesta.Contains(estado))
                q = q.Where(p => p.Estado == estado);

            var list = await q
                .OrderBy(p => p.Estado == "Enviada" ? 0 : 1)
                .ThenByDescending(p => p.FechaEnvio)
                .ThenByDescending(p => p.PropuestaId)
                .ToListAsync();

            var ids = list.Select(p => p.ClienteId).Distinct().ToList();
            var clientes = await _context.CrmClientes.AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && ids.Contains(c.ClienteId))
                .ToDictionaryAsync(c => c.ClienteId);

            var vendIds = list.Where(p => p.VendedorId != null).Select(p => p.VendedorId!.Value).Distinct().ToList();
            var vendedores = vendIds.Count == 0
                ? new Dictionary<int, CrmVendedor>()
                : await _context.CrmVendedores.AsNoTracking()
                    .Where(v => v.EmpresaId == empresaId && vendIds.Contains(v.VendedorId))
                    .ToDictionaryAsync(v => v.VendedorId);

            var resumen = new CrmPropuestasResumenDto
            {
                Propuestas = list
                    .Select(p => ToPropuestaDto(
                        p,
                        clientes.TryGetValue(p.ClienteId, out var c) ? c : null,
                        p.VendedorId != null && vendedores.TryGetValue(p.VendedorId.Value, out var v) ? v : null))
                    .ToList()
            };

            foreach (var p in list)
            {
                switch (p.Estado)
                {
                    case "Ganada":
                        resumen.MontoGanado += p.MontoGs;
                        resumen.CantidadGanadas++;
                        break;
                    case "Perdida":
                        resumen.MontoPerdido += p.MontoGs;
                        resumen.CantidadPerdidas++;
                        break;
                    default:
                        resumen.MontoEnviado += p.MontoGs;
                        resumen.CantidadEnviadas++;
                        break;
                }
            }

            var cerradas = resumen.CantidadGanadas + resumen.CantidadPerdidas;
            resumen.TasaCierrePct = cerradas > 0
                ? (int)Math.Round(resumen.CantidadGanadas * 100m / cerradas)
                : 0;

            return resumen;
        }

        public async Task<CrmPropuestaDto> UpsertPropuestaAsync(CrmPropuestaUpsertRequest request, int? propuestaId = null)
        {
            var cliente = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.ClienteId == request.ClienteId && c.EmpresaId == request.EmpresaId)
                ?? throw new InvalidOperationException("Cliente no encontrado");

            var estado = Normalizar(request.Estado, EstadosPropuesta, "Enviada");

            CrmPropuesta entity;
            if (propuestaId.HasValue)
            {
                entity = await _context.CrmPropuestas
                    .FirstOrDefaultAsync(p => p.PropuestaId == propuestaId.Value && p.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Propuesta no encontrada");
            }
            else
            {
                entity = new CrmPropuesta
                {
                    EmpresaId = request.EmpresaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CrmPropuestas.Add(entity);
            }

            entity.ClienteId = request.ClienteId;
            entity.VendedorId = request.VendedorId;
            entity.Descripcion = (request.Descripcion ?? "").Trim();
            entity.MontoGs = Math.Abs(request.MontoGs);
            entity.Estado = estado;
            entity.FechaEnvio = (request.FechaEnvio ?? DateTime.Today).Date;
            // Una propuesta cerrada necesita fecha de cierre; si no vino, se asume hoy.
            entity.FechaCierre = estado == "Enviada"
                ? null
                : (request.FechaCierre ?? DateTime.Today).Date;
            entity.Notas = request.Notas;
            entity.UsuarioNombre = request.UsuarioNombre;

            // El estado de la propuesta arrastra al cliente en el pipeline.
            var estadoAnterior = cliente.EstadoPipeline;
            if (estado == "Ganada")
                cliente.EstadoPipeline = "Cliente activo";
            else if (estado == "Perdida")
                cliente.EstadoPipeline = "Cerrado";
            else if (cliente.EstadoPipeline is "Prospecto" or "Contactado" or "En negociacion")
                cliente.EstadoPipeline = "Propuesta enviada";

            if (cliente.EstadoPipeline != estadoAnterior)
            {
                cliente.FechaActualizacion = DateTime.UtcNow;
                _context.CrmHistorial.Add(new CrmHistorial
                {
                    EmpresaId = request.EmpresaId,
                    ClienteId = cliente.ClienteId,
                    Tipo = "cambio_estado",
                    Titulo = $"Propuesta {estado.ToLowerInvariant()}",
                    Detalle = $"{entity.Descripcion} — {entity.MontoGs:N0} Gs.",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = cliente.EstadoPipeline,
                    UsuarioNombre = request.UsuarioNombre,
                    Fecha = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var vendedor = entity.VendedorId.HasValue
                ? await _context.CrmVendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VendedorId == entity.VendedorId.Value)
                : null;
            return ToPropuestaDto(entity, cliente, vendedor);
        }

        public async Task<bool> EliminarPropuestaAsync(int propuestaId, int empresaId)
        {
            var e = await _context.CrmPropuestas
                .FirstOrDefaultAsync(p => p.PropuestaId == propuestaId && p.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Gastos / Cuentas por pagar ----------

        private static readonly string[] CategoriasGasto =
            { "Operativo", "Servicios", "Infraestructura", "Impuestos", "Marketing", "Honorarios", "Otros" };
        private static readonly string[] EstadosGasto = { "Pendiente", "Pagado", "Anulado" };

        private static CrmGastoDto ToGastoDto(CrmGasto g, DateTime hoy)
        {
            var pendiente = g.Estado == "Pendiente";
            int? dias = g.FechaVencimiento.HasValue
                ? (int)(g.FechaVencimiento.Value.Date - hoy).TotalDays
                : null;
            return new CrmGastoDto
            {
                GastoId = g.GastoId,
                EmpresaId = g.EmpresaId,
                Concepto = g.Concepto,
                Categoria = g.Categoria,
                MontoGs = g.MontoGs,
                FechaEmision = g.FechaEmision,
                FechaVencimiento = g.FechaVencimiento,
                FechaPago = g.FechaPago,
                Estado = g.Estado,
                ProveedorId = g.ProveedorId,
                ProveedorNombre = g.ProveedorNombre,
                MetodoPago = g.MetodoPago,
                NroComprobante = g.NroComprobante,
                Notas = g.Notas,
                UsuarioNombre = g.UsuarioNombre,
                Vencido = pendiente && dias.HasValue && dias.Value < 0,
                DiasParaVencer = dias
            };
        }

        public async Task<CrmGastosResumenDto> ListarGastosAsync(int empresaId, string? estado = null)
        {
            var q = _context.CrmGastos.AsNoTracking().Where(g => g.EmpresaId == empresaId && g.Activo);
            if (!string.IsNullOrWhiteSpace(estado) && EstadosGasto.Contains(estado))
                q = q.Where(g => g.Estado == estado);

            var list = await q
                .OrderBy(g => g.Estado == "Pendiente" ? 0 : 1)
                .ThenBy(g => g.FechaVencimiento ?? g.FechaEmision)
                .ThenByDescending(g => g.GastoId)
                .ToListAsync();

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);
            var limite7 = hoy.AddDays(7);

            var resumen = new CrmGastosResumenDto
            {
                Gastos = list.Select(g => ToGastoDto(g, hoy)).ToList()
            };

            foreach (var g in list)
            {
                if (g.Estado == "Pagado")
                {
                    resumen.PagadoTotal += g.MontoGs;
                    if (g.FechaPago.HasValue && g.FechaPago.Value >= inicioMes && g.FechaPago.Value < finMes)
                        resumen.PagadoMesActual += g.MontoGs;
                }
                else if (g.Estado == "Pendiente")
                {
                    resumen.PendienteTotal += g.MontoGs;
                    resumen.CantidadPendientes++;

                    if (g.FechaVencimiento.HasValue)
                    {
                        var venc = g.FechaVencimiento.Value.Date;
                        if (venc < hoy)
                        {
                            resumen.VencidoTotal += g.MontoGs;
                            resumen.CantidadVencidos++;
                        }
                        else if (venc <= limite7)
                        {
                            resumen.VenceProximos7Dias += g.MontoGs;
                            resumen.CantidadVenceProximos7Dias++;
                        }
                    }
                }
            }

            return resumen;
        }

        public async Task<CrmGastoDto> UpsertGastoAsync(CrmGastoUpsertRequest request, int? gastoId = null)
        {
            var estado = Normalizar(request.Estado, EstadosGasto, "Pendiente");
            var categoria = Normalizar(request.Categoria, CategoriasGasto, "Operativo");

            CrmGasto entity;
            if (gastoId.HasValue)
            {
                entity = await _context.CrmGastos
                    .FirstOrDefaultAsync(g => g.GastoId == gastoId.Value && g.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Gasto no encontrado");
            }
            else
            {
                entity = new CrmGasto
                {
                    EmpresaId = request.EmpresaId,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.CrmGastos.Add(entity);
            }

            entity.Concepto = (request.Concepto ?? "").Trim();
            entity.Categoria = categoria;
            entity.MontoGs = Math.Abs(request.MontoGs);
            entity.FechaEmision = (request.FechaEmision ?? DateTime.Today).Date;
            entity.FechaVencimiento = request.FechaVencimiento?.Date;
            entity.Estado = estado;
            // Un gasto pagado necesita fecha de pago; si no vino, se asume hoy.
            entity.FechaPago = estado == "Pagado"
                ? (request.FechaPago ?? DateTime.Today).Date
                : null;
            entity.ProveedorId = request.ProveedorId;
            entity.ProveedorNombre = string.IsNullOrWhiteSpace(request.ProveedorNombre) ? null : request.ProveedorNombre.Trim();
            entity.MetodoPago = string.IsNullOrWhiteSpace(request.MetodoPago) ? null : request.MetodoPago.Trim();
            entity.NroComprobante = string.IsNullOrWhiteSpace(request.NroComprobante) ? null : request.NroComprobante.Trim();
            entity.Notas = request.Notas;
            entity.UsuarioNombre = request.UsuarioNombre;

            await _context.SaveChangesAsync();
            return ToGastoDto(entity, DateTime.Today);
        }

        public async Task<CrmGastoDto?> MarcarGastoPagadoAsync(int gastoId, CrmGastoPagoRequest request)
        {
            var e = await _context.CrmGastos
                .FirstOrDefaultAsync(g => g.GastoId == gastoId && g.EmpresaId == request.EmpresaId && g.Activo);
            if (e == null) return null;

            e.Estado = "Pagado";
            e.FechaPago = (request.FechaPago ?? DateTime.Today).Date;
            if (!string.IsNullOrWhiteSpace(request.MetodoPago))
                e.MetodoPago = request.MetodoPago.Trim();
            if (!string.IsNullOrWhiteSpace(request.UsuarioNombre))
                e.UsuarioNombre = request.UsuarioNombre;

            await _context.SaveChangesAsync();
            return ToGastoDto(e, DateTime.Today);
        }

        public async Task<bool> EliminarGastoAsync(int gastoId, int empresaId)
        {
            var e = await _context.CrmGastos.FirstOrDefaultAsync(g => g.GastoId == gastoId && g.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CrmEmpleadosResumenDto> ListarEmpleadosHqAsync(int empresaId)
        {
            var list = await _context.CrmEmpleadosHq.AsNoTracking()
                .Where(e => e.EmpresaId == empresaId && e.Activo)
                .OrderBy(e => e.Nombre)
                .ToListAsync();
            var dtos = list.Select(e => new CrmEmpleadoHqDto
            {
                EmpleadoHqId = e.EmpleadoHqId,
                EmpresaId = e.EmpresaId,
                Nombre = e.Nombre,
                RolCargo = e.RolCargo,
                Email = e.Email,
                Telefono = e.Telefono,
                SalarioMensualGs = e.SalarioMensualGs,
                FechaIngreso = e.FechaIngreso,
                Estado = e.Estado,
                Notas = e.Notas
            }).ToList();
            var activos = dtos.Where(d => d.Estado == "Activo").ToList();
            return new CrmEmpleadosResumenDto
            {
                CantidadActivos = activos.Count,
                NominaMensualGs = activos.Sum(a => a.SalarioMensualGs),
                Empleados = dtos
            };
        }

        public async Task<CrmEmpleadoHqDto> UpsertEmpleadoHqAsync(CrmEmpleadoHqUpsertRequest request, int? empleadoId = null)
        {
            CrmEmpleadoHq entity;
            if (empleadoId.HasValue)
            {
                entity = await _context.CrmEmpleadosHq.FirstOrDefaultAsync(e =>
                    e.EmpleadoHqId == empleadoId.Value && e.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Empleado no encontrado");
            }
            else
            {
                entity = new CrmEmpleadoHq { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _context.CrmEmpleadosHq.Add(entity);
            }

            entity.Nombre = request.Nombre.Trim();
            entity.RolCargo = string.IsNullOrWhiteSpace(request.RolCargo) ? "Colaborador" : request.RolCargo.Trim();
            entity.Email = request.Email;
            entity.Telefono = request.Telefono;
            entity.SalarioMensualGs = request.SalarioMensualGs;
            entity.FechaIngreso = request.FechaIngreso?.Date;
            entity.Estado = Normalizar(request.Estado, new[] { "Activo", "Inactivo", "Licencia" }, "Activo");
            entity.Notas = request.Notas;
            entity.Activo = true;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new CrmEmpleadoHqDto
            {
                EmpleadoHqId = entity.EmpleadoHqId,
                EmpresaId = entity.EmpresaId,
                Nombre = entity.Nombre,
                RolCargo = entity.RolCargo,
                Email = entity.Email,
                Telefono = entity.Telefono,
                SalarioMensualGs = entity.SalarioMensualGs,
                FechaIngreso = entity.FechaIngreso,
                Estado = entity.Estado,
                Notas = entity.Notas
            };
        }

        public async Task<bool> EliminarEmpleadoHqAsync(int empleadoId, int empresaId)
        {
            var e = await _context.CrmEmpleadosHq.FirstOrDefaultAsync(x => x.EmpleadoHqId == empleadoId && x.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            e.Estado = "Inactivo";
            e.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmObjetivoDto>> ListarObjetivosAsync(int empresaId, string? ambito = null, int? tareaId = null)
        {
            var q = _context.CrmObjetivos.AsNoTracking().Where(o => o.EmpresaId == empresaId && o.Activo);
            if (!string.IsNullOrWhiteSpace(ambito))
                q = q.Where(o => o.Ambito == ambito.Trim());
            if (tareaId.HasValue)
                q = q.Where(o => o.TareaId == tareaId.Value);

            var list = await q.OrderByDescending(o => o.Prioridad == "Alta")
                .ThenBy(o => o.FechaObjetivo ?? DateTime.MaxValue)
                .ToListAsync();

            var clienteIds = list.Where(o => o.ClienteId.HasValue).Select(o => o.ClienteId!.Value).Distinct().ToList();
            var tareaIds = list.Where(o => o.TareaId.HasValue).Select(o => o.TareaId!.Value).Distinct().ToList();
            var clientes = await _context.CrmClientes.AsNoTracking()
                .Where(c => clienteIds.Contains(c.ClienteId))
                .ToDictionaryAsync(c => c.ClienteId, c => c.RazonSocial);
            var tareas = await _context.CrmTareas.AsNoTracking()
                .Where(t => tareaIds.Contains(t.TareaId))
                .ToDictionaryAsync(t => t.TareaId, t => t.Titulo);

            return list.Select(o => new CrmObjetivoDto
            {
                ObjetivoId = o.ObjetivoId,
                EmpresaId = o.EmpresaId,
                Ambito = o.Ambito,
                Titulo = o.Titulo,
                Descripcion = o.Descripcion,
                NotasReunion = o.NotasReunion,
                Estado = o.Estado,
                Prioridad = o.Prioridad,
                FechaObjetivo = o.FechaObjetivo,
                MetaValor = o.MetaValor,
                ValorActual = o.ValorActual,
                AvancePct = o.AvancePct,
                ClienteId = o.ClienteId,
                ClienteNombre = o.ClienteId.HasValue && clientes.ContainsKey(o.ClienteId.Value) ? clientes[o.ClienteId.Value] : null,
                TareaId = o.TareaId,
                TareaTitulo = o.TareaId.HasValue && tareas.ContainsKey(o.TareaId.Value) ? tareas[o.TareaId.Value] : null,
                CreadoPor = o.CreadoPor,
                FechaActualizacion = o.FechaActualizacion
            }).ToList();
        }

        public async Task<CrmObjetivoDto> UpsertObjetivoAsync(CrmObjetivoUpsertRequest request, int? objetivoId = null)
        {
            CrmObjetivo entity;
            if (objetivoId.HasValue)
            {
                entity = await _context.CrmObjetivos.FirstOrDefaultAsync(o =>
                    o.ObjetivoId == objetivoId.Value && o.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Objetivo no encontrado");
            }
            else
            {
                entity = new CrmObjetivo { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _context.CrmObjetivos.Add(entity);
            }

            entity.Ambito = Normalizar(request.Ambito, new[] { "Empresa", "Crm", "Tarea" }, "Empresa");
            entity.Titulo = request.Titulo.Trim();
            entity.Descripcion = request.Descripcion;
            entity.NotasReunion = request.NotasReunion;
            entity.Estado = Normalizar(request.Estado, new[] { "Pendiente", "En curso", "Logrado", "Pausado", "Cancelado" }, "Pendiente");
            entity.Prioridad = Normalizar(request.Prioridad, Prioridades, "Media");
            entity.FechaObjetivo = request.FechaObjetivo?.Date;
            entity.MetaValor = request.MetaValor;
            entity.ValorActual = request.ValorActual;
            entity.AvancePct = request.AvancePct.HasValue
                ? Math.Clamp(request.AvancePct.Value, 0, 100)
                : null;
            entity.ClienteId = request.ClienteId;
            entity.TareaId = request.TareaId;
            if (!string.IsNullOrWhiteSpace(request.CreadoPor))
                entity.CreadoPor = request.CreadoPor;
            entity.Activo = true;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var list = await ListarObjetivosAsync(request.EmpresaId);
            return list.First(o => o.ObjetivoId == entity.ObjetivoId);
        }

        public async Task<bool> EliminarObjetivoAsync(int objetivoId, int empresaId)
        {
            var e = await _context.CrmObjetivos.FirstOrDefaultAsync(o => o.ObjetivoId == objetivoId && o.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            e.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static readonly string[] EstadosProducto = { "Activo", "En desarrollo", "Planificado", "Pausado" };
        private static readonly string[] PrioridadesP = { "P0", "P1", "P2" };
        private static readonly string[] EstadosProveedor = { "Activo", "Inactivo", "Prospecto" };
        private static readonly string[] EstadosProyecto = { "Planificado", "En curso", "Pausado", "Hecho", "Cancelado" };

        public async Task<List<CrmProductoDto>> ListarProductosAsync(int empresaId)
        {
            var list = await _context.CrmProductos.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .OrderBy(p => p.Orden).ThenBy(p => p.Nombre)
                .ToListAsync();
            return list.Select(MapProducto).ToList();
        }

        public async Task<CrmProductoDto> UpsertProductoAsync(CrmProductoUpsertRequest request, int? productoId = null)
        {
            CrmProducto entity;
            if (productoId.HasValue)
            {
                entity = await _context.CrmProductos.FirstOrDefaultAsync(p =>
                    p.ProductoId == productoId.Value && p.EmpresaId == request.EmpresaId && p.Activo)
                    ?? throw new InvalidOperationException("Producto no encontrado");
            }
            else
            {
                entity = new CrmProducto { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _context.CrmProductos.Add(entity);
            }

            entity.Codigo = (request.Codigo ?? "").Trim().ToUpperInvariant();
            entity.Nombre = request.Nombre.Trim();
            entity.Significado = request.Significado;
            entity.QueSera = request.QueSera;
            entity.QueIncluye = request.QueIncluye;
            entity.IdeaCentral = request.IdeaCentral;
            entity.Estado = Normalizar(request.Estado, EstadosProducto, "Planificado");
            entity.Prioridad = Normalizar(request.Prioridad, PrioridadesP, "P2");
            entity.FeeEstimadoGs = request.FeeEstimadoGs;
            entity.Orden = request.Orden;
            entity.Notas = request.Notas;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapProducto(entity);
        }

        public async Task<bool> EliminarProductoAsync(int productoId, int empresaId)
        {
            var e = await _context.CrmProductos.FirstOrDefaultAsync(p => p.ProductoId == productoId && p.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            e.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmProveedorDto>> ListarProveedoresAsync(int empresaId, string? estado = null)
        {
            var q = _context.CrmProveedores.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Activo);
            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(p => p.Estado == estado);
            var list = await q.OrderBy(p => p.Nombre).ToListAsync();
            return list.Select(MapProveedor).ToList();
        }

        public async Task<CrmProveedorDto> UpsertProveedorAsync(CrmProveedorUpsertRequest request, int? proveedorId = null)
        {
            CrmProveedor entity;
            if (proveedorId.HasValue)
            {
                entity = await _context.CrmProveedores.FirstOrDefaultAsync(p =>
                    p.ProveedorId == proveedorId.Value && p.EmpresaId == request.EmpresaId && p.Activo)
                    ?? throw new InvalidOperationException("Proveedor no encontrado");
            }
            else
            {
                entity = new CrmProveedor { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _context.CrmProveedores.Add(entity);
            }

            entity.Nombre = request.Nombre.Trim();
            entity.Categoria = string.IsNullOrWhiteSpace(request.Categoria) ? "General" : request.Categoria.Trim();
            entity.ContactoNombre = request.ContactoNombre;
            entity.Email = request.Email;
            entity.Telefono = request.Telefono;
            entity.Ruc = request.Ruc;
            entity.ProductoCodigo = string.IsNullOrWhiteSpace(request.ProductoCodigo) ? null : request.ProductoCodigo.Trim().ToUpperInvariant();
            entity.Estado = Normalizar(request.Estado, EstadosProveedor, "Activo");
            entity.Notas = request.Notas;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapProveedor(entity);
        }

        public async Task<bool> EliminarProveedorAsync(int proveedorId, int empresaId)
        {
            var e = await _context.CrmProveedores.FirstOrDefaultAsync(p => p.ProveedorId == proveedorId && p.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            e.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmProyectoDto>> ListarProyectosAsync(int empresaId, string? estado = null, string? productoCodigo = null)
        {
            var q = _context.CrmProyectos.AsNoTracking()
                .Include(p => p.Cliente)
                .Where(p => p.EmpresaId == empresaId && p.Activo);
            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(p => p.Estado == estado);
            if (!string.IsNullOrWhiteSpace(productoCodigo))
                q = q.Where(p => p.ProductoCodigo == productoCodigo);
            var list = await q.OrderByDescending(p => p.FechaActualizacion).ToListAsync();
            return list.Select(MapProyecto).ToList();
        }

        public async Task<CrmProyectoDto> UpsertProyectoAsync(CrmProyectoUpsertRequest request, int? proyectoId = null)
        {
            CrmProyecto entity;
            if (proyectoId.HasValue)
            {
                entity = await _context.CrmProyectos.FirstOrDefaultAsync(p =>
                    p.ProyectoId == proyectoId.Value && p.EmpresaId == request.EmpresaId && p.Activo)
                    ?? throw new InvalidOperationException("Proyecto no encontrado");
            }
            else
            {
                entity = new CrmProyecto { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _context.CrmProyectos.Add(entity);
            }

            entity.Titulo = request.Titulo.Trim();
            entity.Descripcion = request.Descripcion;
            entity.ProductoCodigo = string.IsNullOrWhiteSpace(request.ProductoCodigo) ? null : request.ProductoCodigo.Trim().ToUpperInvariant();
            entity.Estado = Normalizar(request.Estado, EstadosProyecto, "Planificado");
            entity.Prioridad = Normalizar(request.Prioridad, PrioridadesP, "P1");
            entity.ClienteId = request.ClienteId;
            entity.FechaInicio = request.FechaInicio?.Date;
            entity.FechaFin = request.FechaFin?.Date;
            entity.ResponsableNombre = request.ResponsableNombre;
            entity.AvancePct = Math.Clamp(request.AvancePct, 0, 100);
            entity.Notas = request.Notas;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (entity.ClienteId.HasValue)
            {
                entity = await _context.CrmProyectos.AsNoTracking()
                    .Include(p => p.Cliente)
                    .FirstAsync(p => p.ProyectoId == entity.ProyectoId);
            }
            return MapProyecto(entity);
        }

        public async Task<bool> EliminarProyectoAsync(int proyectoId, int empresaId)
        {
            var e = await _context.CrmProyectos.FirstOrDefaultAsync(p => p.ProyectoId == proyectoId && p.EmpresaId == empresaId);
            if (e == null) return false;
            e.Activo = false;
            e.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static CrmProductoDto MapProducto(CrmProducto p) => new()
        {
            ProductoId = p.ProductoId,
            EmpresaId = p.EmpresaId,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            Significado = p.Significado,
            QueSera = p.QueSera,
            QueIncluye = p.QueIncluye,
            IdeaCentral = p.IdeaCentral,
            Estado = p.Estado,
            Prioridad = p.Prioridad,
            FeeEstimadoGs = p.FeeEstimadoGs,
            Orden = p.Orden,
            Notas = p.Notas,
        };

        private static CrmProveedorDto MapProveedor(CrmProveedor p) => new()
        {
            ProveedorId = p.ProveedorId,
            EmpresaId = p.EmpresaId,
            Nombre = p.Nombre,
            Categoria = p.Categoria,
            ContactoNombre = p.ContactoNombre,
            Email = p.Email,
            Telefono = p.Telefono,
            Ruc = p.Ruc,
            ProductoCodigo = p.ProductoCodigo,
            Estado = p.Estado,
            Notas = p.Notas,
        };

        private static CrmProyectoDto MapProyecto(CrmProyecto p) => new()
        {
            ProyectoId = p.ProyectoId,
            EmpresaId = p.EmpresaId,
            Titulo = p.Titulo,
            Descripcion = p.Descripcion,
            ProductoCodigo = p.ProductoCodigo,
            Estado = p.Estado,
            Prioridad = p.Prioridad,
            ClienteId = p.ClienteId,
            ClienteNombre = p.Cliente?.RazonSocial,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            ResponsableNombre = p.ResponsableNombre,
            AvancePct = p.AvancePct,
            Notas = p.Notas,
        };
    }
}
