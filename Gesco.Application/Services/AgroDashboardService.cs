using Gesco.Application.DTOs;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    /// <summary>
    /// VORDIA AGRO — dashboard principal, inventario de insumos y lotes.
    /// El stock de alimento se descuenta solo con las raciones entregadas (mismo nombre que el insumo).
    /// </summary>
    public partial class AgroService
    {
        private static readonly string[] TiposInsumo = { "Alimento", "Medicamento", "Otro" };
        private static readonly string[] TiposMovimiento = { "Ingreso", "Consumo", "Ajuste" };

        // ───────────────────────── Inventario ─────────────────────────

        private async Task<List<AgroInsumoDto>> CalcularInventarioAsync(int empresaId, int establecimientoId)
        {
            var insumos = await _db.AgroInsumos.AsNoTracking()
                .Where(i => i.EmpresaId == empresaId && i.EstablecimientoId == establecimientoId && i.Activo)
                .OrderBy(i => i.Tipo).ThenBy(i => i.Nombre).ToListAsync();
            if (insumos.Count == 0) return new();

            var ids = insumos.Select(i => i.InsumoId).ToList();
            var movs = await _db.AgroInsumoMovimientos.AsNoTracking()
                .Where(m => ids.Contains(m.InsumoId) && m.Activo).ToListAsync();

            var desde = insumos.Min(i => i.FechaCreacion);
            var nombresAlimento = insumos.Where(i => i.Tipo == "Alimento").Select(i => i.Nombre).ToList();
            var loteIds = await _db.AgroLotes.AsNoTracking().Where(l => l.EstablecimientoId == establecimientoId).Select(l => l.LoteId).ToListAsync();
            var raciones = nombresAlimento.Count == 0 ? new List<AgroRacion>() : await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo && loteIds.Contains(r.LoteId) && r.FechaEntrega >= desde && nombresAlimento.Contains(r.TipoAlimento))
                .ToListAsync();

            var hace7 = DateTime.UtcNow.AddDays(-7);
            var hace30 = DateTime.UtcNow.AddDays(-30);
            return insumos.Select(i =>
            {
                var m = movs.Where(x => x.InsumoId == i.InsumoId).ToList();
                decimal Signo(AgroInsumoMovimiento x) => x.Tipo == "Consumo" ? -x.Cantidad : x.Cantidad;
                var stock = m.Sum(Signo);
                decimal consumoDiario;
                if (i.Tipo == "Alimento")
                {
                    var propias = raciones.Where(r => string.Equals(r.TipoAlimento, i.Nombre, StringComparison.OrdinalIgnoreCase) && r.FechaEntrega >= i.FechaCreacion).ToList();
                    stock -= propias.Sum(r => r.CantidadKg);
                    consumoDiario = (propias.Where(r => r.FechaEntrega >= hace7).Sum(r => r.CantidadKg)
                                     + m.Where(x => x.Tipo == "Consumo" && x.Fecha >= hace7).Sum(x => x.Cantidad)) / 7m;
                }
                else
                {
                    consumoDiario = m.Where(x => x.Tipo == "Consumo" && x.Fecha >= hace30).Sum(x => x.Cantidad) / 30m;
                }
                var ingresos = m.Where(x => x.Tipo == "Ingreso").ToList();
                var conCosto = ingresos.Where(x => x.CostoUnitarioGs > 0).ToList();
                return new AgroInsumoDto
                {
                    InsumoId = i.InsumoId,
                    Nombre = i.Nombre,
                    Tipo = i.Tipo,
                    Unidad = i.Unidad,
                    Stock = R(stock, 1),
                    ConsumoDiario = R(consumoDiario, 1),
                    DiasRestantes = consumoDiario > 0 ? R(Math.Max(0, stock) / consumoDiario, 1) : null,
                    StockMinimo = i.StockMinimo,
                    BajoMinimo = stock < i.StockMinimo,
                    CostoPromedioGs = conCosto.Count > 0 ? R(conCosto.Sum(x => x.Cantidad * x.CostoUnitarioGs!.Value) / conCosto.Sum(x => x.Cantidad), 0) : null,
                    UltimoIngreso = ingresos.Count > 0 ? ingresos.Max(x => x.Fecha) : null
                };
            }).ToList();
        }

        private async Task<AgroEstablecimiento> EstablecimientoAsync(int empresaId) =>
            await _db.AgroEstablecimientos.AsNoTracking().Where(e => e.EmpresaId == empresaId && e.Activo)
                .OrderBy(e => e.EstablecimientoId).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Sin establecimiento Agro");

        public async Task<List<AgroInsumoDto>> ListarInventarioAsync(int empresaId)
        {
            var est = await _db.AgroEstablecimientos.AsNoTracking().Where(e => e.EmpresaId == empresaId && e.Activo)
                .OrderBy(e => e.EstablecimientoId).FirstOrDefaultAsync();
            return est == null ? new() : await CalcularInventarioAsync(empresaId, est.EstablecimientoId);
        }

        public async Task<AgroInsumoDto> CrearInsumoAsync(AgroInsumoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre)) throw new InvalidOperationException("Indicá el nombre del insumo");
            var tipo = TiposInsumo.FirstOrDefault(t => t.Equals(request.Tipo, StringComparison.OrdinalIgnoreCase))
                       ?? throw new InvalidOperationException("Tipo inválido");
            var est = await EstablecimientoAsync(request.EmpresaId);
            var nombre = request.Nombre.Trim();
            if (await _db.AgroInsumos.AnyAsync(i => i.EstablecimientoId == est.EstablecimientoId && i.Activo && i.Nombre == nombre))
                throw new InvalidOperationException("Ya existe un insumo con ese nombre");

            var insumo = new AgroInsumo
            {
                EmpresaId = request.EmpresaId,
                EstablecimientoId = est.EstablecimientoId,
                Nombre = nombre.Length > 120 ? nombre[..120] : nombre,
                Tipo = tipo,
                Unidad = string.IsNullOrWhiteSpace(request.Unidad) ? "kg" : request.Unidad.Trim(),
                StockMinimo = Math.Max(0, request.StockMinimo),
                FechaCreacion = DateTime.UtcNow
            };
            _db.AgroInsumos.Add(insumo);
            await _db.SaveChangesAsync();
            if (request.StockInicial is > 0)
            {
                _db.AgroInsumoMovimientos.Add(new AgroInsumoMovimiento
                {
                    EmpresaId = request.EmpresaId,
                    InsumoId = insumo.InsumoId,
                    Tipo = "Ingreso",
                    Cantidad = request.StockInicial.Value,
                    CostoUnitarioGs = request.CostoUnitarioGs,
                    Referencia = "Stock inicial",
                    Fecha = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            return (await CalcularInventarioAsync(request.EmpresaId, est.EstablecimientoId)).First(i => i.InsumoId == insumo.InsumoId);
        }

        public async Task<List<AgroMovimientoDto>> ListarMovimientosAsync(int empresaId, int insumoId) =>
            await _db.AgroInsumoMovimientos.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && m.InsumoId == insumoId && m.Activo)
                .OrderByDescending(m => m.Fecha).Take(100)
                .Select(m => new AgroMovimientoDto
                {
                    MovimientoId = m.MovimientoId,
                    Tipo = m.Tipo,
                    Cantidad = m.Cantidad,
                    CostoUnitarioGs = m.CostoUnitarioGs,
                    Fecha = m.Fecha,
                    Referencia = m.Referencia,
                    Operario = m.Operario
                }).ToListAsync();

        public async Task<AgroInsumoDto> RegistrarMovimientoAsync(int insumoId, AgroMovimientoRequest request)
        {
            var insumo = await _db.AgroInsumos.AsNoTracking().FirstOrDefaultAsync(i => i.InsumoId == insumoId && i.EmpresaId == request.EmpresaId && i.Activo)
                         ?? throw new InvalidOperationException("Insumo no encontrado");
            var tipo = TiposMovimiento.FirstOrDefault(t => t.Equals(request.Tipo, StringComparison.OrdinalIgnoreCase))
                       ?? throw new InvalidOperationException("Tipo de movimiento inválido");
            if (request.Cantidad == 0 || (tipo != "Ajuste" && request.Cantidad < 0))
                throw new InvalidOperationException("Indicá una cantidad válida");

            _db.AgroInsumoMovimientos.Add(new AgroInsumoMovimiento
            {
                EmpresaId = request.EmpresaId,
                InsumoId = insumoId,
                Tipo = tipo,
                Cantidad = request.Cantidad,
                CostoUnitarioGs = request.CostoUnitarioGs,
                Referencia = request.Referencia?.Trim() is { Length: > 200 } r ? r[..200] : request.Referencia?.Trim(),
                Operario = request.Operario?.Trim() is { Length: > 120 } o ? o[..120] : request.Operario?.Trim(),
                Fecha = DateTime.UtcNow,
                Activo = true
            });
            await _db.SaveChangesAsync();
            return (await CalcularInventarioAsync(request.EmpresaId, insumo.EstablecimientoId)).First(i => i.InsumoId == insumoId);
        }

        // ───────────────────────── Lotes ─────────────────────────

        public async Task<AgroLoteDto> GuardarLoteAsync(AgroLoteRequest request, int? loteId = null)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Nombre))
                throw new InvalidOperationException("Código y nombre son obligatorios");
            var est = await EstablecimientoAsync(request.EmpresaId);
            var codigo = request.Codigo.Trim().ToUpperInvariant();
            if (await _db.AgroLotes.AnyAsync(l => l.EstablecimientoId == est.EstablecimientoId && l.Activo && l.Codigo == codigo && l.LoteId != (loteId ?? 0)))
                throw new InvalidOperationException($"Ya existe el lote {codigo}");

            string? etapa = null;
            if (!string.IsNullOrWhiteSpace(request.Etapa))
            {
                etapa = NormalizarEtapa(request.Etapa);
                if (etapa is not ("Reproductor" or "Cria" or "Recria" or "Engorde"))
                    throw new InvalidOperationException("Un lote puede ser de reproductores, cría, recría o engorde");
            }

            AgroLote lote;
            if (loteId.HasValue)
            {
                lote = await _db.AgroLotes.FirstOrDefaultAsync(l => l.LoteId == loteId && l.EmpresaId == request.EmpresaId && l.Activo)
                       ?? throw new InvalidOperationException("Lote no encontrado");
            }
            else
            {
                lote = new AgroLote { EmpresaId = request.EmpresaId, EstablecimientoId = est.EstablecimientoId, FechaCreacion = DateTime.UtcNow };
                _db.AgroLotes.Add(lote);
            }
            lote.Codigo = codigo.Length > 40 ? codigo[..40] : codigo;
            lote.Nombre = request.Nombre.Trim().Length > 120 ? request.Nombre.Trim()[..120] : request.Nombre.Trim();
            lote.Tipo = request.Tipo is "Corral" or "Potrero" or "Manga" ? request.Tipo : "Corral";
            lote.Capacidad = Math.Max(0, request.Capacidad);
            lote.Etapa = etapa;
            lote.PesoObjetivoKg = request.PesoObjetivoKg is > 0 ? request.PesoObjetivoKg : null;
            await _db.SaveChangesAsync();

            var animales = await _db.AgroAnimales.CountAsync(a => a.LoteId == lote.LoteId && a.Activo && a.Etapa != "Vendido" && a.Etapa != "Baja");
            return new AgroLoteDto
            {
                LoteId = lote.LoteId,
                EstablecimientoId = lote.EstablecimientoId,
                Codigo = lote.Codigo,
                Nombre = lote.Nombre,
                Tipo = lote.Tipo,
                Capacidad = lote.Capacidad,
                CamaraCodigo = lote.CamaraCodigo,
                Animales = animales,
                Etapa = lote.Etapa,
                PesoObjetivoKg = lote.PesoObjetivoKg
            };
        }

        // ───────────────────────── Dashboard principal ─────────────────────────

        public async Task<AgroPrincipalDto?> PrincipalAsync(int empresaId, int dias = 30)
        {
            var ctx = await CargarContextoAsync(empresaId, null);
            if (ctx == null) return null;
            var ejecutivo = await EjecutivoAsync(empresaId);
            if (ejecutivo == null) return null;
            var feed = await FeedAsync(empresaId, null, dias);

            var ahora = DateTime.UtcNow;
            var hoy = ctx.Hoy;
            var estId = ctx.Est.EstablecimientoId;
            var ids = ctx.Animales.Select(a => a.AnimalId).ToHashSet();

            // Atención completa: también define el desglose de alertas activas
            var atencion = ConstruirAtencion(ctx, soloFeed: false);
            if (feed != null)
                atencion.AddRange(feed.Atencion.Where(f => !atencion.Any(a => a.Titulo == f.Titulo)));
            var orden = new Dictionary<string, int> { ["Critica"] = 0, ["Alta"] = 1, ["Media"] = 2, ["Baja"] = 3 };
            atencion = atencion.OrderBy(a => orden.GetValueOrDefault(a.Prioridad, 4)).ToList();
            ejecutivo.Atencion = atencion.Take(12).ToList();

            var dto = new AgroPrincipalDto
            {
                Ejecutivo = ejecutivo,
                Feed = feed,
                SistemaProductivo = ctx.Cfg.SistemaProductivo,
                Paquete = ctx.Cfg.Paquete,
                AlertasSanitarias = atencion.Count(a => a.Modulo == "HEALTH"),
                AlertasDispositivos = atencion.Count(a => a.Modulo == "CONNECT"),
                AlertasOperativas = atencion.Count(a => a.Modulo != "HEALTH" && a.Modulo != "CONNECT"),
                AlertasTotal = atencion.Count,
                Inventario = ctx.Inventario
            };

            // Ubicación para el clima
            dto.Latitud = ctx.Est.Latitud;
            dto.Longitud = ctx.Est.Longitud;
            if (!dto.Latitud.HasValue)
            {
                var zona = await _db.AgroZonas.AsNoTracking().Where(z => z.EstablecimientoId == estId && z.Activo && z.LatCentro != null).FirstOrDefaultAsync();
                dto.Latitud = zona?.LatCentro;
                dto.Longitud = zona?.LngCentro;
            }

            // Movimiento del rodeo en 30 días (nacimientos y compras vs ventas y bajas)
            var todos = await _db.AgroAnimales.AsNoTracking().Where(a => a.EstablecimientoId == estId && a.Activo).ToListAsync();
            var desde30 = hoy.AddDays(-30);
            dto.IngresosMes = todos.Count(a => (a.FechaNacimiento.HasValue && a.FechaNacimiento >= desde30)
                || (a.FechaCreacion >= desde30 && a.FechaIngreso.HasValue && a.FechaIngreso >= desde30 && Math.Abs((a.FechaCreacion - a.FechaIngreso.Value).TotalDays) < 2));
            dto.SalidasMes = todos.Count(a => (a.Etapa == "Vendido" || a.Etapa == "Baja") && a.FechaEtapa >= desde30);
            var inicio = ctx.Animales.Count - dto.IngresosMes + dto.SalidasMes;
            dto.VariacionMesPct = inicio > 0 ? R((ctx.Animales.Count - inicio) * 100m / inicio, 1) : null;

            // Estado sanitario de todo el rodeo
            var tratamiento = ctx.Animales.Where(a => a.Estado == "Tratamiento").Select(a => a.AnimalId).ToHashSet();
            var retiro = ctx.Retiro.Keys.Where(ids.Contains).ToHashSet();
            var observacion = ctx.Alertas.Where(a => a.AnimalId.HasValue && ids.Contains(a.AnimalId.Value)).Select(a => a.AnimalId!.Value).ToHashSet();
            foreach (var a in ctx.Animales.Where(a => a.Etapa == "Engorde" && a.FechaIngreso.HasValue && (hoy - a.FechaIngreso.Value.Date).TotalDays >= 14))
                if (Gpd(a, hoy) < ctx.Cfg.GpdMinimoKg) observacion.Add(a.AnimalId);
            observacion.ExceptWith(tratamiento);

            HashSet<int> Alcanzados(IEnumerable<AgroSanidad> s) => s.SelectMany(x => x.AnimalId.HasValue
                    ? new[] { x.AnimalId.Value }
                    : ctx.Animales.Where(a => a.LoteId == x.LoteId).Select(a => a.AnimalId).ToArray())
                .Where(ids.Contains).ToHashSet();
            var programadas = ctx.Sanidad.Where(s => s.Estado == "Programada" && s.FechaProgramada.HasValue).ToList();
            var vencidas = Alcanzados(programadas.Where(s => s.FechaProgramada!.Value.Date < hoy));
            var proximas = Alcanzados(programadas.Where(s => s.Tipo == "Vacuna" && s.FechaProgramada!.Value.Date >= hoy && s.FechaProgramada!.Value.Date <= hoy.AddDays(7)));
            var conIncidencia = new HashSet<int>(tratamiento);
            conIncidencia.UnionWith(retiro);
            conIncidencia.UnionWith(observacion);
            conIncidencia.UnionWith(vencidas);
            var muertes = todos.Count(a => a.Estado == "Muerto" && a.FechaEtapa >= desde30);
            dto.Sanitario = new AgroEstadoSanitarioDto
            {
                Total = ctx.Animales.Count,
                SinIncidencias = ctx.Animales.Count - conIncidencia.Count,
                EnObservacion = observacion.Count,
                EnTratamiento = tratamiento.Count,
                EnRetiro = retiro.Count,
                VacunasProximas = proximas.Count,
                VacunasVencidas = vencidas.Count,
                Muertes30d = muertes,
                MortalidadPct30d = ctx.Animales.Count + muertes > 0 ? R(muertes * 100m / (ctx.Animales.Count + muertes), 2) : 0
            };

            // Trazabilidad
            var pesajes = await _db.AgroPesajes.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Activo && ids.Contains(p.AnimalId) && p.FechaPesaje >= ahora.AddDays(-30))
                .Select(p => new { p.AnimalId, p.FechaPesaje, p.PesoKg, p.Origen }).ToListAsync();
            var completos = ctx.Animales.Count(a => !string.IsNullOrWhiteSpace(a.RfidTag) && a.LoteId.HasValue
                                                    && !string.IsNullOrWhiteSpace(a.Raza) && (a.FechaNacimiento.HasValue || a.FechaIngreso.HasValue));
            dto.Trazabilidad = new AgroTrazaResumenDto
            {
                PctIdentificacionCompleta = ctx.Animales.Count > 0 ? R(completos * 100m / ctx.Animales.Count, 1) : 0,
                InfoIncompleta = ctx.Animales.Count - completos,
                SinRfid = ctx.Animales.Count(a => string.IsNullOrWhiteSpace(a.RfidTag)),
                SinLote = ctx.Animales.Count(a => !a.LoteId.HasValue),
                SinRazaOFecha = ctx.Animales.Count(a => string.IsNullOrWhiteSpace(a.Raza) || (!a.FechaNacimiento.HasValue && !a.FechaIngreso.HasValue)),
                UltimoPesaje = pesajes.Count > 0 ? pesajes.Max(p => p.FechaPesaje) : null,
                Pesajes7d = pesajes.Count(p => p.FechaPesaje >= ahora.AddDays(-7))
            };

            // Alimentación del confinamiento + stock
            if (feed != null)
            {
                var diario = feed.AlimentacionDiaria;
                var hoyKg = diario.Count > 0 ? diario[^1].Kg : 0;
                var ayerKg = diario.Count > 1 ? diario[^2].Kg : 0;
                var feedLoteIds = feed.Lotes.Select(l => l.LoteId).ToList();
                var tiposFeed = await _db.AgroRaciones.AsNoTracking()
                    .Where(r => feedLoteIds.Contains(r.LoteId) && r.Activo && r.FechaEntrega >= ahora.AddDays(-7))
                    .Select(r => r.TipoAlimento).Distinct().ToListAsync();
                var stockFeed = ctx.Inventario.Where(i => i.Tipo == "Alimento" && tiposFeed.Any(t => string.Equals(t, i.Nombre, StringComparison.OrdinalIgnoreCase))).ToList();
                var consumoStock = stockFeed.Sum(i => i.ConsumoDiario);
                dto.Alimento = new AgroAlimentoResumenDto
                {
                    ConsumoHoyKg = hoyKg,
                    ConsumoAyerKg = ayerKg,
                    VariacionPct = ayerKg > 0 ? R((hoyKg - ayerKg) * 100m / ayerKg, 1) : null,
                    ConsumoCabezaKg = feed.KgCabDia,
                    ConsumoObjetivoCabKg = feed.PesoPromedioKg.HasValue ? R(feed.PesoPromedioKg.Value * 0.028m, 1) : null,
                    StockKg = R(stockFeed.Sum(i => i.Stock), 0),
                    StockDias = consumoStock > 0 ? R(Math.Max(0, stockFeed.Sum(i => i.Stock)) / consumoStock, 1) : null,
                    Conversion = feed.ConversionEstimada,
                    ConversionObjetivo = ctx.Cfg.ConversionObjetivo,
                    ConsumoDiario = diario.Skip(Math.Max(0, diario.Count - 7)).ToList()
                };

                // Rentabilidad estimada por cabeza
                var costoCab = (feed.Economia.CostoCabDiaGs ?? 0) * (feed.DiasPromedioEnCorral ?? 0);
                var valor = (feed.PesoPromedioKg ?? 0) * ctx.Cfg.PrecioKgGs;
                var ingreso = (feed.PesoIngresoPromedioKg ?? 0) * ctx.Cfg.PrecioKgGs;
                var margen = valor - ingreso - costoCab;
                dto.Rentabilidad = new AgroRentabilidadDto
                {
                    CostoCabezaGs = R(costoCab, 0),
                    CostoKgGanadoGs = feed.Economia.CostoKgProducidoGs,
                    ValorCabezaGs = R(valor, 0),
                    CostoIngresoCabezaGs = R(ingreso, 0),
                    MargenCabezaGs = R(margen, 0),
                    MargenPct = ingreso + costoCab > 0 ? R(margen * 100m / (ingreso + costoCab), 1) : null
                };
            }

            // Dispositivos por tipo
            dto.DispositivosTotal = ctx.Dispositivos.Count;
            dto.DispositivosOnline = ctx.Dispositivos.Count(d => d.Estado == "Online");
            dto.DispositivosPorTipo = ctx.Dispositivos.GroupBy(d => d.Tipo)
                .Select(g => new AgroDispositivoTipoDto { Tipo = g.Key, Total = g.Count(), Online = g.Count(d => d.Estado == "Online") })
                .OrderByDescending(x => x.Total).ToList();

            // Última sincronización: el dato más reciente que llegó del campo
            var ultimaRacion = await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo).MaxAsync(r => (DateTime?)r.FechaEntrega);
            dto.UltimaSincronizacion = new[]
            {
                dto.Trazabilidad.UltimoPesaje,
                ultimaRacion,
                ctx.Dispositivos.Max(d => d.UltimoPingAt),
                ctx.Sanidad.Max(s => s.FechaAplicacion)
            }.Where(x => x.HasValue && x <= ahora.AddMinutes(5)).DefaultIfEmpty(null).Max();

            // Actividad reciente de todo el establecimiento
            var codigos = todos.ToDictionary(a => a.AnimalId, a => a.CodigoInterno);
            var lotes = ctx.Lotes.ToDictionary(l => l.LoteId, l => l.Codigo);
            var act = new List<AgroActividadDto>();
            act.AddRange(pesajes.Where(p => p.FechaPesaje >= ahora.AddDays(-7))
                .GroupBy(p => new { p.FechaPesaje.Date, p.Origen })
                .Select(g => g.Count() == 1
                    ? new AgroActividadDto { Fecha = g.First().FechaPesaje, Tipo = "Pesaje", Titulo = $"Pesaje de #{codigos.GetValueOrDefault(g.First().AnimalId)}: {g.First().PesoKg:0.#} kg", Detalle = g.Key.Origen }
                    : new AgroActividadDto { Fecha = g.Max(p => p.FechaPesaje), Tipo = "Pesaje", Titulo = $"{g.Count()} pesajes registrados ({g.Key.Origen})", Detalle = $"promedio {g.Average(p => p.PesoKg):0.#} kg" }));
            var racionesRec = await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo).OrderByDescending(r => r.FechaEntrega).Take(4).ToListAsync();
            act.AddRange(racionesRec.Select(r => new AgroActividadDto
            {
                Fecha = r.FechaEntrega, Tipo = "Racion",
                Titulo = $"Entrega de {r.CantidadKg:N0} kg de {r.TipoAlimento.ToLowerInvariant()}",
                Detalle = $"{lotes.GetValueOrDefault(r.LoteId)}{(string.IsNullOrWhiteSpace(r.Operario) ? "" : " · " + r.Operario)}"
            }));
            act.AddRange(ctx.Sanidad.Where(s => s.Estado == "Aplicada" && s.FechaAplicacion >= ahora.AddDays(-14)).Select(s => new AgroActividadDto
            {
                Fecha = s.FechaAplicacion!.Value, Tipo = "Sanidad",
                Titulo = $"{s.Tipo} aplicado a {(s.AnimalId.HasValue ? "#" + codigos.GetValueOrDefault(s.AnimalId.Value) : "lote " + lotes.GetValueOrDefault(s.LoteId ?? 0))}",
                Detalle = s.Producto
            }));
            act.AddRange(todos.Where(a => a.FechaEtapa >= ahora.AddDays(-14) && a.FechaEtapa > a.FechaCreacion.AddMinutes(1)).Select(a => new AgroActividadDto
            {
                Fecha = a.FechaEtapa!.Value, Tipo = "Etapa",
                Titulo = $"Animal #{a.CodigoInterno} pasó a {EtapaNombre(a.Etapa)}",
                Detalle = a.LoteId.HasValue ? $"lote {lotes.GetValueOrDefault(a.LoteId.Value)}" : null
            }));
            act.AddRange(ctx.Alertas.Where(a => a.FechaAlerta >= ahora.AddDays(-14)).Select(a => new AgroActividadDto { Fecha = a.FechaAlerta, Tipo = "Alerta", Titulo = a.Titulo, Detalle = a.Detalle }));
            var ingresos = await _db.AgroInsumoMovimientos.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && m.Activo && m.Tipo == "Ingreso" && m.Fecha >= ahora.AddDays(-14)).ToListAsync();
            var nombresInsumo = ctx.Inventario.ToDictionary(i => i.InsumoId, i => i);
            act.AddRange(ingresos.Where(m => nombresInsumo.ContainsKey(m.InsumoId)).Select(m => new AgroActividadDto
            {
                Fecha = m.Fecha, Tipo = "Inventario",
                Titulo = $"Ingreso de {m.Cantidad:N0} {nombresInsumo[m.InsumoId].Unidad} de {nombresInsumo[m.InsumoId].Nombre.ToLowerInvariant()}",
                Detalle = m.Referencia
            }));
            dto.Actividad = act.OrderByDescending(a => a.Fecha).Take(8).ToList();

            return dto;
        }
    }
}
