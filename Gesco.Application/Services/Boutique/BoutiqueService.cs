using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gesco.Application.Services.Boutique
{
    public class VarianteRequest
    {
        public int? VarianteId { get; set; }
        public string Talle { get; set; } = "Único";
        public string Color { get; set; } = "";
        public string? ColorHex { get; set; }
        public int Stock { get; set; }
        public int? BustoCm { get; set; }
        public int? CinturaCm { get; set; }
        public int? CaderaCm { get; set; }
        public int? LargoCm { get; set; }
    }

    public class PrendaRequest
    {
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "Vestidos";
        public string? Ocasiones { get; set; }
        public string? Descripcion { get; set; }
        public string? Material { get; set; }
        public string Modalidad { get; set; } = "Venta";
        public decimal PrecioVenta { get; set; }
        public decimal PrecioAlquiler { get; set; }
        public int DiasAlquiler { get; set; } = 4;
        public decimal Garantia { get; set; }
        public decimal? PrecioAnterior { get; set; }
        public string? ColorHex { get; set; }
        public bool Publicada { get; set; } = true;
        public List<VarianteRequest> Variantes { get; set; } = new();
    }

    public class PerfilRequest
    {
        public string NombreComercial { get; set; } = "";
        public string? Slug { get; set; }
        public string? Descripcion { get; set; }
        public string? Ciudad { get; set; }
        public string? Barrio { get; set; }
        public string? Direccion { get; set; }
        public string? Whatsapp { get; set; }
        public string? Instagram { get; set; }
        public string? Horario { get; set; }
        public string? Estilo { get; set; }
        public bool Publicada { get; set; } = true;
    }

    public class ClientaRequest
    {
        public string Nombre { get; set; } = "";
        public string? Whatsapp { get; set; }
        public string? Email { get; set; }
        public string? Talle { get; set; }
        public DateTime? Cumpleanos { get; set; }
        public string? Notas { get; set; }
    }

    public class VentaItemRequest { public int VarianteId { get; set; } public int Cantidad { get; set; } = 1; public decimal? Precio { get; set; } }

    public class VentaRequest
    {
        public List<VentaItemRequest> Items { get; set; } = new();
        public int? ClientaId { get; set; }
        public string? ClientaNombre { get; set; }
        public string? ClientaWhatsapp { get; set; }
        public string? MedioPago { get; set; }
        public string Canal { get; set; } = "Tienda";
        /// <summary>Apartado: se guarda la prenda con una seña por unos días.</summary>
        public bool Apartado { get; set; }
        public decimal? Sena { get; set; }
        public int DiasApartado { get; set; } = 3;
        public string? Notas { get; set; }
    }

    public class AlquilerRequest
    {
        public int VarianteId { get; set; }
        public string ClientaNombre { get; set; } = "";
        public string? ClientaWhatsapp { get; set; }
        public DateTime Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public decimal? Sena { get; set; }
        public string? Notas { get; set; }
    }

    public record FotoArchivo(byte[] Datos, string ContentType);

    /// <summary>
    /// VORDIA BOUTIQUE: gestión de la boutique (catálogo con talles y colores, stock, caja con apartados,
    /// alquileres por fecha, pruebas, clientas) y su vidriera pública en el marketplace.
    /// </summary>
    public class BoutiqueService
    {
        public static readonly string[] Categorias = { "Vestidos", "Blusas", "Pantalones", "Faldas", "Conjuntos", "Abrigos", "Calzados", "Carteras", "Accesorios" };
        private static readonly string[] Modalidades = { "Venta", "Alquiler", "Ambos" };
        private const int MaxFotosPorPrenda = 8;
        private const long MaxBytesFoto = 6 * 1024 * 1024;

        private readonly IApplicationDbContext _db;
        private readonly string _almacen;

        public BoutiqueService(IApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _almacen = config["Storage:Boutique"] ?? Path.Combine(Directory.GetCurrentDirectory(), "almacen-crm", "boutique");
        }

        private static string? Corto(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);
        private static DateTime HoyPy => DateTime.UtcNow.AddHours(-3).Date;

        public static string Slug(string texto)
        {
            var norm = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in norm) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');
        }

        // ───────────── Vidriera (perfil) ─────────────

        public Task<BoutiquePerfil?> PerfilAsync(int empresaId) => _db.BoutiquePerfiles.FirstOrDefaultAsync(p => p.EmpresaId == empresaId);

        public async Task<BoutiquePerfil> GuardarPerfilAsync(int empresaId, PerfilRequest r)
        {
            if (string.IsNullOrWhiteSpace(r.NombreComercial)) throw new InvalidOperationException("El nombre de la boutique es obligatorio");
            var slug = Slug(string.IsNullOrWhiteSpace(r.Slug) ? r.NombreComercial : r.Slug);
            if (slug.Length < 3) throw new InvalidOperationException("La dirección de la vidriera es muy corta");
            if (slug is "prenda" or "mi-lista" or "boutiques") throw new InvalidOperationException("Esa dirección está reservada: probá con otra");
            if (await _db.BoutiquePerfiles.AnyAsync(p => p.Slug == slug && p.EmpresaId != empresaId)) throw new InvalidOperationException("Esa dirección ya la usa otra boutique: probá con otra");
            var p = await PerfilAsync(empresaId) ?? new BoutiquePerfil { EmpresaId = empresaId, FechaAlta = DateTime.UtcNow };
            p.NombreComercial = Corto(r.NombreComercial, 120)!;
            p.Slug = slug.Length > 80 ? slug[..80] : slug;
            p.Descripcion = Corto(r.Descripcion, 500);
            p.Ciudad = Corto(r.Ciudad, 60);
            p.Barrio = Corto(r.Barrio, 80);
            p.Direccion = Corto(r.Direccion, 250);
            p.Whatsapp = Corto(r.Whatsapp, 40);
            p.Instagram = Corto(r.Instagram?.TrimStart('@'), 80);
            p.Horario = Corto(r.Horario, 200);
            p.Estilo = Corto(r.Estilo, 60);
            p.Publicada = r.Publicada;
            if (p.PerfilId == 0) _db.BoutiquePerfiles.Add(p);
            await _db.SaveChangesAsync();
            return p;
        }

        // ───────────── Prendas ─────────────

        public async Task<object> PrendasAsync(int empresaId)
        {
            var prendas = await _db.BoutiquePrendas.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Activo).OrderByDescending(p => p.FechaAlta).ToListAsync();
            var ids = prendas.Select(p => p.PrendaId).ToList();
            var variantes = await _db.BoutiqueVariantes.AsNoTracking().Where(v => ids.Contains(v.PrendaId) && v.Activo).ToListAsync();
            var fotos = await _db.BoutiqueFotos.AsNoTracking().Where(f => ids.Contains(f.PrendaId)).OrderBy(f => f.Orden).ToListAsync();
            return prendas.Select(p => new
            {
                prenda = p,
                variantes = variantes.Where(v => v.PrendaId == p.PrendaId).OrderBy(v => v.Color).ThenBy(v => OrdenTalle(v.Talle)),
                fotos = fotos.Where(f => f.PrendaId == p.PrendaId).Select(f => f.FotoId),
                stock = variantes.Where(v => v.PrendaId == p.PrendaId).Sum(v => v.Stock)
            });
        }

        public static int OrdenTalle(string t) => t.ToUpperInvariant() switch
        {
            "XS" => 1, "S" => 2, "M" => 3, "L" => 4, "XL" => 5, "XXL" => 6, "ÚNICO" or "UNICO" => 0,
            _ => int.TryParse(t, out var n) ? 10 + n : 99
        };

        public async Task<BoutiquePrenda> GuardarPrendaAsync(int empresaId, PrendaRequest r, int? prendaId = null)
        {
            if (string.IsNullOrWhiteSpace(r.Nombre)) throw new InvalidOperationException("Poné un nombre a la prenda");
            if (!Categorias.Contains(r.Categoria)) throw new InvalidOperationException("Categoría inválida");
            if (!Modalidades.Contains(r.Modalidad)) throw new InvalidOperationException("Modalidad inválida");
            if (r.Modalidad != "Alquiler" && r.PrecioVenta <= 0) throw new InvalidOperationException("Indicá el precio de venta");
            if (r.Modalidad != "Venta" && r.PrecioAlquiler <= 0) throw new InvalidOperationException("Indicá el precio de alquiler");
            if (r.Variantes.Count == 0) throw new InvalidOperationException("Agregá al menos un talle y color");
            if (r.Variantes.Any(v => string.IsNullOrWhiteSpace(v.Talle) || string.IsNullOrWhiteSpace(v.Color) || v.Stock < 0))
                throw new InvalidOperationException("Cada variante necesita talle, color y stock (0 o más)");
            if (r.Variantes.GroupBy(v => (v.Talle.Trim().ToUpperInvariant(), v.Color.Trim().ToUpperInvariant())).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Hay talles y colores repetidos");

            var p = prendaId.HasValue
                ? await _db.BoutiquePrendas.FirstOrDefaultAsync(x => x.PrendaId == prendaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException()
                : new BoutiquePrenda { EmpresaId = empresaId, FechaAlta = DateTime.UtcNow };
            p.Nombre = Corto(r.Nombre, 150)!;
            p.Categoria = r.Categoria;
            p.Ocasiones = Corto(r.Ocasiones, 200);
            p.Descripcion = Corto(r.Descripcion, 1500);
            p.Material = Corto(r.Material, 120);
            p.Modalidad = r.Modalidad;
            p.PrecioVenta = r.Modalidad == "Alquiler" ? 0 : Math.Round(r.PrecioVenta);
            p.PrecioAlquiler = r.Modalidad == "Venta" ? 0 : Math.Round(r.PrecioAlquiler);
            p.DiasAlquiler = Math.Clamp(r.DiasAlquiler, 1, 15);
            p.Garantia = Math.Max(0, Math.Round(r.Garantia));
            p.PrecioAnterior = r.PrecioAnterior is > 0 && r.PrecioAnterior > p.PrecioVenta ? Math.Round(r.PrecioAnterior.Value) : null;
            p.ColorHex = ColorValido(r.ColorHex) ?? ColorValido(r.Variantes[0].ColorHex);
            p.Publicada = r.Publicada;
            if (!prendaId.HasValue) _db.BoutiquePrendas.Add(p);
            await _db.SaveChangesAsync();

            var actuales = await _db.BoutiqueVariantes.Where(v => v.PrendaId == p.PrendaId).ToListAsync();
            foreach (var vr in r.Variantes)
            {
                var v = vr.VarianteId.HasValue ? actuales.FirstOrDefault(a => a.VarianteId == vr.VarianteId) : null;
                v ??= actuales.FirstOrDefault(a => a.Talle.Equals(vr.Talle.Trim(), StringComparison.OrdinalIgnoreCase) && a.Color.Equals(vr.Color.Trim(), StringComparison.OrdinalIgnoreCase));
                if (v == null)
                {
                    v = new BoutiqueVariante { EmpresaId = empresaId, PrendaId = p.PrendaId };
                    _db.BoutiqueVariantes.Add(v);
                    actuales.Add(v);
                }
                v.Talle = Corto(vr.Talle, 10)!;
                v.Color = Corto(vr.Color, 40)!;
                v.ColorHex = ColorValido(vr.ColorHex);
                v.Stock = vr.Stock;
                v.BustoCm = vr.BustoCm; v.CinturaCm = vr.CinturaCm; v.CaderaCm = vr.CaderaCm; v.LargoCm = vr.LargoCm;
                v.Activo = true;
            }
            // Las que ya no están se desactivan (quedan para el historial de ventas y alquileres)
            foreach (var v in actuales.Where(a => a.VarianteId != 0 && !r.Variantes.Any(vr =>
                         (vr.VarianteId.HasValue && vr.VarianteId == a.VarianteId) ||
                         (vr.Talle.Trim().Equals(a.Talle, StringComparison.OrdinalIgnoreCase) && vr.Color.Trim().Equals(a.Color, StringComparison.OrdinalIgnoreCase)))))
                v.Activo = false;
            await _db.SaveChangesAsync();
            return p;
        }

        private static string? ColorValido(string? hex) => hex != null && Regex.IsMatch(hex, "^#[0-9a-fA-F]{6}$") ? hex : null;

        public async Task<bool> EliminarPrendaAsync(int empresaId, int prendaId)
        {
            var p = await _db.BoutiquePrendas.FirstOrDefaultAsync(x => x.PrendaId == prendaId && x.EmpresaId == empresaId);
            if (p == null) return false;
            p.Activo = false;
            p.Publicada = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────── Fotos ─────────────

        /// <summary>Solo JPG, PNG o WEBP reales (se revisa la firma del archivo, no la extensión).</summary>
        private static string? TipoImagen(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length > 12 && Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && Encoding.ASCII.GetString(b, 8, 4) == "WEBP") return "image/webp";
            return null;
        }

        public async Task<BoutiqueFoto> SubirFotoAsync(int empresaId, int prendaId, byte[] datos)
        {
            if (!await _db.BoutiquePrendas.AnyAsync(p => p.PrendaId == prendaId && p.EmpresaId == empresaId)) throw new KeyNotFoundException();
            if (datos.Length == 0 || datos.Length > MaxBytesFoto) throw new InvalidOperationException("La foto debe pesar menos de 6 MB");
            var tipo = TipoImagen(datos) ?? throw new InvalidOperationException("Subí una foto JPG, PNG o WEBP");
            var cantidad = await _db.BoutiqueFotos.CountAsync(f => f.PrendaId == prendaId);
            if (cantidad >= MaxFotosPorPrenda) throw new InvalidOperationException($"Máximo {MaxFotosPorPrenda} fotos por prenda");
            Directory.CreateDirectory(_almacen);
            var archivo = $"{Guid.NewGuid():N}{(tipo == "image/png" ? ".png" : tipo == "image/webp" ? ".webp" : ".jpg")}";
            await File.WriteAllBytesAsync(Path.Combine(_almacen, archivo), datos);
            var f = new BoutiqueFoto { EmpresaId = empresaId, PrendaId = prendaId, Archivo = archivo, ContentType = tipo, Orden = cantidad, Fecha = DateTime.UtcNow };
            _db.BoutiqueFotos.Add(f);
            await _db.SaveChangesAsync();
            return f;
        }

        public async Task<bool> EliminarFotoAsync(int empresaId, int fotoId)
        {
            var f = await _db.BoutiqueFotos.FirstOrDefaultAsync(x => x.FotoId == fotoId && x.EmpresaId == empresaId);
            if (f == null) return false;
            _db.BoutiqueFotos.Remove(f);
            await _db.SaveChangesAsync();
            try { File.Delete(Path.Combine(_almacen, f.Archivo)); } catch (IOException) { /* el archivo puede no existir */ }
            return true;
        }

        /// <summary>Foto para la web pública: solo de prendas publicadas de vidrieras publicadas.</summary>
        public async Task<FotoArchivo?> FotoPublicaAsync(int fotoId)
        {
            var f = await (from fo in _db.BoutiqueFotos
                           join pr in _db.BoutiquePrendas on fo.PrendaId equals pr.PrendaId
                           join pe in _db.BoutiquePerfiles on pr.EmpresaId equals pe.EmpresaId
                           where fo.FotoId == fotoId && pr.Activo && pr.Publicada && pe.Publicada
                           select fo).AsNoTracking().FirstOrDefaultAsync();
            return f == null ? null : await LeerAsync(f);
        }

        public async Task<FotoArchivo?> FotoPrivadaAsync(int empresaId, int fotoId)
        {
            var f = await _db.BoutiqueFotos.AsNoTracking().FirstOrDefaultAsync(x => x.FotoId == fotoId && x.EmpresaId == empresaId);
            return f == null ? null : await LeerAsync(f);
        }

        private async Task<FotoArchivo?> LeerAsync(BoutiqueFoto f)
        {
            var ruta = Path.Combine(_almacen, Path.GetFileName(f.Archivo));
            return File.Exists(ruta) ? new FotoArchivo(await File.ReadAllBytesAsync(ruta), f.ContentType) : null;
        }

        // ───────────── Clientas ─────────────

        public Task<List<BoutiqueClienta>> ClientasAsync(int empresaId) =>
            _db.BoutiqueClientas.AsNoTracking().Where(c => c.EmpresaId == empresaId).OrderBy(c => c.Nombre).Take(2000).ToListAsync();

        public async Task<BoutiqueClienta> GuardarClientaAsync(int empresaId, ClientaRequest r, int? clientaId = null)
        {
            if (string.IsNullOrWhiteSpace(r.Nombre)) throw new InvalidOperationException("El nombre es obligatorio");
            var c = clientaId.HasValue
                ? await _db.BoutiqueClientas.FirstOrDefaultAsync(x => x.ClientaId == clientaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException()
                : new BoutiqueClienta { EmpresaId = empresaId, Origen = "Manual", FechaAlta = DateTime.UtcNow };
            c.Nombre = Corto(r.Nombre, 150)!;
            c.Whatsapp = Corto(r.Whatsapp, 40);
            c.Email = Corto(r.Email, 150);
            c.Talle = Corto(r.Talle, 10);
            c.Cumpleanos = r.Cumpleanos?.Date;
            c.Notas = Corto(r.Notas, 500);
            if (!clientaId.HasValue) _db.BoutiqueClientas.Add(c);
            await _db.SaveChangesAsync();
            return c;
        }

        private async Task<BoutiqueClienta?> ClientaPorWhatsappAsync(int empresaId, string? nombre, string? whatsapp, string origen)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            var digitos = Regex.Replace(whatsapp ?? "", @"\D", "");
            var nombreLimpio = nombre.Trim();
            // Con WhatsApp se busca por número; sin número, por nombre exacto
            var c = digitos.Length >= 6
                ? (await _db.BoutiqueClientas.Where(x => x.EmpresaId == empresaId && x.Whatsapp != null).ToListAsync())
                    .FirstOrDefault(x => Regex.Replace(x.Whatsapp!, @"\D", "").EndsWith(digitos.Length > 9 ? digitos[^9..] : digitos))
                : await _db.BoutiqueClientas.FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Nombre == nombreLimpio);
            if (c != null) return c;
            c = new BoutiqueClienta { EmpresaId = empresaId, Nombre = Corto(nombre, 150)!, Whatsapp = Corto(whatsapp, 40), Origen = origen, FechaAlta = DateTime.UtcNow };
            _db.BoutiqueClientas.Add(c);
            return c;
        }

        // ───────────── Caja: ventas y apartados ─────────────

        public async Task<BoutiqueVenta> VenderAsync(int empresaId, VentaRequest r, string? vendedora)
        {
            if (r.Items.Count == 0) throw new InvalidOperationException("Agregá al menos una prenda");
            var ids = r.Items.Select(i => i.VarianteId).ToList();
            var variantes = await _db.BoutiqueVariantes.Where(v => ids.Contains(v.VarianteId) && v.EmpresaId == empresaId && v.Activo).ToListAsync();
            var prendaIds = variantes.Select(v => v.PrendaId).Distinct().ToList();
            var prendas = await _db.BoutiquePrendas.Where(p => prendaIds.Contains(p.PrendaId)).ToDictionaryAsync(p => p.PrendaId);

            var venta = new BoutiqueVenta
            {
                EmpresaId = empresaId, Estado = r.Apartado ? "Apartada" : "Pagada", MedioPago = Corto(r.MedioPago, 20),
                Canal = r.Canal is "WhatsApp" or "Instagram" or "Marketplace" ? r.Canal : "Tienda",
                Notas = Corto(r.Notas, 300), Vendedora = Corto(vendedora, 150), Fecha = DateTime.UtcNow,
                VenceApartado = r.Apartado ? HoyPy.AddDays(Math.Clamp(r.DiasApartado, 1, 15)) : null
            };
            var items = new List<BoutiqueVentaItem>();
            foreach (var i in r.Items)
            {
                var v = variantes.FirstOrDefault(x => x.VarianteId == i.VarianteId) ?? throw new InvalidOperationException("Una de las prendas ya no existe");
                var p = prendas[v.PrendaId];
                if (i.Cantidad < 1) throw new InvalidOperationException("Cantidad inválida");
                if (p.Modalidad == "Alquiler") throw new InvalidOperationException($"{p.Nombre} es solo para alquilar: registralo en Alquileres");
                if (v.Stock < i.Cantidad) throw new InvalidOperationException($"No hay stock de {p.Nombre} talle {v.Talle} {v.Color} (quedan {v.Stock})");
                // Prendas de venta y alquiler: no se vende la pieza que una clienta ya tiene reservada para su evento
                if (p.Modalidad == "Ambos" && v.Stock - i.Cantidad < await MaxAlquiladasALaVezAsync(v.VarianteId))
                    throw new InvalidOperationException($"{p.Nombre} talle {v.Talle} {v.Color} está reservado para alquiler: no se puede vender esa pieza");
                v.Stock -= i.Cantidad;
                var precio = i.Precio is > 0 ? Math.Round(i.Precio.Value) : p.PrecioVenta;
                items.Add(new BoutiqueVentaItem { VarianteId = v.VarianteId, Cantidad = i.Cantidad, PrecioUnitario = precio, Descripcion = Corto($"{p.Nombre} · {v.Talle} · {v.Color}", 220)! });
            }
            venta.Total = items.Sum(x => x.PrecioUnitario * x.Cantidad);
            venta.Pagado = r.Apartado ? Math.Min(venta.Total, Math.Max(0, Math.Round(r.Sena ?? 0))) : venta.Total;
            if (r.Apartado && venta.Pagado <= 0) throw new InvalidOperationException("Indicá la seña del apartado");

            var clienta = r.ClientaId.HasValue
                ? await _db.BoutiqueClientas.FirstOrDefaultAsync(c => c.ClientaId == r.ClientaId && c.EmpresaId == empresaId)
                : await ClientaPorWhatsappAsync(empresaId, r.ClientaNombre, r.ClientaWhatsapp, "Caja");
            if (r.Apartado && clienta == null) throw new InvalidOperationException("Para apartar, cargá el nombre de la clienta");

            _db.BoutiqueVentas.Add(venta);
            await _db.SaveChangesAsync();
            venta.ClientaId = clienta?.ClientaId;
            foreach (var it in items) { it.VentaId = venta.VentaId; _db.BoutiqueVentaItems.Add(it); }
            await _db.SaveChangesAsync();
            return venta;
        }

        public async Task<object> VentasAsync(int empresaId, int dias)
        {
            var desde = DateTime.UtcNow.AddDays(-Math.Clamp(dias, 1, 400));
            var ventas = await _db.BoutiqueVentas.AsNoTracking().Where(v => v.EmpresaId == empresaId && (v.Fecha >= desde || v.Estado == "Apartada")).OrderByDescending(v => v.Fecha).Take(500).ToListAsync();
            var ids = ventas.Select(v => v.VentaId).ToList();
            var items = await _db.BoutiqueVentaItems.AsNoTracking().Where(i => ids.Contains(i.VentaId)).ToListAsync();
            var clientaIds = ventas.Where(v => v.ClientaId.HasValue).Select(v => v.ClientaId!.Value).Distinct().ToList();
            var clientas = await _db.BoutiqueClientas.AsNoTracking().Where(c => clientaIds.Contains(c.ClientaId)).ToDictionaryAsync(c => c.ClientaId, c => c.Nombre);
            return ventas.Select(v => new { venta = v, clienta = v.ClientaId.HasValue && clientas.TryGetValue(v.ClientaId.Value, out var n) ? n : null, items = items.Where(i => i.VentaId == v.VentaId) });
        }

        /// <summary>Completa un apartado (paga el saldo) o lo anula devolviendo el stock.</summary>
        public async Task<BoutiqueVenta?> CerrarApartadoAsync(int empresaId, int ventaId, bool pagar, string? medioPago)
        {
            var v = await _db.BoutiqueVentas.FirstOrDefaultAsync(x => x.VentaId == ventaId && x.EmpresaId == empresaId);
            if (v == null) return null;
            if (v.Estado != "Apartada") throw new InvalidOperationException("Esa venta no es un apartado vigente");
            if (pagar)
            {
                v.Estado = "Pagada";
                v.Pagado = v.Total;
                v.MedioPago = Corto(medioPago, 20) ?? v.MedioPago;
            }
            else
            {
                v.Estado = "Anulada";
                var items = await _db.BoutiqueVentaItems.Where(i => i.VentaId == v.VentaId).ToListAsync();
                var vids = items.Select(i => i.VarianteId).ToList();
                var variantes = await _db.BoutiqueVariantes.Where(x => vids.Contains(x.VarianteId)).ToListAsync();
                foreach (var it in items) { var va = variantes.First(x => x.VarianteId == it.VarianteId); va.Stock += it.Cantidad; }
            }
            await _db.SaveChangesAsync();
            return v;
        }

        // ───────────── Alquileres ─────────────

        /// <summary>Unidades libres de una variante entre dos fechas (stock menos alquileres que se superponen).</summary>
        public async Task<int> LibresAsync(int varianteId, DateTime desde, DateTime hasta, int? excluirAlquiler = null, int? empresaId = null)
        {
            var v = await _db.BoutiqueVariantes.AsNoTracking().FirstOrDefaultAsync(x => x.VarianteId == varianteId && (empresaId == null || x.EmpresaId == empresaId));
            if (v == null) return 0;
            var ocupadas = await _db.BoutiqueAlquileres.CountAsync(a => a.VarianteId == varianteId && a.AlquilerId != (excluirAlquiler ?? 0)
                && (a.Estado == "Reservado" || a.Estado == "Retirado") && a.Desde <= hasta && a.Hasta >= desde);
            return Math.Max(0, v.Stock - ocupadas);
        }

        /// <summary>Cuántas piezas de la variante van a estar alquiladas al mismo tiempo, como máximo, de hoy en adelante.</summary>
        private async Task<int> MaxAlquiladasALaVezAsync(int varianteId)
        {
            var hoy = HoyPy;
            var activos = await _db.BoutiqueAlquileres.AsNoTracking()
                .Where(a => a.VarianteId == varianteId && (a.Estado == "Reservado" || a.Estado == "Retirado") && a.Hasta >= hoy)
                .Select(a => new { a.Desde, a.Hasta }).ToListAsync();
            return activos.Count == 0 ? 0 : activos.Max(a => activos.Count(b => b.Desde <= a.Hasta && b.Hasta >= a.Desde && b.Desde <= (a.Desde < hoy ? hoy : a.Desde) && b.Hasta >= (a.Desde < hoy ? hoy : a.Desde)));
        }

        public async Task<BoutiqueAlquiler> AlquilarAsync(int empresaId, AlquilerRequest r)
        {
            var v = await _db.BoutiqueVariantes.FirstOrDefaultAsync(x => x.VarianteId == r.VarianteId && x.EmpresaId == empresaId && x.Activo) ?? throw new KeyNotFoundException();
            var p = await _db.BoutiquePrendas.FirstAsync(x => x.PrendaId == v.PrendaId);
            if (p.Modalidad == "Venta") throw new InvalidOperationException("Esa prenda no está para alquiler");
            if (string.IsNullOrWhiteSpace(r.ClientaNombre)) throw new InvalidOperationException("Cargá el nombre de la clienta");
            var desde = r.Desde.Date;
            var hasta = (r.Hasta ?? desde.AddDays(p.DiasAlquiler - 1)).Date;
            if (hasta < desde) throw new InvalidOperationException("La fecha de devolución es anterior al retiro");
            if (desde < HoyPy) throw new InvalidOperationException("La fecha de retiro ya pasó");
            if (await LibresAsync(v.VarianteId, desde, hasta) <= 0) throw new InvalidOperationException($"{p.Nombre} talle {v.Talle} {v.Color} no está libre en esas fechas");
            var clienta = await ClientaPorWhatsappAsync(empresaId, r.ClientaNombre, r.ClientaWhatsapp, "Caja");
            await _db.SaveChangesAsync();
            var a = new BoutiqueAlquiler
            {
                EmpresaId = empresaId, VarianteId = v.VarianteId, ClientaId = clienta?.ClientaId, ClientaNombre = Corto(r.ClientaNombre, 150)!,
                ClientaWhatsapp = Corto(r.ClientaWhatsapp, 40), Desde = desde, Hasta = hasta, Estado = "Reservado",
                Precio = p.PrecioAlquiler, Sena = Math.Max(0, Math.Round(r.Sena ?? 0)), Garantia = p.Garantia, Notas = Corto(r.Notas, 300), Fecha = DateTime.UtcNow
            };
            _db.BoutiqueAlquileres.Add(a);
            await _db.SaveChangesAsync();
            return a;
        }

        public async Task<object> AlquileresAsync(int empresaId)
        {
            var desde = HoyPy.AddDays(-30);
            var lista = await _db.BoutiqueAlquileres.AsNoTracking().Where(a => a.EmpresaId == empresaId && (a.Hasta >= desde || a.Estado == "Retirado")).OrderBy(a => a.Desde).Take(500).ToListAsync();
            var vids = lista.Select(a => a.VarianteId).Distinct().ToList();
            var variantes = await _db.BoutiqueVariantes.AsNoTracking().Where(v => vids.Contains(v.VarianteId)).ToListAsync();
            var pids = variantes.Select(v => v.PrendaId).Distinct().ToList();
            var prendas = await _db.BoutiquePrendas.AsNoTracking().Where(p => pids.Contains(p.PrendaId)).ToDictionaryAsync(p => p.PrendaId);
            return lista.Select(a =>
            {
                var v = variantes.First(x => x.VarianteId == a.VarianteId);
                var p = prendas[v.PrendaId];
                return new { alquiler = a, prenda = p.Nombre, prendaId = p.PrendaId, talle = v.Talle, color = v.Color, colorHex = v.ColorHex ?? p.ColorHex };
            });
        }

        public async Task<BoutiqueAlquiler?> EstadoAlquilerAsync(int empresaId, int alquilerId, string estado)
        {
            if (estado is not ("Retirado" or "Devuelto" or "Cancelado")) throw new InvalidOperationException("Estado inválido");
            var a = await _db.BoutiqueAlquileres.FirstOrDefaultAsync(x => x.AlquilerId == alquilerId && x.EmpresaId == empresaId);
            if (a == null) return null;
            a.Estado = estado;
            await _db.SaveChangesAsync();
            return a;
        }

        // ───────────── Pruebas ─────────────

        public async Task<object> PruebasAsync(int empresaId)
        {
            var desde = HoyPy.AddDays(-15);
            var lista = await _db.BoutiquePruebas.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.FechaHora >= desde).OrderBy(p => p.FechaHora).Take(300).ToListAsync();
            var pids = lista.Select(p => p.PrendaId).Distinct().ToList();
            var prendas = await _db.BoutiquePrendas.AsNoTracking().Where(p => pids.Contains(p.PrendaId)).ToDictionaryAsync(p => p.PrendaId, p => p.Nombre);
            var vids = lista.Where(p => p.VarianteId.HasValue).Select(p => p.VarianteId!.Value).Distinct().ToList();
            var variantes = await _db.BoutiqueVariantes.AsNoTracking().Where(v => vids.Contains(v.VarianteId)).ToDictionaryAsync(v => v.VarianteId, v => $"{v.Talle} · {v.Color}");
            return lista.Select(p => new
            {
                prueba = p,
                prenda = prendas.GetValueOrDefault(p.PrendaId),
                variante = p.VarianteId.HasValue ? variantes.GetValueOrDefault(p.VarianteId.Value) : null
            });
        }

        public async Task<BoutiquePrueba?> EstadoPruebaAsync(int empresaId, int pruebaId, string estado)
        {
            if (estado is not ("Confirmada" or "Vino" or "NoVino" or "Cancelada")) throw new InvalidOperationException("Estado inválido");
            var p = await _db.BoutiquePruebas.FirstOrDefaultAsync(x => x.PruebaId == pruebaId && x.EmpresaId == empresaId);
            if (p == null) return null;
            p.Estado = estado;
            await _db.SaveChangesAsync();
            return p;
        }

        // ───────────── Panel ─────────────

        public async Task<object> PanelAsync(int empresaId)
        {
            var hoy = HoyPy;
            var inicioHoyUtc = hoy.AddHours(3);
            var inicioMesUtc = new DateTime(hoy.Year, hoy.Month, 1).AddHours(3);
            var ventasMes = await _db.BoutiqueVentas.AsNoTracking().Where(v => v.EmpresaId == empresaId && v.Fecha >= inicioMesUtc && v.Estado != "Anulada").Select(v => new { v.Fecha, v.Total, v.Pagado, v.Estado, v.Canal }).ToListAsync();
            var apartados = await _db.BoutiqueVentas.AsNoTracking().Where(v => v.EmpresaId == empresaId && v.Estado == "Apartada").Select(v => new { v.VentaId, v.Total, v.Pagado, v.VenceApartado }).ToListAsync();
            var alquileres = await _db.BoutiqueAlquileres.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && ((a.Estado == "Reservado" && a.Desde <= hoy.AddDays(7)) || (a.Estado == "Retirado" && a.Hasta <= hoy.AddDays(7))))
                .CountAsync();
            var pruebasPendientes = await _db.BoutiquePruebas.CountAsync(p => p.EmpresaId == empresaId && p.Estado == "Pedida");
            var pruebasProximas = await _db.BoutiquePruebas.CountAsync(p => p.EmpresaId == empresaId && (p.Estado == "Pedida" || p.Estado == "Confirmada") && p.FechaHora >= hoy && p.FechaHora < hoy.AddDays(7));
            var sinStock = await (from v in _db.BoutiqueVariantes
                                  join p in _db.BoutiquePrendas on v.PrendaId equals p.PrendaId
                                  where v.EmpresaId == empresaId && v.Activo && p.Activo && v.Stock <= 1
                                  orderby v.Stock, p.Nombre
                                  select new { p.PrendaId, p.Nombre, v.Talle, v.Color, v.Stock }).Take(8).ToListAsync();
            var vistas = await _db.BoutiquePrendas.Where(p => p.EmpresaId == empresaId && p.Activo).SumAsync(p => (int?)p.Vistas) ?? 0;
            var masVistas = await _db.BoutiquePrendas.AsNoTracking().Where(p => p.EmpresaId == empresaId && p.Activo).OrderByDescending(p => p.Vistas).Take(5)
                .Select(p => new { p.PrendaId, p.Nombre, p.Vistas, p.ColorHex }).ToListAsync();
            var hoyVentas = ventasMes.Where(v => v.Fecha >= inicioHoyUtc).ToList();
            return new
            {
                ventasHoy = hoyVentas.Sum(v => v.Pagado),
                cantidadHoy = hoyVentas.Count,
                ventasMes = ventasMes.Sum(v => v.Pagado),
                cantidadMes = ventasMes.Count,
                porCanal = ventasMes.GroupBy(v => v.Canal).Select(g => new { canal = g.Key, total = g.Sum(x => x.Pagado) }),
                apartados = apartados.Count,
                saldoApartados = apartados.Sum(a => a.Total - a.Pagado),
                apartadosVencidos = apartados.Count(a => a.VenceApartado < hoy),
                alquileresSemana = alquileres,
                pruebasPendientes,
                pruebasProximas,
                sinStock,
                vistasMarketplace = vistas,
                masVistas
            };
        }
    }
}
