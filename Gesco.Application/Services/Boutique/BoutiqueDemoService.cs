using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services.Boutique
{
    /// <summary>
    /// Datos de ejemplo para la demostración: la boutique de la clienta demo y otras boutiques de Gran Asunción
    /// (empresas "Closet Demo", rubro CLOSET_DEMO) para que el marketplace se vea lleno. Todo queda marcado como ejemplo.
    /// </summary>
    public class BoutiqueDemoService
    {
        private readonly IApplicationDbContext _db;
        public BoutiqueDemoService(IApplicationDbContext db) => _db = db;

        private record Prenda(string Nombre, string Cat, string Ocasiones, string Modo, decimal Venta, decimal Alquiler, decimal Garantia, string Material, string Desc, (string, string)[] Colores, string[] Talles, decimal? Antes = null);
        private record Tienda(string Nombre, string Ciudad, string Barrio, string Estilo, string Desc, string Instagram, string Horario, bool Verificada, Prenda[] Prendas);

        private static readonly (string, string) Rosa = ("Rosa empolvado", "#E8A7BD"), Lila = ("Lila", "#B79CE0"), Negro = ("Negro", "#1F1A24"),
            Esmeralda = ("Verde esmeralda", "#1F8A70"), Champagne = ("Champagne", "#E9D3B0"), Rojo = ("Rojo", "#C0283A"), Azul = ("Azul noche", "#1E2A55"),
            Fucsia = ("Fucsia", "#D63384"), Blanco = ("Blanco", "#F7F3EE"), Celeste = ("Celeste", "#9CC8E8"), Beige = ("Beige", "#D9C3A5"),
            Bordo = ("Bordó", "#6E1E33"), Dorado = ("Dorado", "#C9A24A"), Plateado = ("Plateado", "#BFC3CC"), Jean = ("Jean", "#4A6A99"),
            Terracota = ("Terracota", "#C46A4A"), Salvia = ("Verde salvia", "#9CB59A"), Lavanda = ("Lavanda", "#CDB8F0");

        private static readonly string[] SML = { "S", "M", "L" }, XSXL = { "XS", "S", "M", "L", "XL" }, Grandes = { "XL", "XXL", "48", "50" }, Calzado = { "35", "36", "37", "38", "39" }, Unico = { "Único" };

        private static readonly Tienda[] Tiendas =
        {
            new("Maison Lila", "Asunción", "Villa Morra", "Fiesta y gala",
                "Vestidos de fiesta para comprar o alquilar. Te asesoramos con el talle y te lo ajustamos en el día.", "maisonlila.py", "Lun a Sáb 9:00 a 19:30", true, new Prenda[]
            {
                new("Vestido Aurora de satén", "Vestidos", "Fiesta, Casamiento, Gala", "Ambos", 1_150_000, 320_000, 300_000, "Satén de seda con forro", "Corte sirena con espalda descubierta y tajo lateral. Cae perfecto y no se arruga.", new[] { Rosa, Esmeralda, Negro }, SML),
                new("Vestido Celeste brillo", "Vestidos", "15 años, Fiesta", "Alquiler", 0, 380_000, 400_000, "Tul bordado con lentejuelas", "Falda amplia de tul con corset bordado a mano. Ideal para la quinceañera o la madrina.", new[] { Celeste, Lila }, XSXL),
                new("Vestido Madrina Champagne", "Vestidos", "Casamiento, Madrina, Gala", "Ambos", 1_490_000, 420_000, 400_000, "Crepe con pedrería", "Manga larga con transparencia y pedrería en el escote. Elegante sin ser pesado.", new[] { Champagne, Azul }, SML),
                new("Vestido corto Coral", "Vestidos", "Cóctel, Civil, Graduación", "Venta", 590_000, 0, 0, "Crepe stretch", "Corte recto con hombro caído. Perfecto para civil o graduación.", new[] { Rojo, Fucsia, Negro }, XSXL, 720_000),
                new("Conjunto Palazzo Gala", "Conjuntos", "Gala, Fiesta", "Ambos", 890_000, 260_000, 250_000, "Crepe pesado", "Top drapeado y pantalón palazzo de tiro alto: cómodo para bailar toda la noche.", new[] { Negro, Esmeralda }, SML),
                new("Clutch joya", "Carteras", "Fiesta, Casamiento, Gala", "Venta", 210_000, 0, 0, "Metal y cristales", "Cartera de mano rígida con cadena desmontable.", new[] { Dorado, Plateado }, Unico),
                new("Sandalia Stiletto", "Calzados", "Fiesta, Gala", "Venta", 340_000, 0, 0, "Cuero sintético", "Taco 9 cm con plantilla acolchada.", new[] { Dorado, Negro }, Calzado),
                new("Vestido Noche Bordó", "Vestidos", "Gala, Noche, Fiesta", "Alquiler", 0, 350_000, 350_000, "Terciopelo", "Escote corazón y falda con caída. El más pedido para fiestas de noche.", new[] { Bordo, Azul }, SML),
                new("Vestido Lavanda vaporoso", "Vestidos", "Casamiento, Madrina, Civil", "Ambos", 980_000, 290_000, 250_000, "Gasa plisada", "Escote en V y falda plisada en capas. Muy fresco para casamientos de día.", new[] { Lavanda, Celeste }, XSXL),
            }),
            new("Rosa Pétalo", "San Lorenzo", "Centro", "Casual y urbano",
                "Ropa de todos los días con onda: blusas, jeans y conjuntos que combinan con todo.", "rosapetalo.sl", "Lun a Sáb 8:30 a 19:00", true, new Prenda[]
            {
                new("Blusa lino Bruma", "Blusas", "Oficina, Casual", "Venta", 189_000, 0, 0, "Lino", "Blusa holgada con botones de nácar. Fresca para el calor.", new[] { Blanco, Celeste, Beige }, XSXL),
                new("Jean Mom tiro alto", "Pantalones", "Casual", "Venta", 259_000, 0, 0, "Denim rígido", "Tiro alto que estiliza, ruedo sin terminar.", new[] { Jean }, new[] { "36", "38", "40", "42" }),
                new("Conjunto Sastrero Salvia", "Conjuntos", "Oficina, Civil", "Venta", 520_000, 0, 0, "Gabardina liviana", "Blazer y pantalón recto. Se usan juntos o por separado.", new[] { Salvia, Beige, Negro }, SML, 640_000),
                new("Falda midi plisada", "Faldas", "Oficina, Casual", "Venta", 210_000, 0, 0, "Satén plisado", "Falda con cintura elástica: va con zapatillas o tacos.", new[] { Rosa, Champagne, Negro }, SML),
                new("Top corset Rosé", "Blusas", "Salida, Fiesta", "Venta", 175_000, 0, 0, "Algodón con ballenas", "Top estructurado para salir de noche.", new[] { Rosa, Negro }, XSXL),
                new("Tapado oversize", "Abrigos", "Casual, Oficina", "Venta", 480_000, 0, 0, "Paño de lana", "Para los días frescos de invierno.", new[] { Beige, Negro }, SML),
            }),
            new("Alma Curvy", "Luque", "Centro", "Talles grandes",
                "Moda real para cuerpos reales, del XL al 54. Vestidos de fiesta y ropa diaria.", "almacurvy.py", "Lun a Vie 9:00 a 18:30 · Sáb 9:00 a 13:00", true, new Prenda[]
            {
                new("Vestido Envolvente Fucsia", "Vestidos", "Fiesta, Casamiento, Civil", "Ambos", 620_000, 230_000, 200_000, "Jersey de seda", "Cruzado con lazo en la cintura: se adapta y marca la figura.", new[] { Fucsia, Azul, Esmeralda }, Grandes),
                new("Vestido Gala Imperio", "Vestidos", "Gala, Madrina", "Alquiler", 0, 330_000, 300_000, "Crepe con encaje", "Corte imperio con mangas de encaje. Cómodo y elegante.", new[] { Azul, Bordo }, Grandes),
                new("Blusa Terracota", "Blusas", "Oficina, Casual", "Venta", 165_000, 0, 0, "Viscosa", "Manga globo y escote en V.", new[] { Terracota, Blanco }, Grandes),
                new("Pantalón Palazzo", "Pantalones", "Oficina, Casual", "Venta", 230_000, 0, 0, "Lino viscosa", "Cintura elástica y bolsillos.", new[] { Negro, Beige }, Grandes),
            }),
            new("Lumière Novias & Fiesta", "Fernando de la Mora", "Zona Norte", "Novias y 15 años",
                "Vestidos de novia, de 15 y de civil. Alquiler con ajuste incluido y prueba con cita.", "lumiere.novias", "Mar a Sáb 10:00 a 19:00 (con cita)", false, new Prenda[]
            {
                new("Vestido Novia Encaje Chantilly", "Vestidos", "Novia, Casamiento", "Ambos", 4_900_000, 1_450_000, 1_000_000, "Encaje chantilly y tul", "Escote ilusión con espalda de botones forrados y cola de 1,5 m.", new[] { Blanco, Champagne }, SML),
                new("Vestido Civil Minimal", "Vestidos", "Civil, Novia", "Ambos", 1_350_000, 450_000, 400_000, "Crepe de seda", "Corte columna, sin adornos. Para la novia que quiere algo simple y perfecto.", new[] { Blanco }, XSXL),
                new("Vestido 15 Princesa", "Vestidos", "15 años", "Alquiler", 0, 690_000, 600_000, "Tul y pedrería", "Corset con pedrería y falda de tul de 6 capas.", new[] { Lila, Celeste, Rosa }, SML),
                new("Tocado de perlas", "Accesorios", "Novia, Civil", "Venta", 160_000, 0, 0, "Perlas y alambre dorado", "Vincha hecha a mano.", new[] { Dorado }, Unico),
            }),
            new("Atelier Mburucuyá", "Lambaré", "Barrio Valle Apu'a", "Diseño de autora",
                "Diseño paraguayo con ao po'i y ñandutí: piezas únicas hechas en el país.", "atelier.mburucuya", "Lun a Sáb 9:00 a 18:00", true, new Prenda[]
            {
                new("Vestido Ao Po'i bordado", "Vestidos", "Civil, Casual, Fiesta", "Venta", 780_000, 0, 0, "Ao po'i de Yataity", "Bordado a mano por artesanas de Guairá. Pieza única.", new[] { Blanco, Beige }, SML),
                new("Blusa Ñandutí", "Blusas", "Casual, Oficina", "Venta", 290_000, 0, 0, "Algodón con ñandutí", "Cuello de ñandutí tejido en Itauguá.", new[] { Blanco, Salvia }, SML),
                new("Vestido largo Mburucuyá", "Vestidos", "Fiesta, Casamiento, Madrina", "Ambos", 1_250_000, 360_000, 300_000, "Gasa estampada", "Estampado exclusivo de flor de mburucuyá.", new[] { Lila, Esmeralda }, SML),
            }),
        };

        /// <summary>Carga la demo: la boutique de esta empresa y las demás boutiques de ejemplo del marketplace.</summary>
        public async Task<int> CargarAsync(int empresaId)
        {
            if (await _db.BoutiquePrendas.AnyAsync(p => p.EmpresaId == empresaId && p.Activo))
                throw new InvalidOperationException("Esta boutique ya tiene prendas: la demo se carga solo en una boutique vacía");

            var prendas = await SembrarAsync(empresaId, Tiendas[0], true);
            await SembrarMovimientosAsync(empresaId);

            // Otras boutiques del marketplace (empresas de ejemplo creadas por el script SQL)
            var otras = await _db.Empresas.Where(e => e.Rubro == "CLOSET_DEMO").OrderBy(e => e.EmpresaId).Select(e => e.EmpresaId).ToListAsync();
            for (var i = 0; i < otras.Count && i + 1 < Tiendas.Length; i++)
                if (!await _db.BoutiquePrendas.AnyAsync(p => p.EmpresaId == otras[i]))
                    prendas += await SembrarAsync(otras[i], Tiendas[i + 1], false);
            return prendas;
        }

        private async Task<int> SembrarAsync(int empresaId, Tienda t, bool propia)
        {
            var perfil = await _db.BoutiquePerfiles.FirstOrDefaultAsync(p => p.EmpresaId == empresaId);
            if (perfil == null)
            {
                var slug = BoutiqueService.Slug(t.Nombre);
                if (await _db.BoutiquePerfiles.AnyAsync(p => p.Slug == slug)) slug = $"{slug}-{empresaId}";
                perfil = new BoutiquePerfil { EmpresaId = empresaId, Slug = slug, FechaAlta = DateTime.UtcNow };
                _db.BoutiquePerfiles.Add(perfil);
            }
            perfil.NombreComercial = t.Nombre;
            perfil.Ciudad = t.Ciudad;
            perfil.Barrio = t.Barrio;
            perfil.Estilo = t.Estilo;
            perfil.Descripcion = t.Desc;
            perfil.Instagram = t.Instagram;
            perfil.Horario = t.Horario;
            perfil.Direccion ??= $"{t.Barrio}, {t.Ciudad}";
            perfil.Whatsapp ??= "0981 000 000";
            perfil.Verificada = t.Verificada;
            perfil.Publicada = true;
            perfil.Demo = true;
            await _db.SaveChangesAsync();

            var rnd = new Random(empresaId * 31 + t.Nombre.Length);
            var n = 0;
            foreach (var d in t.Prendas)
            {
                var p = new BoutiquePrenda
                {
                    EmpresaId = empresaId, Nombre = d.Nombre, Categoria = d.Cat, Ocasiones = d.Ocasiones, Modalidad = d.Modo,
                    PrecioVenta = d.Venta, PrecioAlquiler = d.Alquiler, DiasAlquiler = 4, Garantia = d.Garantia, PrecioAnterior = d.Antes,
                    Material = d.Material, Descripcion = d.Desc, ColorHex = d.Colores[0].Item2, Publicada = true, Activo = true,
                    Vistas = rnd.Next(12, 380), FechaAlta = DateTime.UtcNow.AddDays(-rnd.Next(0, 40)).AddMinutes(-n)
                };
                _db.BoutiquePrendas.Add(p);
                await _db.SaveChangesAsync();
                var medidaBase = 80;
                foreach (var (color, hex) in d.Colores)
                {
                    var paso = 0;
                    foreach (var talle in d.Talles)
                    {
                        var medidas = d.Cat is "Vestidos" or "Blusas" or "Conjuntos" or "Abrigos";
                        _db.BoutiqueVariantes.Add(new BoutiqueVariante
                        {
                            EmpresaId = empresaId, PrendaId = p.PrendaId, Talle = talle, Color = color, ColorHex = hex,
                            // Alquiler: una pieza por talle (como en la realidad); venta: algunas unidades
                            Stock = d.Modo == "Alquiler" ? 1 : rnd.Next(0, 5) == 0 ? 1 : rnd.Next(2, 6),
                            BustoCm = medidas ? medidaBase + paso * 4 : null,
                            CinturaCm = medidas ? medidaBase - 16 + paso * 4 : null,
                            CaderaCm = medidas ? medidaBase + 10 + paso * 4 : null,
                            LargoCm = d.Cat == "Vestidos" ? (d.Ocasiones.Contains("Gala") || d.Ocasiones.Contains("Novia") || d.Ocasiones.Contains("15") ? 150 : 95) : null,
                        });
                        paso++;
                    }
                }
                n++;
            }
            await _db.SaveChangesAsync();
            return n;
        }

        /// <summary>Ventas, apartados, alquileres, pruebas y clientas de ejemplo para que el panel se vea vivo.</summary>
        private async Task SembrarMovimientosAsync(int empresaId)
        {
            var hoy = DateTime.UtcNow.AddHours(-3).Date;
            var clientas = new[]
            {
                ("Camila Benítez", "0981 234 567", "S"), ("Fernanda Giménez", "0982 345 678", "M"), ("Lucía Ortiz", "0971 456 789", "M"),
                ("Valeria Duarte", "0961 567 890", "L"), ("Sofía Rojas", "0983 678 901", "S"), ("Paola Acosta", "0991 789 012", "XL"),
            };
            var lista = clientas.Select((c, i) => new BoutiqueClienta
            {
                EmpresaId = empresaId, Nombre = c.Item1, Whatsapp = c.Item2, Talle = c.Item3, Origen = i % 3 == 0 ? "Marketplace" : "Caja",
                Cumpleanos = new DateTime(1995 + i, ((hoy.Month + i - 1) % 12) + 1, Math.Min(28, 3 + i * 4)), FechaAlta = DateTime.UtcNow.AddDays(-30 + i)
            }).ToList();
            _db.BoutiqueClientas.AddRange(lista);
            await _db.SaveChangesAsync();

            var variantes = await (from v in _db.BoutiqueVariantes
                                   join p in _db.BoutiquePrendas on v.PrendaId equals p.PrendaId
                                   where v.EmpresaId == empresaId
                                   select new { v, p }).ToListAsync();
            var venta = variantes.Where(x => x.p.Modalidad != "Alquiler" && x.v.Stock >= 2).ToList();
            var alquiler = variantes.Where(x => x.p.Modalidad != "Venta").ToList();
            var rnd = new Random(empresaId);
            string[] medios = { "Efectivo", "Transferencia", "Tarjeta", "QR" }, canales = { "Tienda", "Tienda", "WhatsApp", "Instagram", "Marketplace" };

            // Ventas del mes (y algunas de hoy)
            for (var i = 0; i < 16 && venta.Count > 0; i++)
            {
                var x = venta[rnd.Next(venta.Count)];
                if (x.v.Stock < 1) continue;
                x.v.Stock--;
                var dia = i < 3 ? 0 : rnd.Next(1, Math.Max(2, hoy.Day));
                var v = new BoutiqueVenta
                {
                    EmpresaId = empresaId, ClientaId = lista[rnd.Next(lista.Count)].ClientaId, Estado = "Pagada", Total = x.p.PrecioVenta, Pagado = x.p.PrecioVenta,
                    MedioPago = medios[rnd.Next(medios.Length)], Canal = canales[rnd.Next(canales.Length)], Vendedora = "Demo",
                    Fecha = i < 3 ? DateTime.UtcNow.AddMinutes(-(25 + i * 50)) : hoy.AddDays(-dia).AddHours(3 + 10 + rnd.Next(0, 8))
                };
                _db.BoutiqueVentas.Add(v);
                await _db.SaveChangesAsync();
                _db.BoutiqueVentaItems.Add(new BoutiqueVentaItem { VentaId = v.VentaId, VarianteId = x.v.VarianteId, Cantidad = 1, PrecioUnitario = x.p.PrecioVenta, Descripcion = $"{x.p.Nombre} · {x.v.Talle} · {x.v.Color}" });
            }
            // Dos apartados con seña, uno vencido
            for (var i = 0; i < 2 && venta.Count > 0; i++)
            {
                var x = venta[rnd.Next(venta.Count)];
                if (x.v.Stock < 1) continue;
                x.v.Stock--;
                var v = new BoutiqueVenta
                {
                    EmpresaId = empresaId, ClientaId = lista[i + 2].ClientaId, Estado = "Apartada", Total = x.p.PrecioVenta, Pagado = Math.Round(x.p.PrecioVenta * 0.3m / 1000) * 1000,
                    MedioPago = "Transferencia", Canal = "WhatsApp", Vendedora = "Demo", VenceApartado = hoy.AddDays(i == 0 ? 2 : -1), Fecha = DateTime.UtcNow.AddDays(-2 - i)
                };
                _db.BoutiqueVentas.Add(v);
                await _db.SaveChangesAsync();
                _db.BoutiqueVentaItems.Add(new BoutiqueVentaItem { VentaId = v.VentaId, VarianteId = x.v.VarianteId, Cantidad = 1, PrecioUnitario = x.p.PrecioVenta, Descripcion = $"{x.p.Nombre} · {x.v.Talle} · {x.v.Color}" });
            }
            // Alquileres: uno retirado que vuelve pronto, otros reservados para eventos próximos
            var usados = new HashSet<int>();
            int[] inicios = { -2, 3, 5, 9, 12 };
            for (var i = 0; i < inicios.Length && alquiler.Count > 0; i++)
            {
                var x = alquiler[rnd.Next(alquiler.Count)];
                if (!usados.Add(x.v.VarianteId)) continue;
                var c = lista[i % lista.Count];
                _db.BoutiqueAlquileres.Add(new BoutiqueAlquiler
                {
                    EmpresaId = empresaId, VarianteId = x.v.VarianteId, ClientaId = c.ClientaId, ClientaNombre = c.Nombre, ClientaWhatsapp = c.Whatsapp,
                    Desde = hoy.AddDays(inicios[i]), Hasta = hoy.AddDays(inicios[i] + 3), Estado = inicios[i] < 0 ? "Retirado" : "Reservado",
                    Precio = x.p.PrecioAlquiler, Sena = Math.Round(x.p.PrecioAlquiler / 2 / 1000) * 1000, Garantia = x.p.Garantia, Fecha = DateTime.UtcNow.AddDays(-5)
                });
            }
            // Pruebas pedidas desde el marketplace
            var paraProbar = variantes.Where(x => x.p.Categoria == "Vestidos").Take(6).ToList();
            (string, string, int, int, string)[] pruebas =
            {
                ("Andrea Villalba", "0985111222", 1, 10, "Pedida"), ("Romina Cabrera", "0976333444", 1, 16, "Pedida"),
                ("Belén Martínez", "0984555666", 2, 11, "Confirmada"), ("Natalia Espínola", "0972777888", 3, 17, "Pedida"),
            };
            for (var i = 0; i < pruebas.Length && i < paraProbar.Count; i++)
            {
                var (nom, wa, dias, hora, estado) = pruebas[i];
                _db.BoutiquePruebas.Add(new BoutiquePrueba
                {
                    EmpresaId = empresaId, PrendaId = paraProbar[i].p.PrendaId, VarianteId = paraProbar[i].v.VarianteId, Nombre = nom, Whatsapp = wa,
                    FechaHora = hoy.AddDays(dias).AddHours(hora), FechaEvento = hoy.AddDays(dias + 10), Estado = estado,
                    Mensaje = i == 0 ? "Es para el casamiento de mi hermana, ¿me lo pueden ajustar?" : null, Fecha = DateTime.UtcNow.AddHours(-i * 5)
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
