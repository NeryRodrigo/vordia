using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gesco.Application.Services.Boutique
{
    public class ClosetFiltro
    {
        public string? Q { get; set; }
        public string? Categoria { get; set; }
        public string? Ocasion { get; set; }
        public string? Talle { get; set; }
        public string? Color { get; set; }
        /// <summary>comprar | alquilar</summary>
        public string? Modo { get; set; }
        public string? Ciudad { get; set; }
        public string? Boutique { get; set; }
        public decimal? PrecioMax { get; set; }
        /// <summary>Fecha del evento: solo muestra lo que está libre para esa fecha.</summary>
        public DateTime? Fecha { get; set; }
        /// <summary>nuevo | precio | precio-desc | populares</summary>
        public string? Orden { get; set; }
    }

    public class PruebaPublicaRequest
    {
        public int PrendaId { get; set; }
        public int? VarianteId { get; set; }
        public string Nombre { get; set; } = "";
        public string Whatsapp { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public DateTime? FechaEvento { get; set; }
        public string? Mensaje { get; set; }
        /// <summary>Campo trampa para robots: una persona lo deja vacío.</summary>
        public string? Web { get; set; }
    }

    /// <summary>
    /// VORDIA CLOSET · Marketplace público: junta el stock real de todas las boutiques de Asunción y Gran Asunción
    /// que usan VORDIA BOUTIQUE. Lo que se vende en la tienda deja de aparecer acá al instante.
    /// </summary>
    public class ClosetService
    {
        public static readonly string[] Ciudades =
        {
            "Asunción", "San Lorenzo", "Luque", "Fernando de la Mora", "Lambaré", "Capiatá", "Mariano Roque Alonso",
            "Limpio", "Villa Elisa", "Ñemby", "San Antonio", "Itauguá", "Areguá"
        };

        private readonly IApplicationDbContext _db;
        public ClosetService(IApplicationDbContext db) => _db = db;

        private static DateTime HoyPy => DateTime.UtcNow.AddHours(-3).Date;

        /// <summary>Bloque de alquiler que cubre el evento: se retira el día anterior.</summary>
        public static (DateTime desde, DateTime hasta) BloqueEvento(DateTime evento, int dias)
        {
            var desde = evento.Date.AddDays(-1);
            return (desde, desde.AddDays(Math.Max(2, dias) - 1));
        }

        private static string Normal(string s) => BoutiqueService.Slug(s).Replace("-", " ");

        private record Fila(BoutiquePrenda P, BoutiquePerfil B, List<BoutiqueVariante> V, List<int> Fotos);

        private async Task<List<Fila>> CargarAsync(string? boutiqueSlug, int? prendaId = null)
        {
            var q = from p in _db.BoutiquePrendas
                    join b in _db.BoutiquePerfiles on p.EmpresaId equals b.EmpresaId
                    where p.Activo && p.Publicada && b.Publicada
                    select new { p, b };
            if (!string.IsNullOrWhiteSpace(boutiqueSlug)) q = q.Where(x => x.b.Slug == boutiqueSlug);
            if (prendaId.HasValue) q = q.Where(x => x.p.PrendaId == prendaId);
            var filas = await q.AsNoTracking().Take(3000).ToListAsync();
            var ids = filas.Select(f => f.p.PrendaId).ToList();
            var variantes = await _db.BoutiqueVariantes.AsNoTracking().Where(v => ids.Contains(v.PrendaId) && v.Activo).ToListAsync();
            var fotos = await _db.BoutiqueFotos.AsNoTracking().Where(f => ids.Contains(f.PrendaId)).OrderBy(f => f.Orden).Select(f => new { f.PrendaId, f.FotoId }).ToListAsync();
            return filas.Select(f => new Fila(f.p, f.b,
                variantes.Where(v => v.PrendaId == f.p.PrendaId).OrderBy(v => BoutiqueService.OrdenTalle(v.Talle)).ToList(),
                fotos.Where(x => x.PrendaId == f.p.PrendaId).Select(x => x.FotoId).ToList())).ToList();
        }

        /// <summary>Unidades libres por variante para el bloque del evento (stock menos alquileres superpuestos).</summary>
        private async Task<Dictionary<int, int>> LibresAsync(List<Fila> filas, DateTime? evento)
        {
            var libres = filas.SelectMany(f => f.V).ToDictionary(v => v.VarianteId, v => v.Stock);
            var vids = libres.Keys.ToList();
            var hoy = HoyPy;
            var alquileres = await _db.BoutiqueAlquileres.AsNoTracking()
                .Where(a => vids.Contains(a.VarianteId) && (a.Estado == "Reservado" || a.Estado == "Retirado") && a.Hasta >= hoy)
                .Select(a => new { a.VarianteId, a.Desde, a.Hasta }).ToListAsync();
            foreach (var f in filas)
            {
                var (desde, hasta) = evento.HasValue ? BloqueEvento(evento.Value, f.P.DiasAlquiler) : (hoy, hoy);
                foreach (var v in f.V)
                    libres[v.VarianteId] = Math.Max(0, v.Stock - alquileres.Count(a => a.VarianteId == v.VarianteId && a.Desde <= hasta && a.Hasta >= desde));
            }
            return libres;
        }

        private static object Tarjeta(Fila f, Dictionary<int, int> libres)
        {
            var disponibles = f.V.Where(v => libres.GetValueOrDefault(v.VarianteId) > 0).ToList();
            return new
            {
                prendaId = f.P.PrendaId,
                nombre = f.P.Nombre,
                categoria = f.P.Categoria,
                ocasiones = f.P.Ocasiones,
                modalidad = f.P.Modalidad,
                precioVenta = f.P.PrecioVenta,
                precioAlquiler = f.P.PrecioAlquiler,
                diasAlquiler = f.P.DiasAlquiler,
                precioAnterior = f.P.PrecioAnterior,
                colorHex = f.P.ColorHex,
                fotos = f.Fotos.Take(2),
                talles = disponibles.Select(v => v.Talle).Distinct(),
                colores = f.V.GroupBy(v => v.Color).Select(g => new { nombre = g.Key, hex = g.First().ColorHex }),
                unidades = disponibles.Sum(v => libres[v.VarianteId]),
                nuevo = f.P.FechaAlta >= DateTime.UtcNow.AddDays(-14),
                boutique = new { nombre = f.B.NombreComercial, slug = f.B.Slug, ciudad = f.B.Ciudad, barrio = f.B.Barrio, verificada = f.B.Verificada, demo = f.B.Demo }
            };
        }

        public async Task<object> CatalogoAsync(ClosetFiltro fi)
        {
            var filas = await CargarAsync(fi.Boutique);
            var evento = fi.Fecha?.Date;
            if (evento.HasValue && evento < HoyPy) evento = null;
            var libres = await LibresAsync(filas, evento);

            IEnumerable<Fila> r = filas;
            if (!string.IsNullOrWhiteSpace(fi.Q))
            {
                var palabras = Normal(fi.Q).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                r = r.Where(f =>
                {
                    var texto = Normal($"{f.P.Nombre} {f.P.Categoria} {f.P.Ocasiones} {f.P.Material} {string.Join(' ', f.V.Select(v => v.Color))}");
                    return palabras.All(texto.Contains);
                });
            }
            if (!string.IsNullOrWhiteSpace(fi.Categoria)) r = r.Where(f => f.P.Categoria == fi.Categoria);
            if (!string.IsNullOrWhiteSpace(fi.Ocasion)) r = r.Where(f => (f.P.Ocasiones ?? "").Split(',').Any(o => o.Trim().Equals(fi.Ocasion, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(fi.Ciudad)) r = r.Where(f => string.Equals(f.B.Ciudad, fi.Ciudad, StringComparison.OrdinalIgnoreCase));
            if (fi.Modo == "alquilar") r = r.Where(f => f.P.Modalidad != "Venta");
            if (fi.Modo == "comprar") r = r.Where(f => f.P.Modalidad != "Alquiler");
            if (fi.PrecioMax is > 0)
                r = r.Where(f => fi.Modo == "alquilar" ? f.P.PrecioAlquiler <= fi.PrecioMax : fi.Modo == "comprar" ? f.P.PrecioVenta <= fi.PrecioMax
                    : (f.P.PrecioVenta > 0 && f.P.PrecioVenta <= fi.PrecioMax) || (f.P.PrecioAlquiler > 0 && f.P.PrecioAlquiler <= fi.PrecioMax));
            // Solo lo que realmente está: con el talle y color pedidos libres (y libre en la fecha del evento si se indicó)
            r = r.Where(f => f.V.Any(v => libres.GetValueOrDefault(v.VarianteId) > 0
                && (string.IsNullOrWhiteSpace(fi.Talle) || v.Talle.Equals(fi.Talle, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(fi.Color) || v.Color.Equals(fi.Color, StringComparison.OrdinalIgnoreCase))));

            static decimal Precio(Fila f) => f.P.PrecioVenta > 0 ? f.P.PrecioVenta : f.P.PrecioAlquiler;
            r = fi.Orden switch
            {
                "precio" => r.OrderBy(Precio),
                "precio-desc" => r.OrderByDescending(Precio),
                "populares" => r.OrderByDescending(f => f.P.Vistas),
                // Por defecto: lo nuevo primero, pero las boutiques reales delante de las de ejemplo
                _ => r.OrderBy(f => f.B.Demo).ThenByDescending(f => f.P.FechaAlta)
            };
            var lista = r.Take(240).ToList();

            return new
            {
                total = lista.Count,
                prendas = lista.Select(f => Tarjeta(f, libres)),
                facetas = new
                {
                    categorias = filas.Select(f => f.P.Categoria).Distinct().OrderBy(c => Array.IndexOf(BoutiqueService.Categorias, c)),
                    ocasiones = filas.SelectMany(f => (f.P.Ocasiones ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).GroupBy(o => o).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(14),
                    talles = filas.SelectMany(f => f.V.Select(v => v.Talle)).Distinct().OrderBy(BoutiqueService.OrdenTalle),
                    colores = filas.SelectMany(f => f.V).GroupBy(v => v.Color).OrderByDescending(g => g.Count()).Take(20).Select(g => new { nombre = g.Key, hex = g.First().ColorHex }),
                    ciudades = filas.Select(f => f.B.Ciudad).Where(c => c != null).Distinct().OrderBy(c => Array.IndexOf(Ciudades, c)),
                },
                boutiques = filas.GroupBy(f => f.B.PerfilId).Select(g => g.First().B).Count()
            };
        }

        public async Task<object> BoutiquesAsync()
        {
            var perfiles = await _db.BoutiquePerfiles.AsNoTracking().Where(b => b.Publicada).ToListAsync();
            var ids = perfiles.Select(p => p.EmpresaId).ToList();
            var conteo = await _db.BoutiquePrendas.Where(p => ids.Contains(p.EmpresaId) && p.Activo && p.Publicada)
                .GroupBy(p => p.EmpresaId).Select(g => new { g.Key, n = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.n);
            var muestra = await _db.BoutiquePrendas.AsNoTracking().Where(p => ids.Contains(p.EmpresaId) && p.Activo && p.Publicada)
                .OrderByDescending(p => p.Vistas).Select(p => new { p.EmpresaId, p.ColorHex }).ToListAsync();
            return perfiles.OrderBy(p => p.Demo).ThenByDescending(p => p.Verificada).ThenBy(p => p.NombreComercial).Select(p => new
            {
                nombre = p.NombreComercial, slug = p.Slug, descripcion = p.Descripcion, ciudad = p.Ciudad, barrio = p.Barrio,
                estilo = p.Estilo, verificada = p.Verificada, demo = p.Demo, prendas = conteo.GetValueOrDefault(p.EmpresaId),
                paleta = muestra.Where(m => m.EmpresaId == p.EmpresaId && m.ColorHex != null).Select(m => m.ColorHex).Distinct().Take(4)
            });
        }

        public async Task<object?> BoutiqueAsync(string slug)
        {
            var b = await _db.BoutiquePerfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.Publicada);
            if (b == null) return null;
            return new
            {
                nombre = b.NombreComercial, slug = b.Slug, descripcion = b.Descripcion, ciudad = b.Ciudad, barrio = b.Barrio, direccion = b.Direccion,
                whatsapp = b.Whatsapp, instagram = b.Instagram, horario = b.Horario, estilo = b.Estilo, verificada = b.Verificada, demo = b.Demo
            };
        }

        public async Task<object?> PrendaAsync(int prendaId, DateTime? fecha)
        {
            var filas = await CargarAsync(null, prendaId);
            var f = filas.FirstOrDefault();
            if (f == null) return null;
            var evento = fecha?.Date >= HoyPy ? fecha?.Date : null;
            var libres = await LibresAsync(filas, evento);

            // Contador de vistas para el panel de la boutique
            var entidad = await _db.BoutiquePrendas.FirstAsync(p => p.PrendaId == prendaId);
            entidad.Vistas++;
            await _db.SaveChangesAsync();

            var otras = await CargarAsync(f.B.Slug);
            var libresOtras = await LibresAsync(otras, null);
            var bloque = evento.HasValue ? BloqueEvento(evento.Value, f.P.DiasAlquiler) : ((DateTime, DateTime)?)null;
            return new
            {
                prenda = Tarjeta(f, libres),
                descripcion = f.P.Descripcion,
                material = f.P.Material,
                garantia = f.P.Garantia,
                fotos = f.Fotos,
                bloque = bloque.HasValue ? new { desde = bloque.Value.Item1, hasta = bloque.Value.Item2 } : null,
                variantes = f.V.Select(v => new
                {
                    varianteId = v.VarianteId, talle = v.Talle, color = v.Color, colorHex = v.ColorHex,
                    busto = v.BustoCm, cintura = v.CinturaCm, cadera = v.CaderaCm, largo = v.LargoCm,
                    // No se publica el stock exacto: solo si hay y si es la última
                    disponible = libres.GetValueOrDefault(v.VarianteId) > 0,
                    ultima = libres.GetValueOrDefault(v.VarianteId) == 1
                }),
                boutique = await BoutiqueAsync(f.B.Slug),
                masDeLaBoutique = otras.Where(o => o.P.PrendaId != prendaId && o.V.Any(v => libresOtras.GetValueOrDefault(v.VarianteId) > 0))
                    .OrderByDescending(o => o.P.Vistas).Take(4).Select(o => Tarjeta(o, libresOtras))
            };
        }

        /// <summary>Pedido de prueba desde el marketplace: llega al panel de la boutique y la clienta queda registrada.</summary>
        public async Task<object> PedirPruebaAsync(PruebaPublicaRequest r)
        {
            if (!string.IsNullOrEmpty(r.Web)) return new { ok = true }; // robot: se ignora en silencio
            var nombre = r.Nombre?.Trim() ?? "";
            var digitos = Regex.Replace(r.Whatsapp ?? "", @"\D", "");
            if (nombre.Length < 2) throw new InvalidOperationException("Contanos tu nombre");
            if (digitos.Length < 8 || digitos.Length > 15) throw new InvalidOperationException("Revisá tu número de WhatsApp");
            var ahoraPy = DateTime.UtcNow.AddHours(-3);
            if (r.FechaHora < ahoraPy.AddHours(1) || r.FechaHora > ahoraPy.AddDays(60)) throw new InvalidOperationException("Elegí un día y hora desde mañana y dentro de los próximos 2 meses");
            if (r.FechaHora.Hour < 8 || r.FechaHora.Hour > 20) throw new InvalidOperationException("Elegí un horario entre las 8:00 y las 20:00");

            var p = await (from pr in _db.BoutiquePrendas
                           join b in _db.BoutiquePerfiles on pr.EmpresaId equals b.EmpresaId
                           where pr.PrendaId == r.PrendaId && pr.Activo && pr.Publicada && b.Publicada
                           select pr).FirstOrDefaultAsync() ?? throw new KeyNotFoundException();
            if (r.VarianteId.HasValue && !await _db.BoutiqueVariantes.AnyAsync(v => v.VarianteId == r.VarianteId && v.PrendaId == p.PrendaId && v.Activo))
                throw new InvalidOperationException("Ese talle ya no está");
            var pendientes = await _db.BoutiquePruebas.CountAsync(x => x.EmpresaId == p.EmpresaId && x.Whatsapp == digitos && (x.Estado == "Pedida" || x.Estado == "Confirmada"));
            if (pendientes >= 5) throw new InvalidOperationException("Ya tenés varias pruebas pedidas en esta boutique: te van a escribir por WhatsApp");

            var corto9 = digitos.Length > 9 ? digitos[^9..] : digitos;
            var clientas = await _db.BoutiqueClientas.Where(c => c.EmpresaId == p.EmpresaId && c.Whatsapp != null).ToListAsync();
            if (!clientas.Any(c => Regex.Replace(c.Whatsapp!, @"\D", "").EndsWith(corto9)))
                _db.BoutiqueClientas.Add(new BoutiqueClienta { EmpresaId = p.EmpresaId, Nombre = nombre.Length > 150 ? nombre[..150] : nombre, Whatsapp = digitos, Origen = "Marketplace", FechaAlta = DateTime.UtcNow });

            var prueba = new BoutiquePrueba
            {
                EmpresaId = p.EmpresaId, PrendaId = p.PrendaId, VarianteId = r.VarianteId, Nombre = nombre.Length > 150 ? nombre[..150] : nombre, Whatsapp = digitos,
                FechaHora = r.FechaHora, FechaEvento = r.FechaEvento?.Date, Estado = "Pedida",
                Mensaje = string.IsNullOrWhiteSpace(r.Mensaje) ? null : r.Mensaje.Trim()[..Math.Min(500, r.Mensaje.Trim().Length)], Fecha = DateTime.UtcNow
            };
            _db.BoutiquePruebas.Add(prueba);
            await _db.SaveChangesAsync();
            var boutique = await _db.BoutiquePerfiles.AsNoTracking().FirstAsync(b => b.EmpresaId == p.EmpresaId);
            return new { ok = true, pruebaId = prueba.PruebaId, boutique = boutique.NombreComercial, whatsapp = boutique.Whatsapp };
        }
    }
}
