using Gesco.Application.DTOs;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    /// <summary>
    /// VORDIA AGRO — rediseño por módulos.
    /// Ciclo productivo: Reproductor → Cría → Recría → Engorde → Terminado → Vendido (o Baja).
    /// Control transversal: Sanidad, Alimentación, Pesajes, Trazabilidad, Costos.
    /// </summary>
    public partial class AgroService
    {
        public static readonly string[] ModulosDisponibles = { "CRIA", "RECRIA", "FEED", "HEALTH", "TRACE", "FINANCE", "CONNECT" };
        private static readonly string[] EtapasValidas = { "Reproductor", "Cria", "Recria", "Engorde", "Terminado", "Vendido", "Baja" };
        private static readonly string[] EtapasProductivas = { "Reproductor", "Cria", "Recria", "Engorde", "Terminado" };
        private const decimal PesoDesteteObjetivoKg = 180m;
        private const int EdadDesteteDias = 180;

        private sealed class Contexto
        {
            public AgroEstablecimiento Est = null!;
            public AgroConfiguracion Cfg = null!;
            public List<AgroLote> Lotes = new();
            public List<AgroAnimal> Animales = new();
            public List<AgroSanidad> Sanidad = new();
            public List<AgroAlerta> Alertas = new();
            public List<AgroDispositivo> Dispositivos = new();
            public Dictionary<int, DateTime> Retiro = new();
            public List<AgroInsumoDto> Inventario = new();
            public HashSet<string> Modulos = new();
            public DateTime Hoy;
        }

        // ───────────────────────── Configuración / licencia ─────────────────────────

        public async Task<AgroConfiguracionDto> ObtenerConfiguracionAsync(int empresaId)
        {
            var cfg = await _db.AgroConfiguraciones.AsNoTracking().FirstOrDefaultAsync(c => c.EmpresaId == empresaId);
            return MapConfig(cfg ?? ConfigPorDefecto(empresaId), cfg != null);
        }

        public async Task<AgroConfiguracionDto> GuardarConfiguracionAsync(AgroConfiguracionRequest request)
        {
            var sistema = NormalizarSistema(request.SistemaProductivo);
            var modulos = (request.ModulosActivos ?? new List<string>())
                .Select(m => m.Trim().ToUpperInvariant())
                .Where(m => ModulosDisponibles.Contains(m))
                .Distinct()
                .ToList();
            if (modulos.Count == 0) modulos = ModulosPorSistema(sistema);

            var cfg = await _db.AgroConfiguraciones.FirstOrDefaultAsync(c => c.EmpresaId == request.EmpresaId);
            if (cfg == null)
            {
                cfg = ConfigPorDefecto(request.EmpresaId);
                _db.AgroConfiguraciones.Add(cfg);
            }

            cfg.SistemaProductivo = sistema;
            cfg.ModulosActivos = string.Join(",", ModulosDisponibles.Where(modulos.Contains));
            cfg.Paquete = string.IsNullOrWhiteSpace(request.Paquete) ? PaqueteSugerido(sistema, modulos) : request.Paquete.Trim();
            if (request.PesoObjetivoKg is > 0) cfg.PesoObjetivoKg = request.PesoObjetivoKg.Value;
            if (request.PesoObjetivoRecriaKg is > 0) cfg.PesoObjetivoRecriaKg = request.PesoObjetivoRecriaKg.Value;
            if (request.GpdMinimoKg is > 0) cfg.GpdMinimoKg = request.GpdMinimoKg.Value;
            if (request.PrecioKgGs is > 0) cfg.PrecioKgGs = request.PrecioKgGs.Value;
            if (request.CostoRacionKgGs is > 0) cfg.CostoRacionKgGs = request.CostoRacionKgGs.Value;
            if (request.GpdObjetivoKg is > 0) cfg.GpdObjetivoKg = request.GpdObjetivoKg.Value;
            if (request.ConversionObjetivo is > 0) cfg.ConversionObjetivo = request.ConversionObjetivo.Value;
            cfg.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapConfig(cfg, true);
        }

        private static AgroConfiguracion ConfigPorDefecto(int empresaId) => new()
        {
            EmpresaId = empresaId,
            SistemaProductivo = "CicloCompleto",
            Paquete = "Enterprise",
            ModulosActivos = string.Join(",", ModulosDisponibles)
        };

        private static AgroConfiguracionDto MapConfig(AgroConfiguracion c, bool configurada) => new()
        {
            Configurada = configurada,
            SistemaProductivo = c.SistemaProductivo,
            Paquete = c.Paquete,
            ModulosActivos = ParseModulos(c.ModulosActivos).ToList(),
            PesoObjetivoKg = c.PesoObjetivoKg,
            PesoObjetivoRecriaKg = c.PesoObjetivoRecriaKg,
            GpdMinimoKg = c.GpdMinimoKg,
            PrecioKgGs = c.PrecioKgGs,
            CostoRacionKgGs = c.CostoRacionKgGs,
            GpdObjetivoKg = c.GpdObjetivoKg,
            ConversionObjetivo = c.ConversionObjetivo
        };

        private static HashSet<string> ParseModulos(string? csv) =>
            (csv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(m => m.ToUpperInvariant()).ToHashSet();

        private static string NormalizarSistema(string? sistema)
        {
            var s = SinAcentos(sistema ?? "").Replace(" ", "").ToLowerInvariant();
            return s switch
            {
                "cria" => "Cria",
                "recria" => "Recria",
                "engorde" => "Engorde",
                "confinamiento" or "feedlot" => "Confinamiento",
                _ => "CicloCompleto"
            };
        }

        /// <summary>Módulos que se activan por defecto según el sistema productivo elegido en el alta.</summary>
        private static List<string> ModulosPorSistema(string sistema) => sistema switch
        {
            "Cria" => new() { "CRIA", "HEALTH", "TRACE" },
            "Recria" => new() { "RECRIA", "HEALTH", "TRACE" },
            "Engorde" => new() { "RECRIA", "FEED", "HEALTH", "TRACE" },
            "Confinamiento" => new() { "FEED", "HEALTH", "TRACE", "CONNECT" },
            _ => ModulosDisponibles.ToList()
        };

        private static string PaqueteSugerido(string sistema, List<string> modulos)
        {
            if (sistema == "CicloCompleto" || modulos.Count >= 6) return "Enterprise";
            return modulos.Count <= 2 ? "Essential" : "Professional";
        }

        // ───────────────────────── Helpers de dominio ─────────────────────────

        private static string SinAcentos(string s)
        {
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string NormalizarEtapa(string etapa)
        {
            var s = SinAcentos(etapa.Trim()).ToLowerInvariant();
            var match = EtapasValidas.FirstOrDefault(e => e.ToLowerInvariant() == s);
            return match ?? throw new InvalidOperationException($"Etapa inválida: {etapa}");
        }

        private static string EtapaNombre(string etapa) => etapa switch
        {
            "Cria" => "cría",
            "Recria" => "recría",
            "Reproductor" => "rodeo reproductor",
            _ => etapa.ToLowerInvariant()
        };

        private static decimal? Gpd(AgroAnimal a, DateTime hoy)
        {
            if (!a.FechaIngreso.HasValue || !a.PesoIngresoKg.HasValue || !a.PesoActualKg.HasValue) return null;
            var dias = Math.Max(1, (int)(hoy - a.FechaIngreso.Value.Date).TotalDays);
            return (a.PesoActualKg.Value - a.PesoIngresoKg.Value) / dias;
        }

        private static decimal? Objetivo(AgroAnimal a, Dictionary<int, AgroLote> lotes, AgroConfiguracion cfg)
        {
            if (a.PesoObjetivoKg.HasValue) return a.PesoObjetivoKg;
            if (a.LoteId.HasValue && lotes.TryGetValue(a.LoteId.Value, out var l) && l.PesoObjetivoKg.HasValue && l.Etapa == a.Etapa)
                return l.PesoObjetivoKg;
            return a.Etapa switch
            {
                "Cria" => PesoDesteteObjetivoKg,
                "Recria" => cfg.PesoObjetivoRecriaKg,
                "Engorde" or "Terminado" => cfg.PesoObjetivoKg,
                _ => null
            };
        }

        private static bool ListoParaVenta(AgroAnimal a, decimal? objetivo) =>
            a.Etapa == "Terminado" || (a.Etapa == "Engorde" && objetivo.HasValue && (a.PesoActualKg ?? 0) >= objetivo.Value);

        private static decimal R(decimal v, int d = 1) => Math.Round(v, d);
        private static decimal? R(decimal? v, int d = 1) => v.HasValue ? Math.Round(v.Value, d) : null;
        private static string Fmt(decimal v) => v.ToString("N0", CultureInfo.GetCultureInfo("es-PY"));

        private static bool EsLoteFeed(AgroLote l) => l.Etapa == "Engorde" || (l.Etapa == null && l.Tipo == "Corral");

        private static string ModuloDeAlerta(string tipo) => tipo switch
        {
            "FueraZona" or "BateriaBaja" or "SinSenal" or "Camara" => "CONNECT",
            "Mortalidad" => "HEALTH",
            _ => "FEED"
        };

        private async Task<Contexto?> CargarContextoAsync(int empresaId, int? establecimientoId)
        {
            var est = await _db.AgroEstablecimientos.AsNoTracking()
                .Where(e => e.EmpresaId == empresaId && e.Activo)
                .Where(e => !establecimientoId.HasValue || e.EstablecimientoId == establecimientoId.Value)
                .OrderBy(e => e.EstablecimientoId)
                .FirstOrDefaultAsync();
            if (est == null) return null;

            var cfg = await _db.AgroConfiguraciones.AsNoTracking().FirstOrDefaultAsync(c => c.EmpresaId == empresaId)
                      ?? ConfigPorDefecto(empresaId);
            var ctx = new Contexto
            {
                Est = est,
                Cfg = cfg,
                Modulos = ParseModulos(cfg.ModulosActivos),
                Hoy = DateTime.UtcNow.Date
            };

            ctx.Lotes = await _db.AgroLotes.AsNoTracking()
                .Where(l => l.EmpresaId == empresaId && l.EstablecimientoId == est.EstablecimientoId && l.Activo)
                .OrderBy(l => l.Codigo).ToListAsync();
            ctx.Animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.EstablecimientoId == est.EstablecimientoId && a.Activo
                            && a.Etapa != "Vendido" && a.Etapa != "Baja")
                .ToListAsync();
            ctx.Sanidad = await _db.AgroSanidad.AsNoTracking()
                .Where(s => s.EmpresaId == empresaId && s.EstablecimientoId == est.EstablecimientoId && s.Activo && s.Estado != "Cancelada")
                .ToListAsync();
            ctx.Alertas = await _db.AgroAlertas.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.EstablecimientoId == est.EstablecimientoId && a.Activo && !a.Resuelta)
                .OrderByDescending(a => a.FechaAlerta).ToListAsync();
            ctx.Dispositivos = await _db.AgroDispositivos.AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && d.EstablecimientoId == est.EstablecimientoId && d.Activo)
                .ToListAsync();
            ctx.Retiro = CalcularRetiro(ctx.Sanidad, ctx.Animales, DateTime.UtcNow);
            ctx.Inventario = await CalcularInventarioAsync(empresaId, est.EstablecimientoId);
            return ctx;
        }

        /// <summary>Animal → fecha hasta la que está en período de retiro (no apto para faena).</summary>
        private static Dictionary<int, DateTime> CalcularRetiro(List<AgroSanidad> sanidad, List<AgroAnimal> animales, DateTime ahora)
        {
            var res = new Dictionary<int, DateTime>();
            foreach (var s in sanidad.Where(s => s.Estado == "Aplicada" && s.DiasRetiro > 0 && s.FechaAplicacion.HasValue))
            {
                var hasta = s.FechaAplicacion!.Value.Date.AddDays(s.DiasRetiro);
                if (hasta <= ahora.Date) continue;
                IEnumerable<int> ids = s.AnimalId.HasValue
                    ? new[] { s.AnimalId.Value }
                    : animales.Where(a => s.LoteId.HasValue && a.LoteId == s.LoteId).Select(a => a.AnimalId);
                foreach (var id in ids)
                    if (!res.TryGetValue(id, out var prev) || prev < hasta) res[id] = hasta;
            }
            return res;
        }

        private async Task EnriquecerAnimalesAsync(int empresaId, List<AgroAnimalDto> dtos)
        {
            if (dtos.Count == 0) return;
            var cfg = await _db.AgroConfiguraciones.AsNoTracking().FirstOrDefaultAsync(c => c.EmpresaId == empresaId)
                      ?? ConfigPorDefecto(empresaId);
            var lotes = await _db.AgroLotes.AsNoTracking().Where(l => l.EmpresaId == empresaId).ToDictionaryAsync(l => l.LoteId);
            var sanidad = await _db.AgroSanidad.AsNoTracking()
                .Where(s => s.EmpresaId == empresaId && s.Activo && s.Estado == "Aplicada" && s.DiasRetiro > 0)
                .ToListAsync();
            var animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.Activo)
                .Select(a => new AgroAnimal { AnimalId = a.AnimalId, LoteId = a.LoteId })
                .ToListAsync();
            var retiro = CalcularRetiro(sanidad, animales, DateTime.UtcNow);

            foreach (var d in dtos)
            {
                var a = new AgroAnimal
                {
                    AnimalId = d.AnimalId, LoteId = d.LoteId, Etapa = d.Etapa, PesoActualKg = d.PesoActualKg,
                    PesoObjetivoKg = d.PesoObjetivoKg
                };
                var obj = Objetivo(a, lotes, cfg);
                d.PesoObjetivoKg = obj;
                d.ListoParaVenta = ListoParaVenta(a, obj);
                if (obj.HasValue && d.PesoActualKg.HasValue && d.GpdKg is > 0 && d.PesoActualKg < obj)
                    d.DiasParaObjetivo = (int)Math.Ceiling((obj.Value - d.PesoActualKg.Value) / d.GpdKg.Value);
                if (retiro.TryGetValue(d.AnimalId, out var hasta)) d.RetiroSanitarioHasta = hasta;
            }
        }

        // ───────────────────────── "Qué requiere atención hoy" ─────────────────────────

        private static List<AgroAtencionDto> ConstruirAtencion(Contexto ctx, bool soloFeed, Dictionary<int, DateTime?>? ultimaRacionPorLote = null)
        {
            var items = new List<AgroAtencionDto>();
            var lotes = ctx.Lotes.ToDictionary(l => l.LoteId);
            var feedLoteIds = ctx.Lotes.Where(EsLoteFeed).Select(l => l.LoteId).ToHashSet();
            var animales = soloFeed
                ? ctx.Animales.Where(a => a.Etapa == "Engorde" || a.Etapa == "Terminado").ToList()
                : ctx.Animales;
            var animalIds = animales.Select(a => a.AnimalId).ToHashSet();
            var codigos = ctx.Animales.ToDictionary(a => a.AnimalId, a => a.CodigoInterno);

            // Alertas del campo (hardware, peso, etc.)
            foreach (var a in ctx.Alertas.Where(a => !soloFeed || !a.AnimalId.HasValue || animalIds.Contains(a.AnimalId.Value)).Take(6))
            {
                items.Add(new AgroAtencionDto
                {
                    Prioridad = a.Severidad == "Alta" ? "Alta" : a.Severidad == "Baja" ? "Baja" : "Media",
                    Modulo = ModuloDeAlerta(a.Tipo),
                    Titulo = a.Titulo,
                    Detalle = a.Detalle,
                    AlertaId = a.AlertaId,
                    AnimalId = a.AnimalId
                });
            }

            // Sanidad: vencidas, hoy, próximos 3 días
            foreach (var s in ctx.Sanidad.Where(s => s.Estado == "Programada" && s.FechaProgramada.HasValue))
            {
                if (soloFeed && !((s.LoteId.HasValue && feedLoteIds.Contains(s.LoteId.Value)) || (s.AnimalId.HasValue && animalIds.Contains(s.AnimalId.Value))))
                    continue;
                var dias = (int)(s.FechaProgramada!.Value.Date - ctx.Hoy).TotalDays;
                if (dias > 3) continue;
                var destino = s.LoteId.HasValue && lotes.TryGetValue(s.LoteId.Value, out var l) ? $"lote {l.Codigo}"
                    : s.AnimalId.HasValue && codigos.TryGetValue(s.AnimalId.Value, out var c) ? $"animal #{c}" : "";
                items.Add(new AgroAtencionDto
                {
                    Prioridad = dias < 0 ? "Critica" : dias == 0 ? "Media" : "Baja",
                    Modulo = "HEALTH",
                    Titulo = dias < 0 ? $"{s.Tipo} vencida: {s.Producto}" : dias == 0 ? $"{s.Tipo} para hoy: {s.Producto}" : $"{s.Tipo} en {dias} día(s): {s.Producto}",
                    Detalle = dias < 0 ? $"{destino} · atrasada {-dias} día(s)" : destino,
                    SanidadId = s.SanidadId,
                    LoteId = s.LoteId,
                    AnimalId = s.AnimalId
                });
            }

            // Corrales sin ración registrada en 24 h
            if (ultimaRacionPorLote != null)
            {
                foreach (var l in ctx.Lotes.Where(l => feedLoteIds.Contains(l.LoteId)))
                {
                    var cab = ctx.Animales.Count(a => a.LoteId == l.LoteId);
                    if (cab == 0) continue;
                    ultimaRacionPorLote.TryGetValue(l.LoteId, out var ult);
                    if (!ult.HasValue || ult.Value < DateTime.UtcNow.AddHours(-24))
                        items.Add(new AgroAtencionDto
                        {
                            Prioridad = "Alta",
                            Modulo = "FEED",
                            Titulo = $"Corral {l.Codigo} sin ración registrada en 24 h",
                            Detalle = $"{cab} cabezas · verificar entrega del mixer",
                            LoteId = l.LoteId
                        });
                }
            }

            // GPD bajo en engorde
            var bajos = animales
                .Where(a => a.Etapa == "Engorde" && a.FechaIngreso.HasValue && (ctx.Hoy - a.FechaIngreso.Value.Date).TotalDays >= 14)
                .Select(a => new { a, gpd = Gpd(a, ctx.Hoy) })
                .Where(x => x.gpd.HasValue && x.gpd < ctx.Cfg.GpdMinimoKg)
                .OrderBy(x => x.gpd)
                .ToList();
            if (bajos.Count > 0)
                items.Add(new AgroAtencionDto
                {
                    Prioridad = "Media",
                    Modulo = "FEED",
                    Titulo = $"{bajos.Count} animal(es) con GPD bajo (< {ctx.Cfg.GpdMinimoKg:0.##} kg/d)",
                    Detalle = "Revisar sanidad y consumo: " + string.Join(", ", bajos.Take(5).Select(x => $"#{x.a.CodigoInterno} ({x.gpd:0.00})")),
                    AnimalId = bajos.Count == 1 ? bajos[0].a.AnimalId : null
                });

            // Listos para venta (y los que no pueden ir a faena por retiro)
            var listos = animales.Where(a => ListoParaVenta(a, Objetivo(a, lotes, ctx.Cfg))).ToList();
            if (listos.Count > 0)
            {
                var enRetiro = listos.Where(a => ctx.Retiro.ContainsKey(a.AnimalId)).ToList();
                var kg = listos.Sum(a => a.PesoActualKg ?? 0);
                items.Add(new AgroAtencionDto
                {
                    Prioridad = "Media",
                    Modulo = "FEED",
                    Titulo = $"{listos.Count} animal(es) listos para venta / faena",
                    Detalle = $"{Fmt(kg)} kg en pie · Gs. {Fmt(kg * ctx.Cfg.PrecioKgGs)} estimado"
                });
                if (enRetiro.Count > 0)
                    items.Add(new AgroAtencionDto
                    {
                        Prioridad = "Alta",
                        Modulo = "HEALTH",
                        Titulo = $"{enRetiro.Count} listo(s) para venta en período de retiro sanitario",
                        Detalle = $"No enviar a faena antes del {enRetiro.Max(a => ctx.Retiro[a.AnimalId]):dd/MM}"
                    });
            }

            // Ocupación de corrales
            foreach (var l in ctx.Lotes.Where(l => l.Capacidad > 0 && (!soloFeed || feedLoteIds.Contains(l.LoteId))))
            {
                var cab = ctx.Animales.Count(a => a.LoteId == l.LoteId);
                if (cab >= l.Capacidad * 0.95m)
                    items.Add(new AgroAtencionDto
                    {
                        Prioridad = "Media",
                        Modulo = "FEED",
                        Titulo = $"{l.Codigo} al {Math.Round(cab * 100m / l.Capacidad)}% de capacidad",
                        Detalle = $"{cab}/{l.Capacidad} cabezas",
                        LoteId = l.LoteId
                    });
            }

            if (!soloFeed)
            {
                // Recría que alcanzó el peso objetivo → pasar a engorde
                var recriaOk = ctx.Animales.Where(a => a.Etapa == "Recria" && (a.PesoActualKg ?? 0) >= (Objetivo(a, lotes, ctx.Cfg) ?? decimal.MaxValue)).ToList();
                if (recriaOk.Count > 0)
                    items.Add(new AgroAtencionDto
                    {
                        Prioridad = "Media",
                        Modulo = "RECRIA",
                        Titulo = $"{recriaOk.Count} animal(es) de recría alcanzaron el peso objetivo",
                        Detalle = "Listos para pasar a engorde / terminación"
                    });

                // Terneros en edad o peso de destete
                var destete = ctx.Animales.Where(a => a.Etapa == "Cria" &&
                    ((a.FechaNacimiento.HasValue && (ctx.Hoy - a.FechaNacimiento.Value.Date).TotalDays >= EdadDesteteDias)
                     || (a.PesoActualKg ?? 0) >= PesoDesteteObjetivoKg)).ToList();
                if (destete.Count > 0)
                    items.Add(new AgroAtencionDto
                    {
                        Prioridad = "Media",
                        Modulo = "CRIA",
                        Titulo = $"{destete.Count} ternero(s) en edad o peso de destete",
                        Detalle = "Programar destete y pasar a recría"
                    });
            }

            // Lotes de engorde por debajo de la GPD objetivo
            foreach (var l in ctx.Lotes.Where(l => feedLoteIds.Contains(l.LoteId)))
            {
                var gpdsLote = ctx.Animales.Where(a => a.LoteId == l.LoteId && a.Etapa == "Engorde" && a.FechaIngreso.HasValue
                                                        && (ctx.Hoy - a.FechaIngreso.Value.Date).TotalDays >= 14)
                    .Select(a => Gpd(a, ctx.Hoy)).Where(g => g.HasValue).Select(g => g!.Value).ToList();
                if (gpdsLote.Count < 3 || ctx.Cfg.GpdObjetivoKg <= 0) continue;
                var desvio = (ctx.Cfg.GpdObjetivoKg - gpdsLote.Average()) / ctx.Cfg.GpdObjetivoKg;
                if (desvio >= 0.10m)
                    items.Add(new AgroAtencionDto
                    {
                        Prioridad = desvio >= 0.15m ? "Alta" : "Media",
                        Modulo = "FEED",
                        Titulo = $"Lote {l.Codigo} está {Math.Round(desvio * 100)} % por debajo de la GPD objetivo",
                        Detalle = $"GPD {gpdsLote.Average():0.00} vs objetivo {ctx.Cfg.GpdObjetivoKg:0.00} kg/d · revisar dieta y sanidad",
                        LoteId = l.LoteId
                    });
            }

            // Stock de insumos por terminarse
            foreach (var i in ctx.Inventario.Where(i => i.Tipo == "Alimento" && i.DiasRestantes < 5))
                items.Add(new AgroAtencionDto
                {
                    Prioridad = i.DiasRestantes < 3 ? "Alta" : "Media",
                    Modulo = "FEED",
                    Titulo = $"Stock de {i.Nombre}: {i.DiasRestantes:0.0} días restantes",
                    Detalle = $"{i.Stock:N0} {i.Unidad} · consumo {i.ConsumoDiario:N0} {i.Unidad}/día · programar compra"
                });
            foreach (var i in ctx.Inventario.Where(i => i.Tipo != "Alimento" && i.BajoMinimo))
                items.Add(new AgroAtencionDto
                {
                    Prioridad = "Media",
                    Modulo = "HEALTH",
                    Titulo = $"{i.Nombre}: stock bajo el mínimo",
                    Detalle = $"Quedan {i.Stock:N0} {i.Unidad} (mínimo {i.StockMinimo:N0})"
                });

            // Hardware: sin conexión o batería baja
            foreach (var d in ctx.Dispositivos.Where(d => d.Estado != "Online" && d.Tipo != "Collar").Take(3))
            {
                var min = d.UltimoPingAt.HasValue ? (int)(DateTime.UtcNow - d.UltimoPingAt.Value).TotalMinutes : (int?)null;
                items.Add(new AgroAtencionDto
                {
                    Prioridad = "Media",
                    Modulo = "CONNECT",
                    Titulo = $"{d.Tipo} {d.Codigo} sin conexión{(min.HasValue ? $" hace {(min < 120 ? $"{min} min" : $"{min / 60} h")}" : "")}",
                    Detalle = "Revisar energía y gateway"
                });
            }
            var dispMal = ctx.Dispositivos.Where(d => d.Tipo == "Collar" && (d.Estado != "Online" || d.BateriaPct is < 25)).ToList();
            if (dispMal.Count > 0 && !items.Any(i => i.Modulo == "CONNECT" && i.Titulo.Contains("batería", StringComparison.OrdinalIgnoreCase)))
                items.Add(new AgroAtencionDto
                {
                    Prioridad = "Baja",
                    Modulo = "CONNECT",
                    Titulo = $"{dispMal.Count} collar(es) con batería baja o sin señal",
                    Detalle = string.Join(", ", dispMal.Take(4).Select(d => d.Codigo))
                });

            var orden = new Dictionary<string, int> { ["Critica"] = 0, ["Alta"] = 1, ["Media"] = 2, ["Baja"] = 3 };
            return items
                .Where(i => ctx.Modulos.Contains(i.Modulo) || i.Modulo == "FEED" && soloFeed)
                .OrderBy(i => orden.GetValueOrDefault(i.Prioridad, 4))
                .ToList();
        }

        // ───────────────────────── Dashboard ejecutivo ─────────────────────────

        public async Task<AgroEjecutivoDto?> EjecutivoAsync(int empresaId, int? establecimientoId = null)
        {
            var ctx = await CargarContextoAsync(empresaId, establecimientoId);
            if (ctx == null) return null;
            var lotes = ctx.Lotes.ToDictionary(l => l.LoteId);
            var precio = ctx.Cfg.PrecioKgGs;

            var etapas = EtapasProductivas.Select(e =>
            {
                var grupo = ctx.Animales.Where(a => a.Etapa == e).ToList();
                var gpds = grupo.Select(a => Gpd(a, ctx.Hoy)).Where(g => g.HasValue).Select(g => g!.Value).ToList();
                var pesos = grupo.Where(a => a.PesoActualKg.HasValue).Select(a => a.PesoActualKg!.Value).ToList();
                return new AgroEtapaResumenDto
                {
                    Etapa = e,
                    Animales = grupo.Count,
                    PesoPromedioKg = pesos.Count > 0 ? R(pesos.Average()) : null,
                    GpdPromedioKg = e != "Reproductor" && gpds.Count > 0 ? R(gpds.Average(), 2) : null,
                    ValorEstimadoGs = R(pesos.Sum() * precio, 0)
                };
            }).ToList();

            var kg = ctx.Animales.Sum(a => a.PesoActualKg ?? 0);
            return new AgroEjecutivoDto
            {
                EstablecimientoId = ctx.Est.EstablecimientoId,
                Nombre = ctx.Est.Nombre,
                ClienteNombre = ctx.Est.ClienteNombre,
                Ubicacion = ctx.Est.Ubicacion,
                Hectareas = ctx.Est.Hectareas,
                TotalAnimales = ctx.Animales.Count,
                ListosParaVenta = ctx.Animales.Count(a => ListoParaVenta(a, Objetivo(a, lotes, ctx.Cfg))),
                AlertasAbiertas = ctx.Alertas.Count,
                SanidadPendiente = ctx.Sanidad.Count(s => s.Estado == "Programada"),
                SanidadVencida = ctx.Sanidad.Count(s => s.Estado == "Programada" && s.FechaProgramada.HasValue && s.FechaProgramada.Value.Date < ctx.Hoy),
                AnimalesEnRetiro = ctx.Retiro.Count(r => ctx.Animales.Any(a => a.AnimalId == r.Key)),
                KgEnPie = R(kg, 0),
                ValorRodeoGs = R(kg * precio, 0),
                PrecioKgGs = precio,
                Lotes = ctx.Lotes.Count,
                Etapas = etapas,
                Atencion = ConstruirAtencion(ctx, soloFeed: false).Take(10).ToList()
            };
        }

        // ───────────────────────── Dashboard VORDIA Feed ─────────────────────────

        public async Task<AgroFeedDto?> FeedAsync(int empresaId, int? establecimientoId = null, int diasPeriodo = 70)
        {
            var ctx = await CargarContextoAsync(empresaId, establecimientoId);
            if (ctx == null) return null;

            var hoy = ctx.Hoy;
            var ahora = DateTime.UtcNow;
            var desde30 = hoy.AddDays(-30);
            var desde7 = ahora.AddDays(-7);
            var lotesDict = ctx.Lotes.ToDictionary(l => l.LoteId);
            var feedLotes = ctx.Lotes.Where(EsLoteFeed).ToList();
            var feedLoteIds = feedLotes.Select(l => l.LoteId).ToList();
            var animales = ctx.Animales.Where(a => a.Etapa == "Engorde" || a.Etapa == "Terminado").ToList();
            var ids = animales.Select(a => a.AnimalId).ToList();

            var pesajes = await _db.AgroPesajes.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Activo && ids.Contains(p.AnimalId))
                .Select(p => new { p.AnimalId, p.PesoKg, p.FechaPesaje, p.Origen })
                .ToListAsync();
            var pesajesPorAnimal = pesajes.GroupBy(p => p.AnimalId)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.FechaPesaje).Select(p => (p.FechaPesaje, p.PesoKg)).ToList());

            var raciones = await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo && feedLoteIds.Contains(r.LoteId) && r.FechaEntrega >= desde30)
                .ToListAsync();
            var ultimaRacion = await _db.AgroRaciones.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Activo && feedLoteIds.Contains(r.LoteId))
                .GroupBy(r => r.LoteId)
                .Select(g => new { LoteId = g.Key, Ultima = g.Max(r => r.FechaEntrega) })
                .ToDictionaryAsync(x => x.LoteId, x => (DateTime?)x.Ultima);

            decimal Costo(AgroRacion r) => r.CostoGs ?? r.CantidadKg * ctx.Cfg.CostoRacionKgGs;

            decimal? PesoEn(AgroAnimal a, DateTime fecha)
            {
                if (a.FechaIngreso.HasValue && a.FechaIngreso.Value.Date > fecha.Date) return null;
                if (pesajesPorAnimal.TryGetValue(a.AnimalId, out var lista))
                {
                    var p = lista.LastOrDefault(x => x.FechaPesaje <= fecha);
                    if (p != default) return p.PesoKg;
                }
                return a.PesoIngresoKg;
            }

            decimal Ganancia30(AgroAnimal a)
            {
                if (!a.PesoActualKg.HasValue) return 0;
                var baseKg = a.FechaIngreso.HasValue && a.FechaIngreso.Value.Date > desde30 ? a.PesoIngresoKg : PesoEn(a, desde30);
                return baseKg.HasValue ? Math.Max(0, a.PesoActualKg.Value - baseKg.Value) : 0;
            }

            int? Dias(AgroAnimal a) => a.FechaIngreso.HasValue ? Math.Max(1, (int)(hoy - a.FechaIngreso.Value.Date).TotalDays) : null;

            // KPIs
            var pesos = animales.Where(a => a.PesoActualKg.HasValue).Select(a => a.PesoActualKg!.Value).ToList();
            var pesosIng = animales.Where(a => a.PesoIngresoKg.HasValue).Select(a => a.PesoIngresoKg!.Value).ToList();
            var gpds = animales.Select(a => Gpd(a, hoy)).Where(g => g.HasValue).Select(g => g!.Value).ToList();
            var dias = animales.Select(Dias).Where(d => d.HasValue).Select(d => d!.Value).ToList();
            var alimento30 = raciones.Sum(r => r.CantidadKg);
            var alimento7 = raciones.Where(r => r.FechaEntrega >= desde7).Sum(r => r.CantidadKg);
            var ganancia30 = animales.Sum(Ganancia30);
            var listos = animales.Where(a => ListoParaVenta(a, Objetivo(a, lotesDict, ctx.Cfg))).ToList();

            var dto = new AgroFeedDto
            {
                EstablecimientoId = ctx.Est.EstablecimientoId,
                Nombre = ctx.Est.Nombre,
                ClienteNombre = ctx.Est.ClienteNombre,
                Ubicacion = ctx.Est.Ubicacion,
                CabezasActivas = animales.Count,
                CapacidadTotal = feedLotes.Sum(l => l.Capacidad),
                PesoPromedioKg = pesos.Count > 0 ? R(pesos.Average()) : null,
                PesoIngresoPromedioKg = pesosIng.Count > 0 ? R(pesosIng.Average()) : null,
                GpdPromedioKg = gpds.Count > 0 ? R(gpds.Average(), 2) : null,
                ConversionEstimada = ganancia30 > 0 ? R(alimento30 / ganancia30, 2) : null,
                ListosParaVenta = listos.Count,
                AlertasAbiertas = ctx.Alertas.Count(a => !a.AnimalId.HasValue || ids.Contains(a.AnimalId.Value)),
                DiasPromedioEnCorral = dias.Count > 0 ? (int)Math.Round(dias.Average()) : null,
                PesoObjetivoKg = ctx.Cfg.PesoObjetivoKg,
                GpdMinimoKg = ctx.Cfg.GpdMinimoKg,
                GpdObjetivoKg = ctx.Cfg.GpdObjetivoKg,
                ConversionObjetivo = ctx.Cfg.ConversionObjetivo,
                AlimentoKg7d = R(alimento7, 0),
                KgCabDia = animales.Count > 0 ? R(alimento7 / 7m / animales.Count) : null,
                Atencion = ConstruirAtencion(ctx, soloFeed: true, ultimaRacion)
            };

            // Evolución de peso: 11 puntos en el período pedido, con la trayectoria objetivo
            diasPeriodo = Math.Clamp(diasPeriodo, 14, 365);
            var paso = Math.Max(1, (int)Math.Round(diasPeriodo / 10.0));
            decimal? baseObjetivo = null;
            DateTime? fechaBase = null;
            for (var k = 10; k >= 0; k--)
            {
                var fecha = hoy.AddDays(-paso * k);
                var corte = fecha.AddDays(1).AddTicks(-1);
                var vals = animales.Select(a => PesoEn(a, corte)).Where(p => p.HasValue).Select(p => p!.Value).ToList();
                decimal? prom = vals.Count > 0 ? R(vals.Average()) : null;
                if (baseObjetivo == null && prom.HasValue) { baseObjetivo = prom; fechaBase = fecha; }
                dto.EvolucionPeso.Add(new AgroPuntoPesoDto
                {
                    Fecha = fecha,
                    PesoPromedioKg = prom,
                    PesoObjetivoKg = baseObjetivo.HasValue ? R(baseObjetivo.Value + ctx.Cfg.GpdObjetivoKg * (decimal)(fecha - fechaBase!.Value).TotalDays) : null,
                    Animales = vals.Count
                });
            }

            // Proyección de terminación: días al peso objetivo con la GPD de cada animal
            var diasObjetivo = animales.Where(a => !listos.Contains(a))
                .Select(a =>
                {
                    var obj = Objetivo(a, lotesDict, ctx.Cfg);
                    var g = Gpd(a, hoy);
                    return obj.HasValue && g is > 0 && a.PesoActualKg.HasValue ? (int?)Math.Ceiling((obj.Value - a.PesoActualKg.Value) / g.Value) : null;
                })
                .Where(d => d.HasValue).Select(d => d!.Value).ToList();
            dto.Listos15d = diasObjetivo.Count(d => d <= 15);
            dto.Listos30d = diasObjetivo.Count(d => d <= 30);
            dto.DiasEstimadosTerminacion = diasObjetivo.Count > 0 ? (int)Math.Round(diasObjetivo.Average()) : null;

            // Rendimiento por lote / corral
            foreach (var l in feedLotes)
            {
                var la = animales.Where(a => a.LoteId == l.LoteId).ToList();
                var lr = raciones.Where(r => r.LoteId == l.LoteId).ToList();
                var lGpd = la.Select(a => Gpd(a, hoy)).Where(g => g.HasValue).Select(g => g!.Value).ToList();
                var lDias = la.Select(Dias).Where(d => d.HasValue).Select(d => d!.Value).ToList();
                var lAlim7 = lr.Where(r => r.FechaEntrega >= desde7).Sum(r => r.CantidadKg);
                var lGan30 = la.Sum(Ganancia30);
                var lCosto30 = lr.Sum(Costo);
                var objetivo = l.PesoObjetivoKg ?? ctx.Cfg.PesoObjetivoKg;
                var gpdProm = lGpd.Count > 0 ? lGpd.Average() : (decimal?)null;
                var conv = lGan30 > 0 ? lr.Sum(r => r.CantidadKg) / lGan30 : (decimal?)null;
                ultimaRacion.TryGetValue(l.LoteId, out var ult);

                var rend = "Optimo";
                if (la.Count > 0 && gpdProm.HasValue && gpdProm < ctx.Cfg.GpdMinimoKg) rend = "Bajo";
                else if (la.Count > 0 && ((conv.HasValue && conv > 9.5m) || (l.Capacidad > 0 && la.Count >= l.Capacidad * 0.95m)
                                          || !ult.HasValue || ult < ahora.AddHours(-24))) rend = "Atencion";

                dto.Lotes.Add(new AgroFeedLoteDto
                {
                    LoteId = l.LoteId,
                    Codigo = l.Codigo,
                    Nombre = l.Nombre,
                    Tipo = l.Tipo,
                    Capacidad = l.Capacidad,
                    Animales = la.Count,
                    PesoPromedioKg = la.Any(a => a.PesoActualKg.HasValue) ? R(la.Where(a => a.PesoActualKg.HasValue).Average(a => a.PesoActualKg!.Value)) : null,
                    GpdPromedioKg = R(gpdProm, 2),
                    PesoObjetivoKg = objetivo,
                    DiasPromedio = lDias.Count > 0 ? (int)Math.Round(lDias.Average()) : null,
                    AlimentoKg7d = R(lAlim7, 0),
                    KgCabDia = la.Count > 0 ? R(lAlim7 / 7m / la.Count) : null,
                    Conversion = R(conv, 2),
                    KgProducidos30d = R(lGan30, 0),
                    CostoAlimento30dGs = R(lCosto30, 0),
                    CostoKgProducidoGs = lGan30 > 0 ? R(lCosto30 / lGan30, 0) : null,
                    ListosParaVenta = listos.Count(a => a.LoteId == l.LoteId),
                    UltimaRacion = ult,
                    Rendimiento = rend
                });
            }

            // Alimentación diaria (14 días)
            for (var k = 13; k >= 0; k--)
            {
                var dia = hoy.AddDays(-k);
                var del = raciones.Where(r => r.FechaEntrega.Date == dia).ToList();
                dto.AlimentacionDiaria.Add(new AgroPuntoAlimentoDto { Fecha = dia, Kg = R(del.Sum(r => r.CantidadKg), 0), CostoGs = R(del.Sum(Costo), 0) });
            }

            // Sanidad del confinamiento
            var feedIdSet = ids.ToHashSet();
            var feedLoteSet = feedLoteIds.ToHashSet();
            var sanFeed = ctx.Sanidad.Where(s => (s.LoteId.HasValue && feedLoteSet.Contains(s.LoteId.Value)) || (s.AnimalId.HasValue && feedIdSet.Contains(s.AnimalId.Value))).ToList();
            dto.Sanidad = new AgroSanidadResumenDto
            {
                Programadas = sanFeed.Count(s => s.Estado == "Programada" && (!s.FechaProgramada.HasValue || s.FechaProgramada.Value.Date >= hoy)),
                Vencidas = sanFeed.Count(s => s.Estado == "Programada" && s.FechaProgramada.HasValue && s.FechaProgramada.Value.Date < hoy),
                AplicadasMes = sanFeed.Count(s => s.Estado == "Aplicada" && s.FechaAplicacion >= desde30),
                AnimalesEnRetiro = ctx.Retiro.Keys.Count(feedIdSet.Contains),
                AnimalesEnTratamiento = animales.Count(a => a.Estado == "Tratamiento"),
                Proximas = sanFeed.Where(s => s.Estado == "Programada")
                    .OrderBy(s => s.FechaProgramada).Take(5)
                    .Select(s => MapSanidad(s, lotesDict, ctx.Animales, hoy)).ToList()
            };

            // Proyección económica (ventana 30 días)
            var costo30 = raciones.Sum(Costo);
            var costo7 = raciones.Where(r => r.FechaEntrega >= desde7).Sum(Costo);
            var costoKg = ganancia30 > 0 ? costo30 / ganancia30 : (decimal?)null;
            var kgListos = listos.Sum(a => a.PesoActualKg ?? 0);
            dto.Economia = new AgroEconomiaDto
            {
                KgProducidos30d = R(ganancia30, 0),
                CostoAlimento30dGs = R(costo30, 0),
                CostoKgProducidoGs = R(costoKg, 0),
                CostoDiarioGs = R(costo7 / 7m, 0),
                CostoCabDiaGs = animales.Count > 0 ? R(costo7 / 7m / animales.Count, 0) : null,
                PrecioKgGs = ctx.Cfg.PrecioKgGs,
                MargenKgGs = costoKg.HasValue ? R(ctx.Cfg.PrecioKgGs - costoKg.Value, 0) : null,
                ValorStockGs = R(pesos.Sum() * ctx.Cfg.PrecioKgGs, 0),
                ValorListosGs = R(kgListos * ctx.Cfg.PrecioKgGs, 0),
                KgListos = R(kgListos, 0)
            };

            dto.Dispositivos = new AgroDispositivosResumenDto
            {
                Total = ctx.Dispositivos.Count,
                Online = ctx.Dispositivos.Count(d => d.Estado == "Online"),
                Alerta = ctx.Dispositivos.Count(d => d.Estado != "Online"),
                BateriaBaja = ctx.Dispositivos.Count(d => d.BateriaPct is < 25)
            };

            // Actividad reciente
            var act = new List<AgroActividadDto>();
            act.AddRange(pesajes.Where(p => p.FechaPesaje >= ahora.AddDays(-7))
                .GroupBy(p => new { p.FechaPesaje.Date, p.Origen })
                .Select(g => new AgroActividadDto
                {
                    Fecha = g.Max(p => p.FechaPesaje),
                    Tipo = "Pesaje",
                    Titulo = $"Pesaje de {g.Count()} animal(es)",
                    Detalle = $"{g.Key.Origen} · promedio {R(g.Average(p => p.PesoKg)):0.#} kg"
                }));
            act.AddRange(raciones.OrderByDescending(r => r.FechaEntrega).Take(6).Select(r => new AgroActividadDto
            {
                Fecha = r.FechaEntrega,
                Tipo = "Racion",
                Titulo = $"Ración {r.TipoAlimento} · {(lotesDict.TryGetValue(r.LoteId, out var l) ? l.Codigo : "")}",
                Detalle = $"{Fmt(r.CantidadKg)} kg{(string.IsNullOrWhiteSpace(r.Operario) ? "" : " · " + r.Operario)}"
            }));
            act.AddRange(sanFeed.Where(s => s.Estado == "Aplicada" && s.FechaAplicacion.HasValue).Select(s => new AgroActividadDto
            {
                Fecha = s.FechaAplicacion!.Value,
                Tipo = "Sanidad",
                Titulo = $"{s.Tipo} aplicada: {s.Producto}",
                Detalle = s.Responsable
            }));
            act.AddRange(ctx.Alertas.Take(5).Select(a => new AgroActividadDto { Fecha = a.FechaAlerta, Tipo = "Alerta", Titulo = a.Titulo, Detalle = a.Detalle }));
            dto.Actividad = act.OrderByDescending(a => a.Fecha).Take(10).ToList();

            return dto;
        }

        // ───────────────────────── Sanidad ─────────────────────────

        private static AgroSanidadDto MapSanidad(AgroSanidad s, Dictionary<int, AgroLote> lotes, List<AgroAnimal> animales, DateTime hoy) => new()
        {
            SanidadId = s.SanidadId,
            AnimalId = s.AnimalId,
            AnimalCodigo = s.AnimalId.HasValue ? animales.FirstOrDefault(a => a.AnimalId == s.AnimalId)?.CodigoInterno : null,
            LoteId = s.LoteId,
            LoteCodigo = s.LoteId.HasValue && lotes.TryGetValue(s.LoteId.Value, out var l) ? l.Codigo : null,
            Tipo = s.Tipo,
            Producto = s.Producto,
            Dosis = s.Dosis,
            FechaProgramada = s.FechaProgramada,
            FechaAplicacion = s.FechaAplicacion,
            DiasRetiro = s.DiasRetiro,
            RetiroHasta = s.FechaAplicacion.HasValue && s.DiasRetiro > 0 ? s.FechaAplicacion.Value.Date.AddDays(s.DiasRetiro) : null,
            Estado = s.Estado,
            Vencida = s.Estado == "Programada" && s.FechaProgramada.HasValue && s.FechaProgramada.Value.Date < hoy,
            Responsable = s.Responsable,
            Notas = s.Notas,
            AnimalesAlcanzados = s.AnimalId.HasValue ? 1 : animales.Count(a => a.LoteId == s.LoteId)
        };

        public async Task<List<AgroSanidadDto>> ListarSanidadAsync(int empresaId, int? establecimientoId = null, string? estado = null)
        {
            var ctx = await CargarContextoAsync(empresaId, establecimientoId);
            if (ctx == null) return new();
            var lotes = await _db.AgroLotes.AsNoTracking().Where(l => l.EmpresaId == empresaId).ToDictionaryAsync(l => l.LoteId);
            var list = ctx.Sanidad.Select(s => MapSanidad(s, lotes, ctx.Animales, ctx.Hoy)).ToList();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                list = estado switch
                {
                    "Vencida" => list.Where(s => s.Vencida).ToList(),
                    "Programada" => list.Where(s => s.Estado == "Programada").ToList(),
                    _ => list.Where(s => s.Estado == estado).ToList()
                };
            }

            return list
                .OrderBy(s => s.Estado == "Programada" ? 0 : 1)
                .ThenBy(s => s.Estado == "Programada" ? s.FechaProgramada : null)
                .ThenByDescending(s => s.FechaAplicacion)
                .ToList();
        }

        public async Task<AgroSanidadDto> RegistrarSanidadAsync(AgroSanidadCreateRequest request)
        {
            if (!request.AnimalId.HasValue && !request.LoteId.HasValue)
                throw new InvalidOperationException("Indicá un animal o un lote");
            if (string.IsNullOrWhiteSpace(request.Producto))
                throw new InvalidOperationException("Producto obligatorio");

            AgroAnimal? animal = null;
            int estId;
            if (request.AnimalId.HasValue)
            {
                animal = await _db.AgroAnimales.FirstOrDefaultAsync(a => a.AnimalId == request.AnimalId && a.EmpresaId == request.EmpresaId && a.Activo)
                         ?? throw new InvalidOperationException("Animal no encontrado");
                estId = animal.EstablecimientoId;
            }
            else
            {
                var lote = await _db.AgroLotes.AsNoTracking().FirstOrDefaultAsync(l => l.LoteId == request.LoteId && l.EmpresaId == request.EmpresaId && l.Activo)
                           ?? throw new InvalidOperationException("Lote no encontrado");
                estId = lote.EstablecimientoId;
            }

            var s = new AgroSanidad
            {
                EmpresaId = request.EmpresaId,
                EstablecimientoId = estId,
                AnimalId = request.AnimalId,
                LoteId = request.AnimalId.HasValue ? null : request.LoteId,
                Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "Vacuna" : request.Tipo.Trim(),
                Producto = request.Producto.Trim(),
                Dosis = request.Dosis,
                FechaProgramada = (request.FechaProgramada ?? DateTime.UtcNow).Date,
                FechaAplicacion = request.Aplicada ? DateTime.UtcNow : null,
                DiasRetiro = Math.Max(0, request.DiasRetiro),
                Estado = request.Aplicada ? "Aplicada" : "Programada",
                Responsable = request.Responsable,
                Notas = request.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _db.AgroSanidad.Add(s);

            if (animal != null && request.Aplicada && s.Tipo == "Tratamiento" && animal.Estado == "Activo")
            {
                animal.Estado = "Tratamiento";
                animal.FechaActualizacion = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            var lotes = await _db.AgroLotes.AsNoTracking().Where(l => l.EmpresaId == request.EmpresaId).ToDictionaryAsync(l => l.LoteId);
            var animales = await _db.AgroAnimales.AsNoTracking()
                .Where(a => a.EmpresaId == request.EmpresaId && a.EstablecimientoId == estId && a.Activo && a.Etapa != "Vendido" && a.Etapa != "Baja")
                .ToListAsync();
            return MapSanidad(s, lotes, animales, DateTime.UtcNow.Date);
        }

        public async Task<bool> AplicarSanidadAsync(int sanidadId, int empresaId)
        {
            var s = await _db.AgroSanidad.FirstOrDefaultAsync(x => x.SanidadId == sanidadId && x.EmpresaId == empresaId && x.Activo);
            if (s == null) return false;
            s.Estado = "Aplicada";
            s.FechaAplicacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────────────────── Ciclo productivo ─────────────────────────

        public async Task<AgroAnimalDto> CambiarEtapaAsync(int animalId, AgroCambioEtapaRequest request)
        {
            var animal = await _db.AgroAnimales.FirstOrDefaultAsync(a => a.AnimalId == animalId && a.EmpresaId == request.EmpresaId && a.Activo)
                         ?? throw new InvalidOperationException("Animal no encontrado");
            var nueva = NormalizarEtapa(request.Etapa);
            var anterior = animal.Etapa;
            var ahora = DateTime.UtcNow;

            if (request.PesoKg is > 0)
            {
                _db.AgroPesajes.Add(new AgroPesaje
                {
                    EmpresaId = request.EmpresaId,
                    AnimalId = animal.AnimalId,
                    PesoKg = request.PesoKg.Value,
                    FechaPesaje = ahora,
                    Origen = "Cambio de etapa",
                    Activo = true
                });
                animal.PesoActualKg = request.PesoKg.Value;
            }

            // Recría y engorde miden GPD desde el ingreso a la etapa.
            if (nueva != anterior && (nueva == "Recria" || nueva == "Engorde"))
            {
                animal.FechaIngreso = ahora.Date;
                animal.PesoIngresoKg = animal.PesoActualKg;
            }

            if (nueva == "Vendido") animal.Estado = "Vendido";
            else if (nueva == "Baja" && (request.Motivo ?? "").Contains("muert", StringComparison.OrdinalIgnoreCase)) animal.Estado = "Muerto";

            if (request.LoteId.HasValue) animal.LoteId = request.LoteId.Value > 0 ? request.LoteId : null;
            animal.Etapa = nueva;
            animal.FechaEtapa = ahora;
            var nota = $"[{ahora:dd/MM/yyyy}] {anterior} → {nueva}{(string.IsNullOrWhiteSpace(request.Motivo) ? "" : ": " + request.Motivo.Trim())}";
            animal.Notas = string.IsNullOrWhiteSpace(animal.Notas) ? nota : $"{animal.Notas}\n{nota}";
            if (animal.Notas.Length > 500) animal.Notas = animal.Notas[^500..];
            animal.FechaActualizacion = ahora;
            await _db.SaveChangesAsync();

            string? loteCodigo = animal.LoteId.HasValue
                ? await _db.AgroLotes.AsNoTracking().Where(l => l.LoteId == animal.LoteId).Select(l => l.Codigo).FirstOrDefaultAsync()
                : null;
            var dto = MapAnimal(animal, loteCodigo);
            await EnriquecerAnimalesAsync(request.EmpresaId, new List<AgroAnimalDto> { dto });
            return dto;
        }

        // ───────────────────────── Trazabilidad: ficha individual ─────────────────────────

        public async Task<AgroFichaAnimalDto?> FichaAnimalAsync(int empresaId, int animalId)
        {
            var a = await _db.AgroAnimales.AsNoTracking().FirstOrDefaultAsync(x => x.AnimalId == animalId && x.EmpresaId == empresaId && x.Activo);
            if (a == null) return null;

            var lotes = await _db.AgroLotes.AsNoTracking().Where(l => l.EmpresaId == empresaId).ToDictionaryAsync(l => l.LoteId);
            var dto = MapAnimal(a, a.LoteId.HasValue && lotes.TryGetValue(a.LoteId.Value, out var lt) ? lt.Codigo : null);
            await EnriquecerAnimalesAsync(empresaId, new List<AgroAnimalDto> { dto });

            var pesajes = await ListarPesajesAsync(empresaId, animalId);
            var sanidad = await _db.AgroSanidad.AsNoTracking()
                .Where(s => s.EmpresaId == empresaId && s.Activo && s.Estado != "Cancelada"
                            && (s.AnimalId == animalId || (a.LoteId.HasValue && s.LoteId == a.LoteId)))
                .ToListAsync();
            var alertas = await _db.AgroAlertas.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.AnimalId == animalId && x.Activo)
                .OrderByDescending(x => x.FechaAlerta).ToListAsync();
            var crias = await _db.AgroAnimales.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.EstablecimientoId == a.EstablecimientoId && x.Activo && x.MadreCodigo == a.CodigoInterno)
                .Select(x => x.CodigoInterno).ToListAsync();
            var hoy = DateTime.UtcNow.Date;

            var ficha = new AgroFichaAnimalDto
            {
                Animal = dto,
                LoteNombre = a.LoteId.HasValue && lotes.TryGetValue(a.LoteId.Value, out var lote) ? lote.Nombre : null,
                Pesajes = pesajes.OrderBy(p => p.FechaPesaje).ToList(),
                Sanidad = sanidad.Select(s => MapSanidad(s, lotes, new List<AgroAnimal> { a }, hoy))
                    .OrderByDescending(s => s.FechaAplicacion ?? s.FechaProgramada).ToList(),
                Alertas = alertas.Select(x => new AgroAlertaDto
                {
                    AlertaId = x.AlertaId, AnimalId = x.AnimalId, AnimalCodigo = a.CodigoInterno, Tipo = x.Tipo,
                    Severidad = x.Severidad, Titulo = x.Titulo, Detalle = x.Detalle, Resuelta = x.Resuelta, FechaAlerta = x.FechaAlerta
                }).ToList(),
                Crias = crias
            };

            var ev = new List<AgroActividadDto>();
            if (a.FechaNacimiento.HasValue)
                ev.Add(new AgroActividadDto { Fecha = a.FechaNacimiento.Value, Tipo = "Etapa", Titulo = "Nacimiento",
                    Detalle = string.Join(" · ", new[] { a.PesoNacimientoKg.HasValue ? $"{a.PesoNacimientoKg:0.#} kg" : null,
                        a.MadreCodigo != null ? $"madre #{a.MadreCodigo}" : null, a.PadreCodigo != null ? $"padre {a.PadreCodigo}" : null }.Where(x => x != null)) });
            if (a.FechaIngreso.HasValue && a.FechaIngreso != a.FechaNacimiento)
                ev.Add(new AgroActividadDto { Fecha = a.FechaIngreso.Value, Tipo = "Etapa", Titulo = $"Ingreso a {EtapaNombre(a.Etapa)}",
                    Detalle = a.PesoIngresoKg.HasValue ? $"{a.PesoIngresoKg:0.#} kg" : null });
            var hitos = pesajes.Count <= 6 ? pesajes : pesajes.Where((p, i) => i == 0 || i == pesajes.Count - 1 || i % Math.Max(1, pesajes.Count / 5) == 0).ToList();
            ev.AddRange(hitos.Select(p => new AgroActividadDto { Fecha = p.FechaPesaje, Tipo = "Pesaje", Titulo = $"Pesaje {p.PesoKg:0.#} kg", Detalle = p.Origen }));
            ev.AddRange(ficha.Sanidad.Where(s => s.Estado == "Aplicada" && s.FechaAplicacion.HasValue).Select(s => new AgroActividadDto
            {
                Fecha = s.FechaAplicacion!.Value, Tipo = "Sanidad", Titulo = $"{s.Tipo}: {s.Producto}",
                Detalle = (s.LoteCodigo != null ? $"campaña lote {s.LoteCodigo}" : "individual") + (s.RetiroHasta.HasValue ? $" · retiro hasta {s.RetiroHasta:dd/MM}" : "")
            }));
            ev.AddRange(ficha.Alertas.Select(x => new AgroActividadDto { Fecha = x.FechaAlerta, Tipo = "Alerta", Titulo = x.Titulo, Detalle = x.Resuelta ? "Resuelta" : x.Detalle }));
            ficha.Eventos = ev.OrderByDescending(x => x.Fecha).ToList();
            return ficha;
        }
    }
}
