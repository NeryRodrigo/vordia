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
    /// <summary>
    /// VORDIA AGRO — prototipo confinamiento (Agrofield).
    /// Additive: no toca Habitat / Stay / Fontana / HQ CRM.
    /// </summary>
    public partial class AgroService : IAgroService
    {
        private readonly IApplicationDbContext _db;

        public AgroService(IApplicationDbContext db)
        {
            _db = db;
        }

        public Task<object> PingAsync() => Task.FromResult<object>(new
        {
            producto = "VORDIA AGRO",
            clienteDemo = "Agrofield",
            estado = "prototipo",
            mensaje = "Plataforma inteligente de confinamiento agropecuario",
            modulos = new[] { "Ganado", "Alimentacion", "Dispositivos", "Alertas", "Zonas", "Gerencial" },
            hardwarePendiente = new[] { "Collar GPS/LoRa", "RFID", "Balanza", "Camaras IA", "Gateway campo" },
            utc = DateTime.UtcNow
        });

        /// <summary>
        /// Resuelve la empresa Agrofield en GESCO_DB para que usuarios Vordia HQ
        /// (ej. nery.chavez@vordia.com.py) puedan probar el prototipo sin cambiar de login.
        /// </summary>
        public async Task<object?> ResolverContextoAgrofieldAsync()
        {
            var emp = await _db.Empresas.AsNoTracking()
                .Where(e => e.Rubro == "AGRO" || e.RazonSocial == "Agrofield")
                .OrderByDescending(e => e.Rubro == "AGRO")
                .ThenBy(e => e.EmpresaId)
                .FirstOrDefaultAsync();

            if (emp == null) return null;

            var tieneDatos = await _db.AgroEstablecimientos.AsNoTracking()
                .AnyAsync(x => x.EmpresaId == emp.EmpresaId && x.Activo);

            return new
            {
                empresaId = emp.EmpresaId,
                razonSocial = emp.RazonSocial,
                rubro = emp.Rubro,
                cliente = "Agrofield",
                tieneDatos,
                accesoDesde = "Vordia HQ (mismo login)"
            };
        }

        public async Task<List<AgroEstablecimientoDto>> ListarEstablecimientosAsync(int empresaId)
        {
            return await _db.AgroEstablecimientos.AsNoTracking()
                .Where(e => e.EmpresaId == empresaId && e.Activo)
                .OrderBy(e => e.Nombre)
                .Select(e => new AgroEstablecimientoDto
                {
                    EstablecimientoId = e.EstablecimientoId,
                    Nombre = e.Nombre,
                    ClienteNombre = e.ClienteNombre,
                    Ubicacion = e.Ubicacion,
                    Hectareas = e.Hectareas
                }).ToListAsync();
        }

        public async Task<AgroDashboardDto?> DashboardAsync(int empresaId, int? establecimientoId = null)
        {
            var est = await _db.AgroEstablecimientos.AsNoTracking()
                .Where(e => e.EmpresaId == empresaId && e.Activo)
                .Where(e => !establecimientoId.HasValue || e.EstablecimientoId == establecimientoId.Value)
                .OrderBy(e => e.EstablecimientoId)
                .FirstOrDefaultAsync();
            if (est == null) return null;

            var animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.EstablecimientoId == est.EstablecimientoId && a.Activo)
                .ToListAsync();
            var lotes = await _db.AgroLotes.AsNoTracking()
                .CountAsync(l => l.EmpresaId == empresaId && l.EstablecimientoId == est.EstablecimientoId && l.Activo);
            var alertas = await _db.AgroAlertas.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.EstablecimientoId == est.EstablecimientoId && a.Activo && !a.Resuelta)
                .OrderByDescending(a => a.FechaAlerta)
                .Take(8)
                .ToListAsync();
            var dispositivos = await _db.AgroDispositivos.AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && d.EstablecimientoId == est.EstablecimientoId && d.Activo)
                .ToListAsync();

            var desde7 = DateTime.UtcNow.AddDays(-7);
            var loteIds = await _db.AgroLotes.AsNoTracking()
                .Where(l => l.EstablecimientoId == est.EstablecimientoId)
                .Select(l => l.LoteId).ToListAsync();
            var alimento7 = await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo && loteIds.Contains(r.LoteId) && r.FechaEntrega >= desde7)
                .SumAsync(r => (decimal?)r.CantidadKg) ?? 0;

            var gpds = animales
                .Where(a => a.PesoIngresoKg.HasValue && a.PesoActualKg.HasValue && a.FechaIngreso.HasValue)
                .Select(a =>
                {
                    var dias = Math.Max(1, (int)(DateTime.UtcNow.Date - a.FechaIngreso!.Value.Date).TotalDays);
                    return (a.PesoActualKg!.Value - a.PesoIngresoKg!.Value) / dias;
                }).ToList();

            var gananciaTotal = animales
                .Where(a => a.PesoIngresoKg.HasValue && a.PesoActualKg.HasValue)
                .Sum(a => a.PesoActualKg!.Value - a.PesoIngresoKg!.Value);
            decimal? conversion = gananciaTotal > 0 ? Math.Round(alimento7 / gananciaTotal, 2) : null;

            var animalCodigos = animales.ToDictionary(a => a.AnimalId, a => a.CodigoInterno);

            return new AgroDashboardDto
            {
                EstablecimientoId = est.EstablecimientoId,
                Nombre = est.Nombre,
                ClienteNombre = est.ClienteNombre,
                Ubicacion = est.Ubicacion,
                Hectareas = est.Hectareas,
                AnimalesActivos = animales.Count(a => a.Estado == "Activo" || a.Estado == "Tratamiento" || a.Estado == "FueraZona"),
                AnimalesFueraZona = animales.Count(a => a.Estado == "FueraZona"),
                AnimalesTratamiento = animales.Count(a => a.Estado == "Tratamiento"),
                Lotes = lotes,
                AlertasAbiertas = alertas.Count,
                DispositivosOnline = dispositivos.Count(d => d.Estado == "Online"),
                DispositivosAlerta = dispositivos.Count(d => d.Estado != "Online"),
                AlimentoKg7d = alimento7,
                GpdPromedioKg = gpds.Count > 0 ? Math.Round(gpds.Average(), 2) : null,
                ConversionEstimada = conversion,
                UltimasAlertas = alertas.Select(a => new AgroAlertaDto
                {
                    AlertaId = a.AlertaId,
                    AnimalId = a.AnimalId,
                    AnimalCodigo = a.AnimalId.HasValue && animalCodigos.ContainsKey(a.AnimalId.Value) ? animalCodigos[a.AnimalId.Value] : null,
                    Tipo = a.Tipo,
                    Severidad = a.Severidad,
                    Titulo = a.Titulo,
                    Detalle = a.Detalle,
                    Resuelta = a.Resuelta,
                    FechaAlerta = a.FechaAlerta
                }).ToList()
            };
        }

        public async Task<List<AgroAnimalDto>> ListarAnimalesAsync(int empresaId, int? establecimientoId = null, string? estado = null, string? etapa = null)
        {
            var q = _db.AgroAnimales.AsNoTracking().Where(a => a.EmpresaId == empresaId && a.Activo);
            if (establecimientoId.HasValue) q = q.Where(a => a.EstablecimientoId == establecimientoId.Value);
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(a => a.Estado == estado);
            if (!string.IsNullOrWhiteSpace(etapa)) q = q.Where(a => a.Etapa == etapa);

            var list = await q.OrderBy(a => a.CodigoInterno).ToListAsync();
            var loteIds = list.Where(a => a.LoteId.HasValue).Select(a => a.LoteId!.Value).Distinct().ToList();
            var lotes = await _db.AgroLotes.AsNoTracking()
                .Where(l => loteIds.Contains(l.LoteId))
                .ToDictionaryAsync(l => l.LoteId, l => l.Codigo);

            var dtos = list.Select(a => MapAnimal(a, a.LoteId.HasValue && lotes.ContainsKey(a.LoteId.Value) ? lotes[a.LoteId.Value] : null)).ToList();
            await EnriquecerAnimalesAsync(empresaId, dtos);
            return dtos;
        }

        public async Task<AgroAnimalDto> UpsertAnimalAsync(AgroAnimalUpsertRequest request, int? animalId = null)
        {
            AgroAnimal entity;
            if (animalId.HasValue)
            {
                entity = await _db.AgroAnimales.FirstOrDefaultAsync(a =>
                    a.AnimalId == animalId.Value && a.EmpresaId == request.EmpresaId && a.Activo)
                    ?? throw new InvalidOperationException("Animal no encontrado");
            }
            else
            {
                entity = new AgroAnimal
                {
                    EmpresaId = request.EmpresaId,
                    EstablecimientoId = request.EstablecimientoId,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.AgroAnimales.Add(entity);
            }

            entity.LoteId = request.LoteId;
            entity.CodigoInterno = request.CodigoInterno.Trim();
            entity.RfidTag = request.RfidTag;
            entity.CollarId = request.CollarId;
            entity.Categoria = string.IsNullOrWhiteSpace(request.Categoria) ? "Novillo" : request.Categoria.Trim();
            entity.Sexo = request.Sexo;
            entity.FechaIngreso = request.FechaIngreso?.Date;
            entity.PesoIngresoKg = request.PesoIngresoKg;
            if (request.PesoActualKg.HasValue) entity.PesoActualKg = request.PesoActualKg;
            entity.Estado = string.IsNullOrWhiteSpace(request.Estado) ? "Activo" : request.Estado.Trim();
            entity.Notas = request.Notas;
            if (!string.IsNullOrWhiteSpace(request.Etapa))
            {
                var etapa = NormalizarEtapa(request.Etapa);
                if (entity.Etapa != etapa || !entity.FechaEtapa.HasValue) entity.FechaEtapa = DateTime.UtcNow;
                entity.Etapa = etapa;
            }
            if (request.Raza != null) entity.Raza = request.Raza;
            if (request.FechaNacimiento.HasValue) entity.FechaNacimiento = request.FechaNacimiento.Value.Date;
            if (request.PesoNacimientoKg.HasValue) entity.PesoNacimientoKg = request.PesoNacimientoKg;
            if (request.MadreCodigo != null) entity.MadreCodigo = request.MadreCodigo;
            if (request.PadreCodigo != null) entity.PadreCodigo = request.PadreCodigo;
            if (request.PesoObjetivoKg.HasValue) entity.PesoObjetivoKg = request.PesoObjetivoKg;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            string? loteCodigo = null;
            if (entity.LoteId.HasValue)
            {
                loteCodigo = await _db.AgroLotes.AsNoTracking()
                    .Where(l => l.LoteId == entity.LoteId).Select(l => l.Codigo).FirstOrDefaultAsync();
            }
            return MapAnimal(entity, loteCodigo);
        }

        public async Task<List<AgroPesajeDto>> ListarPesajesAsync(int empresaId, int animalId)
        {
            var animal = await _db.AgroAnimales.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AnimalId == animalId && a.EmpresaId == empresaId);
            var list = await _db.AgroPesajes.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.AnimalId == animalId && p.Activo)
                .OrderByDescending(p => p.FechaPesaje)
                .ToListAsync();
            return list.Select(p => new AgroPesajeDto
            {
                PesajeId = p.PesajeId,
                AnimalId = p.AnimalId,
                AnimalCodigo = animal?.CodigoInterno,
                PesoKg = p.PesoKg,
                FechaPesaje = p.FechaPesaje,
                Origen = p.Origen,
                Operario = p.Operario
            }).ToList();
        }

        public async Task<AgroPesajeDto> RegistrarPesajeAsync(AgroPesajeCreateRequest request)
        {
            var animal = await _db.AgroAnimales.FirstOrDefaultAsync(a =>
                a.AnimalId == request.AnimalId && a.EmpresaId == request.EmpresaId && a.Activo)
                ?? throw new InvalidOperationException("Animal no encontrado");

            var pesaje = new AgroPesaje
            {
                EmpresaId = request.EmpresaId,
                AnimalId = request.AnimalId,
                PesoKg = request.PesoKg,
                FechaPesaje = request.FechaPesaje ?? DateTime.UtcNow,
                Origen = string.IsNullOrWhiteSpace(request.Origen) ? "Manual" : request.Origen,
                Operario = request.Operario,
                Notas = request.Notas,
                Activo = true
            };
            _db.AgroPesajes.Add(pesaje);
            animal.PesoActualKg = request.PesoKg;
            animal.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new AgroPesajeDto
            {
                PesajeId = pesaje.PesajeId,
                AnimalId = pesaje.AnimalId,
                AnimalCodigo = animal.CodigoInterno,
                PesoKg = pesaje.PesoKg,
                FechaPesaje = pesaje.FechaPesaje,
                Origen = pesaje.Origen,
                Operario = pesaje.Operario
            };
        }

        public async Task<List<AgroRacionDto>> ListarRacionesAsync(int empresaId, int? loteId = null)
        {
            var q = _db.AgroRaciones.AsNoTracking().Where(r => r.EmpresaId == empresaId && r.Activo);
            if (loteId.HasValue) q = q.Where(r => r.LoteId == loteId.Value);
            var list = await q.OrderByDescending(r => r.FechaEntrega).Take(100).ToListAsync();
            var loteIds = list.Select(r => r.LoteId).Distinct().ToList();
            var lotes = await _db.AgroLotes.AsNoTracking()
                .Where(l => loteIds.Contains(l.LoteId))
                .ToDictionaryAsync(l => l.LoteId, l => l.Codigo);

            return list.Select(r => new AgroRacionDto
            {
                RacionId = r.RacionId,
                LoteId = r.LoteId,
                LoteCodigo = lotes.ContainsKey(r.LoteId) ? lotes[r.LoteId] : null,
                TipoAlimento = r.TipoAlimento,
                CantidadKg = r.CantidadKg,
                FechaEntrega = r.FechaEntrega,
                Operario = r.Operario,
                Vehiculo = r.Vehiculo,
                CostoGs = r.CostoGs
            }).ToList();
        }

        public async Task<AgroRacionDto> RegistrarRacionAsync(AgroRacionCreateRequest request)
        {
            var lote = await _db.AgroLotes.AsNoTracking()
                .FirstOrDefaultAsync(l => l.LoteId == request.LoteId && l.EmpresaId == request.EmpresaId && l.Activo)
                ?? throw new InvalidOperationException("Lote no encontrado");

            var r = new AgroRacion
            {
                EmpresaId = request.EmpresaId,
                LoteId = request.LoteId,
                TipoAlimento = request.TipoAlimento.Trim(),
                CantidadKg = request.CantidadKg,
                FechaEntrega = request.FechaEntrega ?? DateTime.UtcNow,
                Operario = request.Operario,
                Vehiculo = request.Vehiculo,
                CostoGs = request.CostoGs,
                Notas = request.Notas,
                Activo = true
            };
            _db.AgroRaciones.Add(r);
            await _db.SaveChangesAsync();

            return new AgroRacionDto
            {
                RacionId = r.RacionId,
                LoteId = r.LoteId,
                LoteCodigo = lote.Codigo,
                TipoAlimento = r.TipoAlimento,
                CantidadKg = r.CantidadKg,
                FechaEntrega = r.FechaEntrega,
                Operario = r.Operario,
                Vehiculo = r.Vehiculo,
                CostoGs = r.CostoGs
            };
        }

        public async Task<List<AgroLoteDto>> ListarLotesAsync(int empresaId, int? establecimientoId = null)
        {
            var q = _db.AgroLotes.AsNoTracking().Where(l => l.EmpresaId == empresaId && l.Activo);
            if (establecimientoId.HasValue) q = q.Where(l => l.EstablecimientoId == establecimientoId.Value);
            var lotes = await q.OrderBy(l => l.Codigo).ToListAsync();
            var counts = await _db.AgroAnimales.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.Activo && a.LoteId != null && a.Etapa != "Vendido" && a.Etapa != "Baja")
                .GroupBy(a => a.LoteId!.Value)
                .Select(g => new { LoteId = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.LoteId, x => x.N);

            return lotes.Select(l => new AgroLoteDto
            {
                LoteId = l.LoteId,
                EstablecimientoId = l.EstablecimientoId,
                Codigo = l.Codigo,
                Nombre = l.Nombre,
                Tipo = l.Tipo,
                Capacidad = l.Capacidad,
                CamaraCodigo = l.CamaraCodigo,
                Animales = counts.ContainsKey(l.LoteId) ? counts[l.LoteId] : 0,
                Etapa = l.Etapa,
                PesoObjetivoKg = l.PesoObjetivoKg
            }).ToList();
        }

        public async Task<List<AgroDispositivoDto>> ListarDispositivosAsync(int empresaId, int? establecimientoId = null)
        {
            var q = _db.AgroDispositivos.AsNoTracking().Where(d => d.EmpresaId == empresaId && d.Activo);
            if (establecimientoId.HasValue) q = q.Where(d => d.EstablecimientoId == establecimientoId.Value);
            var list = await q.OrderBy(d => d.Tipo).ThenBy(d => d.Codigo).ToListAsync();
            var animalIds = list.Where(d => d.AnimalId.HasValue).Select(d => d.AnimalId!.Value).Distinct().ToList();
            var animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => animalIds.Contains(a.AnimalId))
                .ToDictionaryAsync(a => a.AnimalId, a => a.CodigoInterno);

            return list.Select(d => new AgroDispositivoDto
            {
                DispositivoId = d.DispositivoId,
                Codigo = d.Codigo,
                Tipo = d.Tipo,
                AnimalId = d.AnimalId,
                AnimalCodigo = d.AnimalId.HasValue && animales.ContainsKey(d.AnimalId.Value) ? animales[d.AnimalId.Value] : null,
                Estado = d.Estado,
                BateriaPct = d.BateriaPct,
                UltimoPingAt = d.UltimoPingAt
            }).ToList();
        }

        public async Task<List<AgroAlertaDto>> ListarAlertasAsync(int empresaId, int? establecimientoId = null, bool soloAbiertas = true)
        {
            var q = _db.AgroAlertas.AsNoTracking().Where(a => a.EmpresaId == empresaId && a.Activo);
            if (establecimientoId.HasValue) q = q.Where(a => a.EstablecimientoId == establecimientoId.Value);
            if (soloAbiertas) q = q.Where(a => !a.Resuelta);
            var list = await q.OrderByDescending(a => a.FechaAlerta).Take(50).ToListAsync();
            var animalIds = list.Where(a => a.AnimalId.HasValue).Select(a => a.AnimalId!.Value).Distinct().ToList();
            var animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => animalIds.Contains(a.AnimalId))
                .ToDictionaryAsync(a => a.AnimalId, a => a.CodigoInterno);

            return list.Select(a => new AgroAlertaDto
            {
                AlertaId = a.AlertaId,
                AnimalId = a.AnimalId,
                AnimalCodigo = a.AnimalId.HasValue && animales.ContainsKey(a.AnimalId.Value) ? animales[a.AnimalId.Value] : null,
                Tipo = a.Tipo,
                Severidad = a.Severidad,
                Titulo = a.Titulo,
                Detalle = a.Detalle,
                Resuelta = a.Resuelta,
                FechaAlerta = a.FechaAlerta
            }).ToList();
        }

        public async Task<bool> ResolverAlertaAsync(int alertaId, int empresaId)
        {
            var a = await _db.AgroAlertas.FirstOrDefaultAsync(x => x.AlertaId == alertaId && x.EmpresaId == empresaId && x.Activo);
            if (a == null) return false;
            a.Resuelta = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<AgroZonaDto>> ListarZonasAsync(int empresaId, int? establecimientoId = null)
        {
            var q = _db.AgroZonas.AsNoTracking().Where(z => z.EmpresaId == empresaId && z.Activo);
            if (establecimientoId.HasValue) q = q.Where(z => z.EstablecimientoId == establecimientoId.Value);
            return await q.OrderBy(z => z.Nombre).Select(z => new AgroZonaDto
            {
                ZonaId = z.ZonaId,
                Nombre = z.Nombre,
                Tipo = z.Tipo,
                LatCentro = z.LatCentro,
                LngCentro = z.LngCentro,
                RadioMetros = z.RadioMetros
            }).ToListAsync();
        }

        public async Task<AgroAnimalDto?> SimularUbicacionAsync(AgroSimularUbicacionRequest request)
        {
            var animal = await _db.AgroAnimales.FirstOrDefaultAsync(a =>
                a.AnimalId == request.AnimalId && a.EmpresaId == request.EmpresaId && a.Activo);
            if (animal == null) return null;

            animal.UltimaLat = request.Lat;
            animal.UltimaLng = request.Lng;
            animal.UltimaUbicacionAt = DateTime.UtcNow;
            if (request.BateriaPct.HasValue) animal.BateriaCollarPct = request.BateriaPct;

            if (request.GenerarAlertaFueraZona)
            {
                animal.Estado = "FueraZona";
                _db.AgroAlertas.Add(new AgroAlerta
                {
                    EmpresaId = request.EmpresaId,
                    EstablecimientoId = animal.EstablecimientoId,
                    AnimalId = animal.AnimalId,
                    Tipo = "FueraZona",
                    Severidad = "Alta",
                    Titulo = $"Animal {animal.CodigoInterno} fuera de zona (simulado)",
                    Detalle = $"Lat {request.Lat}, Lng {request.Lng}. Evento simulado — futuro: collar GPS/LoRa.",
                    Resuelta = false,
                    FechaAlerta = DateTime.UtcNow,
                    Activo = true
                });
            }

            var disp = await _db.AgroDispositivos.FirstOrDefaultAsync(d =>
                d.EmpresaId == request.EmpresaId && d.AnimalId == animal.AnimalId && d.Activo && d.Tipo == "Collar");
            if (disp != null)
            {
                disp.UltimoPingAt = DateTime.UtcNow;
                if (request.BateriaPct.HasValue) disp.BateriaPct = request.BateriaPct;
                if (request.BateriaPct.HasValue && request.BateriaPct < 25) disp.Estado = "Alerta";
                else disp.Estado = "Online";
            }

            animal.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            string? loteCodigo = null;
            if (animal.LoteId.HasValue)
            {
                loteCodigo = await _db.AgroLotes.AsNoTracking()
                    .Where(l => l.LoteId == animal.LoteId).Select(l => l.Codigo).FirstOrDefaultAsync();
            }
            return MapAnimal(animal, loteCodigo);
        }

        private static AgroAnimalDto MapAnimal(AgroAnimal a, string? loteCodigo)
        {
            int? dias = null;
            decimal? gpd = null;
            decimal? ganancia = null;
            if (a.PesoIngresoKg.HasValue && a.PesoActualKg.HasValue)
                ganancia = a.PesoActualKg.Value - a.PesoIngresoKg.Value;
            if (a.FechaIngreso.HasValue)
            {
                dias = Math.Max(1, (int)(DateTime.UtcNow.Date - a.FechaIngreso.Value.Date).TotalDays);
                if (ganancia.HasValue) gpd = Math.Round(ganancia.Value / dias.Value, 2);
            }

            return new AgroAnimalDto
            {
                AnimalId = a.AnimalId,
                EstablecimientoId = a.EstablecimientoId,
                LoteId = a.LoteId,
                LoteCodigo = loteCodigo,
                CodigoInterno = a.CodigoInterno,
                RfidTag = a.RfidTag,
                CollarId = a.CollarId,
                Categoria = a.Categoria,
                Sexo = a.Sexo,
                FechaIngreso = a.FechaIngreso,
                PesoIngresoKg = a.PesoIngresoKg,
                PesoActualKg = a.PesoActualKg,
                GananciaKg = ganancia,
                GpdKg = gpd,
                DiasEnEstablecimiento = dias,
                Estado = a.Estado,
                UltimaLat = a.UltimaLat,
                UltimaLng = a.UltimaLng,
                UltimaUbicacionAt = a.UltimaUbicacionAt,
                BateriaCollarPct = a.BateriaCollarPct,
                Notas = a.Notas,
                Etapa = a.Etapa,
                FechaEtapa = a.FechaEtapa,
                Raza = a.Raza,
                FechaNacimiento = a.FechaNacimiento,
                PesoNacimientoKg = a.PesoNacimientoKg,
                MadreCodigo = a.MadreCodigo,
                PadreCodigo = a.PadreCodigo,
                PesoObjetivoKg = a.PesoObjetivoKg
            };
        }
    }
}
