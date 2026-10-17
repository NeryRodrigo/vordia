using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gesco.Application.Services
{
    /// <summary>
    /// VORDIA HQ — Necesidades del Mercado.
    /// Busca pedidos públicos de sistemas y aplicaciones (Freelancer.com, DNCP Paraguay, feeds RSS
    /// como Google Alerts), los clasifica contra los productos Vordia, les da un puntaje de
    /// oportunidad y detecta pedidos repetidos que conviene empaquetar como producto rápido.
    /// </summary>
    public class MercadoService : IMercadoService
    {
        private readonly IApplicationDbContext _db;
        private readonly HttpClient _http;
        private readonly ICrmService _crm;

        public static readonly string[] Estados = { "Nueva", "Evaluando", "Contactada", "Propuesta", "Ganada", "Descartada" };
        private static readonly string[] EstadosGestion = { "Evaluando", "Contactada", "Propuesta" };

        public MercadoService(IApplicationDbContext db, HttpClient http, ICrmService crm)
        {
            _db = db;
            _http = http;
            _crm = crm;
        }

        // ───────────────────────── Clasificación ─────────────────────────

        private sealed record Regla(string Categoria, string Vertical, string Producto, string Kit, string[] Claves);

        /// <summary>Orden = prioridad: la primera regla que coincide define la categoría.</summary>
        private static readonly Regla[] Reglas =
        {
            new("Reservas y hotelería", "STAY", "VORDIA STAY — reservas, check-in y cobros",
                "Motor de reservas web + calendario de ocupación + cobro de seña. Implementación en 5 días.",
                new[] { "hotel", "hostal", "hostel", "posada", "alojamiento", "airbnb", "cabana", "habitaciones", "check in", "booking" }),
            new("Gastronomía y pedidos", "DINE", "VORDIA DINE — comandas, delivery y caja",
                "Menú QR + toma de pedidos/delivery por WhatsApp + caja diaria. Implementación en 3 días.",
                new[] { "restaurante", "restaurant", "pizzeria", "cafeteria", "hamburgues", "delivery", "comanda", "menu digital", "carta digital", "gastronom", "food truck", "panaderia" }),
            new("Inmobiliaria y condominios", "HABITAT", "VORDIA HABITAT — condominios, alquileres y cobranzas",
                "Portal de expensas y alquileres con recibos y recordatorios de pago. Implementación en 7 días.",
                new[] { "condominio", "edificio", "consorcio", "inmobiliaria", "alquiler", "alquileres", "propiedades", "expensas", "inquilino", "bienes raices", "real estate" }),
            new("Turnos y agenda", "A_MEDIDA", "Kit Vordia Turnos (base HQ)",
                "Agenda online con recordatorio por WhatsApp y cobro anticipado para clínicas, estéticas y talleres. Implementación en 3 días.",
                new[] { "turnos", "agenda", "citas", "clinica", "consultorio", "odontolog", "peluqueria", "barberia", "gimnasio", "spa", "estetica", "veterinaria", "taller mecanico", "reserva de turno" }),
            new("Ganadería y agro", "AGRO", "VORDIA AGRO — ciclo ganadero, sanidad y engorde",
                "Ficha por animal con pesajes, vacunas y GPD desde el celular. Implementación en 7 días.",
                new[] { "ganad", "bovino", "hacienda", "feedlot", "engorde", "tambo", "estancia", "agricola", "agro", "cultivo", "cosecha", "silo", "lechero" }),
            new("Moda e indumentaria", "BOUTIQUE", "VORDIA BOUTIQUE — talles, colores y ventas",
                "Catálogo con talles/colores, stock por sucursal y tienda online. Implementación en 5 días.",
                new[] { "boutique", "indumentaria", "ropa", "moda", "calzado", "textil" }),
            new("Punto de venta e inventario", "RETAIL", "VORDIA RETAIL — punto de venta, stock y facturación",
                "POS con stock, facturación y reporte diario por WhatsApp. Implementación en 5 días.",
                new[] { "punto de venta", "pos ", "inventario", "stock", "facturacion", "factura", "caja registradora", "tienda", "comercio", "almacen", "libreria", "ferreteria", "farmacia", "supermercado", "minimarket", "kiosco", "distribuidora" }),
            new("Logística y depósitos", "OPS", "VORDIA OPS — depósitos, flota y despachos",
                "Control de depósito con picking, rutas y entregas firmadas desde el celular. Implementación en 7 días.",
                new[] { "logistica", "deposito", "warehouse", "flota", "transporte", "envios", "rutas", "despacho", "distribucion", "courier", "repartidor" }),
            new("Gestión empresarial / CRM", "HQ", "VORDIA HQ — CRM, finanzas y operación",
                "CRM comercial + cobranzas + tablero de gestión listo para pymes. Implementación en 5 días.",
                new[] { "crm", "erp", "gestion empresarial", "sistema de gestion", "administracion", "contabilidad", "contable", "rrhh", "recursos humanos", "nomina", "planilla", "cobranza", "cotizacion", "presupuestos", "seguimiento de clientes", "ventas" }),
            new("Gestión educativa", "A_MEDIDA", "Kit Vordia Educa (a medida)",
                "Inscripciones, cuotas y notas para colegios y academias. Implementación en 7 días.",
                new[] { "colegio", "escuela", "academia", "alumnos", "cursos", "aula virtual", "lms", "instituto", "universidad", "capacitacion" }),
            new("Gestión documental", "A_MEDIDA", "Kit Vordia Documentos (base HQ)",
                "Digitalización y expediente digital con búsqueda y vencimientos. Implementación en 5 días.",
                new[] { "digitalizacion", "documental", "expediente", "archivo digital", "firma digital", "tramites" }),
            new("Sitio web / e-commerce", "A_MEDIDA", "Sitio Vordia + tienda online",
                "Landing o tienda online conectada a stock y WhatsApp. Implementación en 3 días.",
                new[] { "pagina web", "sitio web", "landing", "tienda online", "ecommerce", "e commerce", "wordpress", "shopify", "web corporativa" }),
            new("App móvil a medida", "A_MEDIDA", "App Vordia a medida",
                "App móvil sobre la plataforma Vordia (login, catálogo, pedidos, notificaciones). Implementación en 10 días.",
                new[] { "app ", "aplicacion", "android", "ios", "flutter", "react native", "movil", "mobile" }),
        };

        /// <summary>Claves en inglés y portugués para pedidos internacionales.</summary>
        private static readonly Dictionary<string, string[]> ClavesIntl = new()
        {
            ["Reservas y hotelería"] = new[] { "booking system", "reservation", "hospitality", "guesthouse", "pousada", "reserva" },
            ["Gastronomía y pedidos"] = new[] { "food ordering", "online ordering", "qr menu", "lanchonete", "pedidos online", "ifood" },
            ["Inmobiliaria y condominios"] = new[] { "property management", "tenant", "rental", "landlord", "imobiliaria", "condominio" },
            ["Turnos y agenda"] = new[] { "appointment", "scheduling", "clinic", "dental", "salon", "agendamento", "consultorio" },
            ["Ganadería y agro"] = new[] { "livestock", "cattle", "farm management", "agricultur", "pecuaria", "gado", "fazenda" },
            ["Moda e indumentaria"] = new[] { "clothing", "apparel", "fashion" },
            ["Punto de venta e inventario"] = new[] { "point of sale", "inventory", "invoicing", "billing system", "retail", "estoque", "ponto de venda", "nota fiscal" },
            ["Logística y depósitos"] = new[] { "logistics", "fleet management", "delivery tracking", "wms ", "transportadora" },
            ["Gestión empresarial / CRM"] = new[] { "accounting", "payroll", "hr system", "business management", "gestao empresarial", "financeiro" },
            ["Gestión educativa"] = new[] { "school management", "learning management", "students", "e learning", "escola" },
            ["Gestión documental"] = new[] { "document management", "digitization", "digitalization", "records management", "e government" },
            ["Sitio web / e-commerce"] = new[] { "website", "online store", "woocommerce", "loja virtual" },
            ["App móvil a medida"] = new[] { "mobile app", "aplicativo" },
        };

        /// <summary>
        /// Precio de referencia de la microapp (USD): implementación única + mensualidad SaaS.
        /// Se ajusta la implementación con el presupuesto mediano que el mercado está ofreciendo.
        /// </summary>
        private static readonly Dictionary<string, (decimal setup, decimal mensual)> PreciosMicroapp = new()
        {
            ["Reservas y hotelería"] = (390, 39),
            ["Gastronomía y pedidos"] = (290, 29),
            ["Inmobiliaria y condominios"] = (490, 49),
            ["Turnos y agenda"] = (190, 19),
            ["Ganadería y agro"] = (490, 49),
            ["Moda e indumentaria"] = (290, 29),
            ["Punto de venta e inventario"] = (290, 29),
            ["Logística y depósitos"] = (590, 59),
            ["Gestión empresarial / CRM"] = (490, 39),
            ["Gestión educativa"] = (390, 29),
            ["Gestión documental"] = (490, 39),
            ["Sitio web / e-commerce"] = (190, 15),
            ["App móvil a medida"] = (990, 49),
            ["Software a medida"] = (1500, 99),
        };

        /// <summary>Señales de confianza que marca el vendedor al cargar un pedido a mano.</summary>
        public static readonly Dictionary<string, (string Etiqueta, int Peso)> SenalesManual = new()
        {
            ["empresa_formal"] = ("empresa formal / RUC", 15),
            ["negocio_identificado"] = ("nombra su negocio", 12),
            ["presupuesto_urgencia"] = ("menciona presupuesto o urgencia", 12),
            ["presencia_web"] = ("tiene página, local o web", 10),
            ["decisor"] = ("habla quien decide", 12),
            ["perfil_dudoso"] = ("perfil nuevo o sin actividad", -25),
            ["sin_pago"] = ("pide trabajar antes de pagar", -30),
            ["solo_barato"] = ("busca gratis o lo más barato", -15),
        };

        private static readonly HashSet<string> PaisesLatam = new(StringComparer.OrdinalIgnoreCase)
        {
            "Argentina", "Bolivia", "Brasil", "Brazil", "Chile", "Colombia", "Costa Rica", "Cuba", "Ecuador", "El Salvador",
            "Guatemala", "Honduras", "Mexico", "México", "Nicaragua", "Panama", "Panamá", "Peru", "Perú", "Uruguay", "Venezuela", "Guyana", "Surinam", "Suriname", "Paraguay",
            "Republica Dominicana", "República Dominicana", "Dominican Republic", "Haiti", "Latin America", "Latin America and Caribbean"
        };

        private static readonly Regla SoftwareAMedida = new("Software a medida", "A_MEDIDA", "Desarrollo a medida sobre la plataforma Vordia",
            "Relevar el proceso y adaptar un módulo existente de Vordia en vez de desarrollar desde cero.", Array.Empty<string>());

        /// <summary>Pedidos que no son del negocio de Vordia (juegos, diseño, hardware, reventa de licencias).</summary>
        private static readonly string[] NoEncaja =
        {
            "juego", "game", "unity", "roblox", "minecraft", "logo", "branding", "identidad de marca", "diseno grafico",
            "video", "animacion", "traduccion", "plc", "scada", "electronica", "nft", "crypto", "trading", "tesis",
            "equipos informaticos", "adquisicion de equipos", "toner", "impresora", "cctv", "camaras de seguridad",
            "licencias de windows", "licencia antiplagio", "autocad", "microsoft office", "fibra optica", "mantenimiento de equipos",
            // avisos que buscan vendedores o servicios, no un sistema
            "vendedor", "captacion de clientes", "telemarketing", "closer", "setter", "community manager", "redes sociales",
            "reactivo", "laboratorio de", "asfalt", "pavimento", "mobiliario", "vehiculo",
            "asesor de ventas", "asesor comercial", "publicidad", "marketing", "adwords", "google ads", "meta ads", "seo ",
            "transcripcion", "pcb", "comercio exterior", "modelado 3d", "3d ", "personaje", "ilustra", "firewall",
            "equipo ", "equipos ", "hardware", "servidor", "accesorios", "componentes", "licencia", "licencias",
            // contratos de personas y obras (SECOP / Banco Mundial)
            "prestacion de servicios profesionales", "persona natural", "apoyo a la gestion", "individual consultant",
            "civil works", "construction", "supervision of works", "vehicles", "audit", "logo design", "video editing",
            "game development", "translation", "data entry", "voice over", "copywriting",
            "aquisicao de equipamentos", "material permanente", "computador", "notebook", "impressora", "outsourcing de impressao",
            "relogio", "publicacoes", "locacao de", "materiais de informatica", "toner", "manutencao de equipamentos",
            "commission", "sales ", "lead generation", "cold calling", "appointment setting", "svg", "illustrat", "photoshop",
            "canva", "drainage", "structural", "stamp", "civil engineer", "pe for", "bulk purchase", "manual testing", "ugc", "writer", "fiction", "wedding", "invitation", "virtual admin", "virtual assistant", "accountability"
        };

        private static readonly string[] TerminosSistema =
        {
            "sistema", "software", "plataforma", "aplicacion", "app ", "web", "erp", "crm", "desarrollo", "automatiz", "digitaliz", "base de datos", "dashboard",
            "system", "application", "platform", "website", "portal", "database", "saas", "aplicativo"
        };

        /// <summary>Filtro de licitaciones internacionales de TI (Banco Mundial, en inglés).</summary>
        private static readonly Regex RegexTiIntl = new(
            @" (software|information (system|management)\w*|management information|digital\w*|platform\w*|web\w*|portal|application\w*|app|erp|database|ict|it (system|solution)\w*|e government|cloud|systems?|solutions?|crm|data\w*|website|digitali\w*|sistema\w*|plataforma\w*) ",
            RegexOptions.Compiled);

        /// <summary>Filtro de llamados DNCP de TI (sobre texto normalizado: palabras separadas por un espacio).</summary>
        private static readonly Regex RegexTi = new(
            @" (software|informatic\w*|sistemas? (de gestion|informatico\w*|web)|aplicacion(es)? (web|movil|informatica)|plataformas? (digital|web|informatica|de comunicacion)\w*|sitio web|pagina web|portal web|desarrollo de (sistema|software|aplicacion|plataforma)\w*|digitaliz\w*|web|app|erp) ",
            RegexOptions.Compiled);

        private static readonly string[] SenalesParaguay = { "paraguay", "asuncion", "guarani", "ciudad del este", "encarnacion", "san lorenzo", "luque", "fernando de la mora", "chaco" };

        public static string Normalizar(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var norm = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var ch in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            // Solo letras y números: así las claves se comparan por palabra completa o inicio de palabra
            return " " + Regex.Replace(sb.ToString(), @"[^a-z0-9]+", " ").Trim() + " ";
        }

        /// <summary>
        /// La clave se compara contra el inicio de una palabra ("ganad" → ganado, ganadería).
        /// Si termina en espacio exige la palabra completa ("app " no coincide con "apple").
        /// </summary>
        private static bool Coincide(string texto, string clave) => texto.Contains(" " + clave);

        /// <summary>Gana la categoría con más coincidencias; ante empate, la de mayor prioridad.</summary>
        private static Regla Clasificar(string texto)
        {
            Regla? mejor = null;
            var max = 0;
            foreach (var r in Reglas)
            {
                var hits = r.Claves.Count(k => Coincide(texto, k))
                           + (ClavesIntl.TryGetValue(r.Categoria, out var intl) ? intl.Count(k => Coincide(texto, k)) : 0);
                if (hits > max) { max = hits; mejor = r; }
            }
            return mejor ?? SoftwareAMedida;
        }

        /// <summary>Puntaje 0-100: encaje con Vordia, cercanía, presupuesto, comprador y frescura.</summary>
        private static (int puntaje, string motivo) Puntuar(HqMercadoNecesidad n, string texto, Regla regla, DateTime ahora)
        {
            var p = 0;
            var motivos = new List<string>();

            var noEncaja = NoEncaja.Any(k => Coincide(texto, k));
            var pideSistema = TerminosSistema.Any(k => Coincide(texto, k));
            if (noEncaja) { p -= 40; motivos.Add("fuera del rubro Vordia"); }
            else if (regla.Vertical != "A_MEDIDA" && pideSistema) { p += 40; motivos.Add($"pide un sistema que cubre VORDIA {regla.Vertical}"); }
            else if (regla.Vertical != "A_MEDIDA") { p += 15; motivos.Add($"rubro VORDIA {regla.Vertical}, sin pedir sistema"); }
            else if (pideSistema) { p += 25; motivos.Add("pide un sistema/app"); }
            else p += 5;

            if (n.Fuente == "DNCP" || n.Pais == "Paraguay" || SenalesParaguay.Any(k => Coincide(texto, k))) { p += 20; motivos.Add("Paraguay"); }
            else if (n.Alcance == "LatAm") { p += 10; motivos.Add(n.Pais ?? "Latinoamérica"); }
            else { p += 3; motivos.Add(n.Pais != null ? $"internacional ({n.Pais})" : "internacional"); }

            if (EsInstitucional(n.Fuente)) { p += 15; motivos.Add("comprador institucional"); }
            else if (n.Confianza >= 70) { p += 10; motivos.Add("comprador confiable"); }
            else if (n.Confianza is > 0 and < 40) { p -= 10; motivos.Add("comprador poco confiable"); }

            if (n.Competidores > 50) { p -= 10; motivos.Add($"muy competido ({n.Competidores} propuestas)"); }
            else if (n.Competidores is >= 0 and <= 10) { p += 5; motivos.Add($"poca competencia ({n.Competidores} propuestas)"); }

            var usd = n.Moneda == "USD" ? n.PresupuestoMax : null;
            if (usd >= 1500) { p += 15; motivos.Add($"presupuesto hasta USD {usd:N0}"); }
            else if (usd >= 500) { p += 10; motivos.Add($"presupuesto hasta USD {usd:N0}"); }
            else if (usd >= 150) p += 5;
            else if (usd is > 0 and < 50) { p -= 5; motivos.Add("presupuesto muy bajo"); }

            if (n.FechaPublicacion.HasValue)
            {
                var dias = (ahora - n.FechaPublicacion.Value).TotalDays;
                if (dias <= 2) { p += 10; motivos.Add("publicado hace menos de 48 h"); }
                else if (dias <= 7) p += 5;
            }

            if (n.FechaCierre.HasValue && n.FechaCierre.Value > ahora && (n.FechaCierre.Value - ahora).TotalDays <= 7)
                motivos.Add($"cierra el {n.FechaCierre.Value:dd/MM}");

            return (Math.Clamp(p, 0, 100), string.Join(" · ", motivos));
        }

        private static bool EsInstitucional(string fuente) =>
            fuente is "DNCP" or "SECOP" or "BancoMundial" or "PNCP" or "ContractsFinder" or "SEACE" or "ARCE" or "PNUD" or "MercadoPublico";

        private static void Enriquecer(HqMercadoNecesidad n, DateTime ahora)
        {
            // Alcance: Paraguay / LatAm / Internacional
            if (n.Pais == "Paraguay") n.Alcance = "Paraguay";
            else if (n.Pais != null && PaisesLatam.Contains(n.Pais)) n.Alcance = "LatAm";
            else n.Alcance ??= "Internacional";

            // Confianza de compradores institucionales y feeds sin verificar (Freelancer y Manual se calculan al capturar)
            if (EsInstitucional(n.Fuente))
            {
                n.Confianza = 85;
                n.ConfianzaMotivo = n.Fuente switch
                {
                    "BancoMundial" => "Financiado por el Banco Mundial · pago contractual · requiere ofertar formalmente",
                    "PNUD" => "Naciones Unidas (PNUD) · pago contractual · se oferta por el portal Quantum del PNUD",
                    _ => "Comprador estatal · pago contractual · requiere estar inscripto como proveedor"
                };
            }
            else if (n.Fuente == "RSS" && n.Confianza == 0)
            {
                n.Confianza = 35;
                n.ConfianzaMotivo = "Sin verificar: revisá quién publica antes de invertir tiempo";
            }

            // El aviso oficial del Banco Mundial trae texto legal genérico: se evalúa por título y tipo de servicio
            var texto = n.Fuente == "BancoMundial"
                ? Normalizar($"{n.Titulo} {n.Habilidades}")
                : Normalizar($"{n.Titulo} {n.Descripcion} {n.Habilidades}");
            var regla = Clasificar(texto);
            n.Categoria = regla.Categoria;
            n.Vertical = regla.Vertical;
            n.ProductoSugerido = regla.Producto;
            var (puntaje, motivo) = Puntuar(n, texto, regla, ahora);
            n.Puntaje = puntaje;
            n.MotivoPuntaje = Recortar(motivo, 500);
        }

        // ───────────────────────── Sincronización ─────────────────────────

        public async Task<int> SincronizarTodasAsync(CancellationToken ct = default)
        {
            var empresas = await _db.HqMercadoFuentes.AsNoTracking().Where(f => f.Activa).Select(f => f.EmpresaId).Distinct().ToListAsync(ct);
            var nuevas = 0;
            foreach (var e in empresas) nuevas += (await SincronizarAsync(e, ct)).Nuevas;
            return nuevas;
        }

        public async Task<MercadoSyncResultDto> SincronizarAsync(int empresaId, CancellationToken ct = default)
        {
            var res = new MercadoSyncResultDto();
            var fuentes = await _db.HqMercadoFuentes.Where(f => f.EmpresaId == empresaId && f.Activa).ToListAsync(ct);
            var ahora = DateTime.UtcNow;

            foreach (var f in fuentes)
            {
                var item = new MercadoSyncFuenteDto { Nombre = f.Nombre, Tipo = f.Tipo };
                try
                {
                    var encontrados = f.Tipo switch
                    {
                        "Freelancer" => await BuscarFreelancerAsync(f, ct),
                        "DNCP" => await BuscarDncpAsync(f, ahora, ct),
                        "SECOP" => await BuscarSecopAsync(f, ahora, ct),
                        "BancoMundial" => await BuscarBancoMundialAsync(f, ahora, ct),
                        "PNCP" => await BuscarPncpAsync(f, ahora, ct),
                        "ContractsFinder" => await BuscarContractsFinderAsync(f, ahora, ct),
                        "SEACE" => await BuscarSeaceAsync(f, ahora, ct),
                        "ARCE" => await BuscarArceAsync(f, ahora, ct),
                        "PNUD" => await BuscarPnudAsync(f, ahora, ct),
                        "MercadoPublico" => await BuscarMercadoPublicoAsync(f, ahora, ct),
                        "RSS" => await BuscarRssAsync(f, ct),
                        _ => new List<HqMercadoNecesidad>()
                    };
                    var (nuevas, actualizadas) = await GuardarAsync(empresaId, encontrados, ahora, ct);
                    // Guardar por fuente: el mismo proyecto puede aparecer en varias búsquedas
                    await _db.SaveChangesAsync(ct);
                    item.Ok = true;
                    item.Revisadas = encontrados.Count;
                    item.Nuevas = nuevas;
                    item.Mensaje = $"{encontrados.Count} revisadas · {nuevas} nuevas";
                    res.Nuevas += nuevas;
                    res.Actualizadas += actualizadas;
                    res.Revisadas += encontrados.Count;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or System.Xml.XmlException or InvalidOperationException)
                {
                    item.Ok = false;
                    item.Mensaje = ex is TaskCanceledException ? "Sin respuesta (timeout)" : Recortar(ex.Message, 250);
                }
                f.UltimaSync = ahora;
                f.UltimoResultado = Recortar(item.Ok ? item.Mensaje : $"Error: {item.Mensaje}", 300);
                res.Fuentes.Add(item);
            }

            // Recalcular todo lo abierto: la frescura baja con los días y las reglas pueden haber cambiado
            var abiertas = await _db.HqMercadoNecesidades
                .Where(n => n.EmpresaId == empresaId && n.Activo && n.Estado != "Ganada" && n.Estado != "Descartada")
                .ToListAsync(ct);
            foreach (var n in abiertas) Enriquecer(n, ahora);

            await _db.SaveChangesAsync(ct);
            return res;
        }

        private async Task<(int nuevas, int actualizadas)> GuardarAsync(int empresaId, List<HqMercadoNecesidad> encontrados, DateTime ahora, CancellationToken ct)
        {
            if (encontrados.Count == 0) return (0, 0);
            var fuente = encontrados[0].Fuente;
            var ids = encontrados.Select(e => e.ExternoId).Distinct().ToList();
            var existentes = await _db.HqMercadoNecesidades
                .Where(n => n.EmpresaId == empresaId && n.Fuente == fuente && ids.Contains(n.ExternoId))
                .ToDictionaryAsync(n => n.ExternoId, ct);

            int nuevas = 0, actualizadas = 0;
            foreach (var e in encontrados.GroupBy(x => x.ExternoId).Select(g => g.First()))
            {
                if (existentes.TryGetValue(e.ExternoId, out var n))
                {
                    n.Titulo = e.Titulo;
                    n.Descripcion = e.Descripcion;
                    n.PresupuestoMin = e.PresupuestoMin;
                    n.PresupuestoMax = e.PresupuestoMax;
                    n.Moneda = e.Moneda;
                    n.FechaCierre = e.FechaCierre;
                    n.Habilidades = e.Habilidades;
                    n.Competidores = e.Competidores;
                    if (string.IsNullOrWhiteSpace(n.ContactoEmail) && string.IsNullOrWhiteSpace(n.ContactoTelefono)
                        && (!string.IsNullOrWhiteSpace(e.ContactoEmail) || !string.IsNullOrWhiteSpace(e.ContactoTelefono)))
                    {
                        n.ContactoNombre = e.ContactoNombre;
                        n.ContactoEmail = e.ContactoEmail;
                        n.ContactoTelefono = e.ContactoTelefono;
                        n.ContactoOrigen = e.ContactoOrigen;
                    }
                    n.Pais = e.Pais ?? n.Pais;
                    n.Alcance = e.Alcance ?? n.Alcance;
                    if (e.Confianza > 0) { n.Confianza = e.Confianza; n.ConfianzaMotivo = e.ConfianzaMotivo; }
                    Enriquecer(n, ahora);
                    actualizadas++;
                }
                else
                {
                    e.EmpresaId = empresaId;
                    e.FechaCaptura = ahora;
                    Enriquecer(e, ahora);
                    _db.HqMercadoNecesidades.Add(e);
                    nuevas++;
                }
            }
            return (nuevas, actualizadas);
        }

        /// <summary>Freelancer.com — API pública de proyectos activos (sin autenticación).</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarFreelancerAsync(HqMercadoFuente f, CancellationToken ct)
        {
            // Consulta "palabras|idioma": "software development|en", "sistema|pt". Sin idioma = español.
            var partes = (string.IsNullOrWhiteSpace(f.Consulta) ? "sistema" : f.Consulta.Trim()).Split('|', 2, StringSplitOptions.TrimEntries);
            var q = partes[0];
            var idioma = partes.Length > 1 && Regex.IsMatch(partes[1], "^[a-z]{2}$") ? partes[1] : "es";
            var url = "https://www.freelancer.com/api/projects/0.1/projects/active/" +
                      $"?query={Uri.EscapeDataString(q)}&limit=50&full_description=true&job_details=true&upgrade_details=true&languages[]={idioma}";
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
            var lista = new List<HqMercadoNecesidad>();
            if (!doc.RootElement.TryGetProperty("result", out var result) || !result.TryGetProperty("projects", out var projects))
                return lista;

            foreach (var p in projects.EnumerateArray())
            {
                var id = p.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);
                decimal? min = null, max = null;
                string? moneda = null;
                if (p.TryGetProperty("budget", out var b))
                {
                    min = Dec(b, "minimum");
                    max = Dec(b, "maximum");
                }
                if (p.TryGetProperty("currency", out var c))
                {
                    // Normalizamos a USD con el tipo de cambio que informa Freelancer
                    var rate = Dec(c, "exchange_rate") ?? 0;
                    if (rate > 0)
                    {
                        min = min.HasValue ? Math.Round(min.Value * rate, 0) : null;
                        max = max.HasValue ? Math.Round(max.Value * rate, 0) : null;
                        moneda = "USD";
                    }
                    else moneda = Str(c, "code");
                }
                var jobs = p.TryGetProperty("jobs", out var j) && j.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", j.EnumerateArray().Select(x => Str(x, "name")).Where(x => !string.IsNullOrEmpty(x)).Take(8))
                    : null;
                DateTime? publicado = p.TryGetProperty("time_submitted", out var ts) && ts.ValueKind == JsonValueKind.Number
                    ? DateTimeOffset.FromUnixTimeSeconds(ts.GetInt64()).UtcDateTime : null;
                var seo = Str(p, "seo_url");
                var descripcion = Str(p, "description") ?? "";
                var propuestas = p.TryGetProperty("bid_stats", out var bs) && bs.TryGetProperty("bid_count", out var bc) && bc.ValueKind == JsonValueKind.Number
                    ? bc.GetInt32() : (int?)null;

                // Confianza: Freelancer oculta al cliente en la API pública, así que medimos la seriedad del pedido
                var conf = 40;
                var porque = new List<string>();
                var prepago = p.TryGetProperty("upgrades", out var up) && up.TryGetProperty("active_prepaid_milestone", out var pm)
                              && pm.ValueKind == JsonValueKind.Object && Dec(pm, "amount") > 0;
                if (Bool(p, "is_escrow_project") || Bool(p, "requires_upfront_funding") || prepago) { conf += 25; porque.Add("pago protegido / fondos adelantados"); }
                if (Bool(p, "featured") || Bool(p, "urgent") || (up.ValueKind == JsonValueKind.Object && (Bool(up, "featured") || Bool(up, "urgent") || Bool(up, "sealed") || Bool(up, "NDA"))))
                { conf += 10; porque.Add("pagó para destacar el aviso"); }
                if (descripcion.Length >= 600) { conf += 10; porque.Add("pedido bien detallado"); }
                else if (descripcion.Length < 150) { conf -= 10; porque.Add("pedido muy vago"); }
                if (max >= 500 && moneda == "USD") { conf += 10; porque.Add("presupuesto serio"); }
                else if (max < 30 && moneda == "USD") { conf -= 15; porque.Add("presupuesto irrisorio"); }
                if (propuestas > 50) porque.Add($"{propuestas} propuestas: mucha competencia");

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "Freelancer",
                    Confianza = Math.Clamp(conf, 5, 95),
                    ConfianzaMotivo = Recortar(string.Join(" · ", porque.DefaultIfEmpty("pedido estándar, sin señales extra")), 300),
                    Competidores = propuestas,
                    Pais = idioma == "pt" ? "Brasil" : null,
                    Alcance = idioma is "es" or "pt" ? "LatAm" : "Internacional",
                    ExternoId = id,
                    Titulo = Recortar(Str(p, "title") ?? "(sin título)", 400),
                    Descripcion = Recortar(descripcion, 4000),
                    Url = seo != null ? $"https://www.freelancer.com/projects/{seo}" : null,
                    Solicitante = idioma == "es" ? "Cliente en Freelancer.com" : $"Cliente en Freelancer.com ({idioma})",
                    PresupuestoMin = min,
                    PresupuestoMax = max,
                    Moneda = moneda,
                    FechaPublicacion = publicado,
                    Habilidades = Recortar(jobs, 400)
                });
            }
            return lista;
        }

        /// <summary>DNCP Paraguay — datos abiertos OCDS (CC-BY 4.0). Solo llamados abiertos de TI.</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarDncpAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            // Ventana fija de 20 días: así los llamados abiertos se re-evalúan y completan su contacto
            var desde = ahora.AddDays(-20).ToString("yyyy-MM-dd");
            var conContacto = (await _db.HqMercadoNecesidades.AsNoTracking()
                .Where(n => n.EmpresaId == f.EmpresaId && n.Fuente == "DNCP" && (n.ContactoEmail != null || n.ContactoTelefono != null))
                .Select(n => n.ExternoId).ToListAsync(ct)).ToHashSet();
            var lista = new List<HqMercadoNecesidad>();
            for (var page = 1; page <= 15; page++)
            {
                var url = "https://www.contrataciones.gov.py/datos/api/v3/doc/search/processes" +
                          $"?fecha_desde={desde}&tipo_fecha=publicacion_llamado&items_per_page=100&page={page}";
                using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
                var root = doc.RootElement;
                if (!root.TryGetProperty("records", out var records)) break;

                foreach (var r in records.EnumerateArray())
                {
                    if (!r.TryGetProperty("compiledRelease", out var cr) || !cr.TryGetProperty("tender", out var t)) continue;
                    var titulo = Str(t, "title");
                    if (string.IsNullOrWhiteSpace(titulo) || !RegexTi.IsMatch(Normalizar(titulo))) continue;

                    var estado = Str(t, "statusDetails") ?? "";
                    DateTime? cierre = null;
                    if (t.TryGetProperty("tenderPeriod", out var tp) && DateTimeOffset.TryParse(Str(tp, "endDate"), out var end)) cierre = end.UtcDateTime;
                    if (estado.Contains("Cancel", StringComparison.OrdinalIgnoreCase) || (cierre.HasValue && cierre < ahora)) continue;

                    var ocid = Str(r, "ocid") ?? Str(cr, "ocid") ?? "";
                    var m = Regex.Match(ocid, @"ocds-[a-z0-9]+-(\d+)");
                    DateTime? publicado = DateTimeOffset.TryParse(Str(cr, "date"), out var dt) ? dt.UtcDateTime : null;
                    var comprador = cr.TryGetProperty("buyer", out var buyer) ? Str(buyer, "name") : null;

                    // Contacto oficial de la Unidad Operativa de Contrataciones (publicado en el release OCDS)
                    (string? nombre, string? email, string? tel) contacto = (null, null, null);
                    if (!conContacto.Contains(ocid) && r.TryGetProperty("releases", out var rels) && rels.ValueKind == JsonValueKind.Array && rels.GetArrayLength() > 0)
                    {
                        var relUrl = Str(rels[rels.GetArrayLength() - 1], "url");
                        if (relUrl != null && relUrl.StartsWith("https://www.contrataciones.gov.py/", StringComparison.OrdinalIgnoreCase))
                        {
                            try { contacto = ContactoComprador(JsonDocument.Parse(await _http.GetStringAsync(relUrl, ct)).RootElement); }
                            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { /* sin contacto */ }
                        }
                    }

                    lista.Add(new HqMercadoNecesidad
                    {
                        Fuente = "DNCP",
                        ContactoNombre = Recortar(contacto.nombre, 150),
                        ContactoEmail = Recortar(contacto.email, 150),
                        ContactoTelefono = Recortar(contacto.tel, 40),
                        ContactoOrigen = contacto.email != null || contacto.tel != null ? "Oficial · UOC de la entidad (DNCP)" : null,
                        ExternoId = Recortar(ocid, 150)!,
                        Titulo = Recortar(titulo, 400)!,
                        Descripcion = string.Join(" · ", new[] { Str(t, "mainProcurementCategoryDetails"), Str(t, "procurementMethodDetails"), estado }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Url = m.Success ? $"https://www.contrataciones.gov.py/licitaciones/convocatoria/{m.Groups[1].Value}.html" : null,
                        Solicitante = Recortar(comprador, 250),
                        Pais = "Paraguay",
                        FechaPublicacion = publicado,
                        FechaCierre = cierre,
                        Habilidades = Recortar(Str(t, "mainProcurementCategoryDetails"), 400)
                    });
                }

                var totalPages = root.TryGetProperty("pagination", out var pag) && pag.TryGetProperty("total_pages", out var tpg) ? tpg.GetInt32() : page;
                if (page >= totalPages) break;
            }
            return lista;
        }

        /// <summary>Tipo de cambio aproximado COP → USD para comparar presupuestos (solo orientativo).</summary>
        private const decimal CopPorUsd = 4100m;

        /// <summary>SECOP II Colombia — datos abiertos (datos.gov.co, Socrata). Procesos publicados de TI.</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarSecopAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var desde = (f.UltimaSync?.AddDays(-2) ?? ahora.AddDays(-15)).ToString("yyyy-MM-dd");
            var claves = new[] { "SOFTWARE", "SISTEMA DE INFORMACI", "PLATAFORMA", "APLICATIVO", "APLICACION", "PAGINA WEB", "SITIO WEB", "DIGITALIZ", "ERP" };
            var filtroTexto = string.Join(" OR ", claves.Select(k => $"upper(nombre_del_procedimiento) like '%{k}%'"));
            var where = $"fecha_de_publicacion_del >= '{desde}' AND estado_del_procedimiento = 'Publicado' AND ({filtroTexto})";
            var url = "https://www.datos.gov.co/resource/p6dx-8zbt.json" +
                      $"?$limit=300&$order=fecha_de_publicacion_del DESC&$where={Uri.EscapeDataString(where)}";
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
            var lista = new List<HqMercadoNecesidad>();
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return lista;

            foreach (var r in doc.RootElement.EnumerateArray())
            {
                var id = Str(r, "id_del_proceso");
                var titulo = Str(r, "nombre_del_procedimiento");
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(titulo)) continue;

                decimal? usd = null;
                var precio = Str(r, "precio_base");
                if (decimal.TryParse(precio, NumberStyles.Any, CultureInfo.InvariantCulture, out var cop) && cop > 0)
                    usd = Math.Round(cop / CopPorUsd, 0);
                var link = r.TryGetProperty("urlproceso", out var u) ? Str(u, "url") : null;
                DateTime? publicado = DateTime.TryParse(Str(r, "fecha_de_publicacion_del"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fp) ? fp.ToUniversalTime() : null;
                var detalle = Str(r, "descripci_n_del_procedimiento");

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "SECOP",
                    ExternoId = Recortar(id, 150)!,
                    Titulo = Recortar(titulo, 400)!,
                    Descripcion = Recortar(string.Join("\n", new[]
                    {
                        detalle,
                        cop > 0 ? $"Precio base: COP {cop:N0} (≈ USD {usd:N0}, tipo de cambio aproximado)" : null,
                        Str(r, "modalidad_de_contratacion"),
                        Str(r, "duracion") is { } dur ? $"Duración: {dur} {Str(r, "unidad_de_duracion")}" : null
                    }.Where(x => !string.IsNullOrWhiteSpace(x))), 4000),
                    Url = Recortar(link, 600),
                    Solicitante = Recortar($"{Str(r, "entidad")} · {Str(r, "ciudad_entidad")}", 250),
                    Pais = "Colombia",
                    Alcance = "LatAm",
                    PresupuestoMax = usd,
                    Moneda = usd.HasValue ? "USD" : null,
                    FechaPublicacion = publicado,
                    Habilidades = Recortar(Str(r, "tipo_de_contrato"), 400)
                });
            }
            return lista;
        }

        /// <summary>Banco Mundial — avisos de adquisiciones (API pública). Solo llamados y expresiones de interés abiertas de TI.</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarBancoMundialAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var q = string.IsNullOrWhiteSpace(f.Consulta) ? "software information system digital platform" : f.Consulta.Trim();
            var url = "https://search.worldbank.org/api/v2/procnotices" +
                      $"?format=json&rows=200&srt=noticedate&order=desc&qterm={Uri.EscapeDataString(q)}";
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
            var lista = new List<HqMercadoNecesidad>();
            if (!doc.RootElement.TryGetProperty("procnotices", out var notices) || notices.ValueKind != JsonValueKind.Array) return lista;

            foreach (var x in notices.EnumerateArray())
            {
                var tipo = Str(x, "notice_type") ?? "";
                if (tipo is not ("Invitation for Bids" or "Request for Expression of Interest" or "Invitation for Prequalification")) continue;
                var id = Str(x, "id");
                var titulo = Str(x, "bid_description") ?? Str(x, "project_name");
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(titulo)) continue;

                var proyecto = Str(x, "project_name");
                if (!RegexTiIntl.IsMatch(Normalizar($"{titulo} {proyecto}"))) continue;

                // La API no informa el plazo de presentación (submission_date repite la publicación):
                // tomamos avisos de los últimos 30 días y el plazo se revisa en el aviso.
                DateTime? publicado = DateTime.TryParseExact(Str(x, "noticedate"), "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var nd) ? nd.ToUniversalTime() : null;
                if (publicado.HasValue && publicado < ahora.AddDays(-30)) continue;
                var pais = Str(x, "project_ctry_name");

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "BancoMundial",
                    ExternoId = Recortar(id, 150)!,
                    Titulo = Recortar(titulo, 400)!,
                    Descripcion = Recortar(string.Join("\n", new[]
                    {
                        $"{tipo} · {Str(x, "procurement_group")} · {Str(x, "procurement_method_name")}",
                        proyecto != null ? $"Proyecto: {proyecto}" : null,
                        SinHtml(Str(x, "notice_text"))
                    }.Where(s => !string.IsNullOrWhiteSpace(s))), 4000),
                    Url = $"https://projects.worldbank.org/en/projects-operations/procurement-detail/{id}",
                    Solicitante = Recortar(proyecto != null ? $"Banco Mundial · {proyecto}" : "Banco Mundial", 250),
                    Pais = Recortar(pais, 60),
                    FechaPublicacion = publicado,
                    Habilidades = Recortar(Str(x, "procurement_group"), 400)
                });
            }
            return lista;
        }

        /// <summary>Contacto del comprador en un release OCDS (parties con rol buyer / procuringEntity).</summary>
        private static (string? nombre, string? email, string? tel) ContactoComprador(JsonElement root)
        {
            var rel = root.TryGetProperty("releases", out var rs) && rs.ValueKind == JsonValueKind.Array && rs.GetArrayLength() > 0 ? rs[0] : root;
            if (!rel.TryGetProperty("parties", out var parties) || parties.ValueKind != JsonValueKind.Array) return (null, null, null);
            foreach (var p in parties.EnumerateArray())
            {
                var roles = p.TryGetProperty("roles", out var ro) && ro.ValueKind == JsonValueKind.Array
                    ? ro.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : new List<string>();
                if (!roles.Contains("buyer") && !roles.Contains("procuringEntity")) continue;
                if (!p.TryGetProperty("contactPoint", out var cp)) continue;
                var email = Str(cp, "email");
                if (email != null && !email.Contains('@')) email = null;
                var tel = Str(cp, "telephone");
                if (email != null || tel != null) return (Str(cp, "name"), email?.Trim(), tel?.Trim());
            }
            return (null, null, null);
        }

        /// <summary>Tipo de cambio aproximado GBP → USD para comparar presupuestos (solo orientativo).</summary>
        private const decimal UsdPorGbp = 1.27m;

        /// <summary>Tipo de cambio aproximado BRL → USD (solo orientativo).</summary>
        private const decimal UsdPorBrl = 0.18m;

        /// <summary>Filtro de licitaciones de TI en portugués (texto normalizado).</summary>
        private static readonly Regex RegexTiPt = new(
            @" (software\w*|sistemas? (de gestao|informatizad\w*|integrad\w*|web|de informac\w*)|plataforma (digital|web|online|tecnologica)|aplicativo\w*|desenvolvimento de (sistema|software|aplicativo|plataforma|site)\w*|licenca\w* de uso|locacao de (software|sistema)\w*|solucao tecnologica|gestao eletronica|portal (web|institucional)|site institucional) ",
            RegexOptions.Compiled);

        /// <summary>
        /// Brasil — PNCP, API oficial de consulta (dados abertos). Lee lo publicado en los últimos días
        /// para la modalidad indicada en la fuente (6 = pregão eletrônico, 8 = dispensa) y filtra lo de TI.
        /// </summary>
        private async Task<List<HqMercadoNecesidad>> BuscarPncpAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var modalidad = int.TryParse(f.Consulta?.Trim(), out var mod) ? mod : 6;
            var desde = (f.UltimaSync?.AddHours(-6) ?? ahora.AddDays(-2));
            if (desde < ahora.AddDays(-3)) desde = ahora.AddDays(-3);
            var lista = new List<HqMercadoNecesidad>();
            for (var pagina = 1; pagina <= 30; pagina++)
            {
                var url = "https://pncp.gov.br/api/consulta/v1/contratacoes/publicacao" +
                          $"?dataInicial={desde:yyyyMMdd}&dataFinal={ahora:yyyyMMdd}&codigoModalidadeContratacao={modalidad}&pagina={pagina}&tamanhoPagina=50";
                // El portal limita la frecuencia: pausa entre páginas y, si responde 429, espera lo que pide una vez
                if (pagina > 1) await Task.Delay(700, ct);
                var resp = await _http.GetAsync(url, ct);
                if (resp.StatusCode == (HttpStatusCode)429)
                {
                    var espera = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(20);
                    resp.Dispose();
                    if (espera > TimeSpan.FromSeconds(45)) break;   // se retoma en la próxima vuelta del radar
                    await Task.Delay(espera, ct);
                    resp = await _http.GetAsync(url, ct);
                    if (resp.StatusCode == (HttpStatusCode)429) { resp.Dispose(); break; }
                }
                using var _ = resp;
                if (resp.StatusCode == HttpStatusCode.NoContent) break;
                resp.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0) break;

                foreach (var x in data.EnumerateArray())
                {
                    var objeto = Str(x, "objetoCompra");
                    var id = Str(x, "numeroControlePNCP");
                    if (string.IsNullOrWhiteSpace(objeto) || string.IsNullOrWhiteSpace(id)) continue;
                    var complemento = Str(x, "informacaoComplementar");
                    // Solo el objeto: el complemento suele nombrar la plataforma del pregão y daría falsos positivos
                    if (!RegexTiPt.IsMatch(Normalizar(objeto))) continue;

                    DateTime? cierre = DateTime.TryParse(Str(x, "dataEncerramentoProposta"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dc) ? dc.ToUniversalTime() : null;
                    if (cierre.HasValue && cierre < ahora) continue;
                    DateTime? publicado = DateTime.TryParse(Str(x, "dataPublicacaoPncp"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dp) ? dp.ToUniversalTime() : null;

                    var orgao = x.TryGetProperty("orgaoEntidade", out var o) ? o : default;
                    var unidade = x.TryGetProperty("unidadeOrgao", out var u) ? u : default;
                    var cnpj = orgao.ValueKind == JsonValueKind.Object ? Str(orgao, "cnpj") : null;
                    var ano = x.TryGetProperty("anoCompra", out var an) && an.ValueKind == JsonValueKind.Number ? an.GetInt32() : (int?)null;
                    var seq = x.TryGetProperty("sequencialCompra", out var sq) && sq.ValueKind == JsonValueKind.Number ? sq.GetInt32() : (int?)null;
                    var valor = Dec(x, "valorTotalEstimado");
                    decimal? usd = valor > 0 ? Math.Round(valor.Value * UsdPorBrl, 0) : null;
                    var titulo = objeto.Length > 220 ? objeto[..217] + "…" : objeto;

                    lista.Add(new HqMercadoNecesidad
                    {
                        Fuente = "PNCP",
                        ExternoId = Recortar(id, 150)!,
                        Titulo = Recortar(titulo, 400)!,
                        Descripcion = Recortar(string.Join("\n", new[]
                        {
                            $"{Str(x, "modalidadeNome")} · {Str(x, "situacaoCompraNome")}",
                            valor > 0 ? $"Valor estimado: R$ {valor:N2} (≈ USD {usd:N0}, tipo de cambio aproximado)" : null,
                            objeto,
                            complemento,
                            "El contacto del órgano y el pliego están en el edital del link (PNCP o sistema de origen)."
                        }.Where(s => !string.IsNullOrWhiteSpace(s))), 4000),
                        Url = cnpj != null && ano.HasValue && seq.HasValue
                            ? $"https://pncp.gov.br/app/editais/{cnpj}/{ano}/{seq}"
                            : Recortar(Str(x, "linkSistemaOrigem"), 600),
                        Solicitante = Recortar(string.Join(" · ", new[]
                        {
                            orgao.ValueKind == JsonValueKind.Object ? Str(orgao, "razaoSocial") : null,
                            unidade.ValueKind == JsonValueKind.Object ? $"{Str(unidade, "municipioNome")}/{Str(unidade, "ufSigla")}" : null
                        }.Where(s => !string.IsNullOrWhiteSpace(s))), 250),
                        Pais = "Brasil",
                        Alcance = "LatAm",
                        PresupuestoMax = usd,
                        Moneda = usd.HasValue ? "USD" : null,
                        FechaPublicacion = publicado,
                        FechaCierre = cierre,
                        Habilidades = Recortar(Str(x, "modalidadeNome"), 400)
                    });
                }

                var totalPaginas = root.TryGetProperty("totalPaginas", out var tp) && tp.ValueKind == JsonValueKind.Number ? tp.GetInt32() : pagina;
                if (pagina >= totalPaginas) break;
            }
            return lista;
        }

        /// <summary>Reino Unido — Contracts Finder (OCDS público). Trae el contacto del comprador.</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarContractsFinderAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var q = string.IsNullOrWhiteSpace(f.Consulta) ? "software" : f.Consulta.Trim();
            var url = "https://www.contractsfinder.service.gov.uk/Published/Notices/OCDS/Search" +
                      $"?publishedFrom={ahora.AddDays(-21):yyyy-MM-dd}&stages=tender&limit=100&keyword={Uri.EscapeDataString(q)}";
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
            var lista = new List<HqMercadoNecesidad>();
            if (!doc.RootElement.TryGetProperty("releases", out var rels) || rels.ValueKind != JsonValueKind.Array) return lista;

            foreach (var r in rels.EnumerateArray())
            {
                if (!r.TryGetProperty("tender", out var t)) continue;
                var titulo = Str(t, "title");
                var ocid = Str(r, "ocid");
                if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(ocid)) continue;
                if ((Str(t, "status") ?? "active") is "cancelled" or "complete" or "unsuccessful") continue;
                // El buscador del portal es muy amplio: se exige que el título sea de TI
                if (!RegexTiIntl.IsMatch(Normalizar(titulo))) continue;

                DateTime? cierre = t.TryGetProperty("tenderPeriod", out var tp) && DateTimeOffset.TryParse(Str(tp, "endDate"), out var end) ? end.UtcDateTime : null;
                if (cierre.HasValue && cierre < ahora) continue;
                DateTime? publicado = DateTimeOffset.TryParse(Str(t, "datePublished") ?? Str(r, "date"), out var dp) ? dp.UtcDateTime : null;

                decimal? usd = null;
                string? montoTxt = null;
                if (t.TryGetProperty("value", out var v))
                {
                    var monto = Dec(v, "amount");
                    var moneda = Str(v, "currency");
                    if (monto > 0)
                    {
                        usd = moneda == "GBP" ? Math.Round(monto.Value * UsdPorGbp, 0) : moneda == "USD" ? monto : null;
                        montoTxt = $"Valor estimado: {moneda} {monto:N0}";
                    }
                }
                string? link = null;
                if (t.TryGetProperty("documents", out var docs) && docs.ValueKind == JsonValueKind.Array)
                    link = docs.EnumerateArray().Select(d => Str(d, "url")).FirstOrDefault(u => u != null && u.Contains("/Notice/", StringComparison.OrdinalIgnoreCase));
                var (cNombre, cEmail, cTel) = ContactoComprador(r);
                var comprador = r.TryGetProperty("buyer", out var b) ? Str(b, "name") : null;

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "ContractsFinder",
                    ExternoId = Recortar(ocid, 150)!,
                    Titulo = Recortar(titulo, 400)!,
                    Descripcion = Recortar(string.Join("\n", new[] { montoTxt, Str(t, "procurementMethodDetails"), SinHtml(Str(t, "description")) }.Where(x => !string.IsNullOrWhiteSpace(x))), 4000),
                    Url = Recortar(link, 600),
                    Solicitante = Recortar(comprador, 250),
                    Pais = "Reino Unido",
                    Alcance = "Internacional",
                    PresupuestoMax = usd,
                    Moneda = usd.HasValue ? "USD" : null,
                    FechaPublicacion = publicado,
                    FechaCierre = cierre,
                    Habilidades = Recortar(Str(t, "mainProcurementCategory"), 400),
                    ContactoNombre = Recortar(cNombre, 150),
                    ContactoEmail = Recortar(cEmail, 150),
                    ContactoTelefono = Recortar(cTel, 40),
                    ContactoOrigen = cEmail != null || cTel != null ? "Oficial · comprador (Contracts Finder)" : null
                });
            }
            return lista;
        }

        // ───────────────────────── Sudamérica ─────────────────────────

        /// <summary>Tipos de cambio aproximados a USD (solo orientativos).</summary>
        private const decimal UsdPorPen = 0.27m, UsdPorUyu = 0.025m, UsdPorClp = 0.00105m;

        /// <summary>
        /// Perú — Portal de Contrataciones Abiertas (OECE, OCDS de SEACE). Busca por palabra en lo publicado
        /// este mes y el anterior; el contacto del comprador sale del release.
        /// </summary>
        private async Task<List<HqMercadoNecesidad>> BuscarSeaceAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var claves = (string.IsNullOrWhiteSpace(f.Consulta) ? "software,sistema,aplicativo,plataforma" : f.Consulta)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(6).ToList();
            var meses = new[] { ahora, ahora.AddDays(-20) }.Select(d => (d.Year, d.Month)).Distinct().ToList();
            var lista = new List<HqMercadoNecesidad>();
            var vistos = new HashSet<string>();

            foreach (var clave in claves)
                foreach (var (anio, mes) in meses)
                {
                    var url = "https://contratacionesabiertas.oece.gob.pe/api/v1/search" +
                              $"?search={Uri.EscapeDataString(clave)}&year={anio}&month={mes}&has_tender=1&paginateBy=50&page=1&format=json";
                    using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
                    if (!doc.RootElement.TryGetProperty("results", out var res) || res.ValueKind != JsonValueKind.Array) continue;

                    foreach (var r in res.EnumerateArray())
                    {
                        if (!r.TryGetProperty("compiledRelease", out var cr) || !cr.TryGetProperty("tender", out var t)) continue;
                        var ocid = Str(cr, "ocid");
                        var desc = Str(t, "description");
                        if (string.IsNullOrWhiteSpace(ocid) || string.IsNullOrWhiteSpace(desc) || !vistos.Add(ocid)) continue;
                        if (!RegexTi.IsMatch(Normalizar(desc))) continue;
                        DateTime? publicado = DateTimeOffset.TryParse(Str(cr, "date"), out var dp) ? dp.UtcDateTime : null;
                        // El período de la API suele ser el registro de participantes: se toma lo publicado en los últimos 15 días
                        if (publicado.HasValue && publicado < ahora.AddDays(-15)) continue;

                        decimal? usd = null;
                        string? montoTxt = null;
                        if (t.TryGetProperty("value", out var v) && Dec(v, "amount") is > 0 and var monto)
                        {
                            var moneda = Str(v, "currency");
                            usd = moneda == "PEN" ? Math.Round(monto * UsdPorPen, 0) : moneda == "USD" ? monto : (decimal?)null;
                            montoTxt = $"Valor referencial: {moneda} {monto:N0}";
                        }

                        (string? nombre, string? email, string? tel) contacto = (null, null, null);
                        if (r.TryGetProperty("releases", out var rels) && rels.ValueKind == JsonValueKind.Array && rels.GetArrayLength() > 0)
                        {
                            var relUrl = Str(rels[rels.GetArrayLength() - 1], "url");
                            if (relUrl != null && relUrl.StartsWith("https://contratacionesabiertas.oece.gob.pe/", StringComparison.OrdinalIgnoreCase))
                            {
                                try { contacto = ContactoComprador(JsonDocument.Parse(await _http.GetStringAsync(relUrl, ct)).RootElement); }
                                catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { /* sin contacto */ }
                            }
                        }
                        var comprador = cr.TryGetProperty("buyer", out var b) ? Str(b, "name") : null;

                        lista.Add(new HqMercadoNecesidad
                        {
                            Fuente = "SEACE",
                            ExternoId = Recortar(ocid, 150)!,
                            Titulo = Recortar(desc, 400)!,
                            Descripcion = Recortar(string.Join("\n", new[] { $"{Str(t, "procurementMethodDetails")} · {Str(t, "title")}", montoTxt, desc }
                                .Where(x => !string.IsNullOrWhiteSpace(x))), 4000),
                            Url = $"https://contratacionesabiertas.oece.gob.pe/proceso/{Uri.EscapeDataString(ocid)}",
                            Solicitante = Recortar(comprador, 250),
                            Pais = "Perú",
                            Alcance = "LatAm",
                            PresupuestoMax = usd,
                            Moneda = usd.HasValue ? "USD" : null,
                            FechaPublicacion = publicado,
                            Habilidades = Recortar(Str(t, "mainProcurementCategory"), 400),
                            ContactoNombre = Recortar(contacto.nombre, 150),
                            ContactoEmail = Recortar(contacto.email, 150),
                            ContactoTelefono = Recortar(contacto.tel, 40),
                            ContactoOrigen = contacto.email != null || contacto.tel != null ? "Oficial · entidad compradora (SEACE Perú)" : null
                        });
                    }
                    await Task.Delay(400, ct);
                }
            return lista;
        }

        private static readonly Regex RegexPlazoArce = new(@"hasta:\s*(\d{2}/\d{2}/\d{4})\s+(\d{2}:\d{2})", RegexOptions.Compiled);

        /// <summary>
        /// Uruguay — ARCE (Agencia Reguladora de Compras Estatales). Feed de llamados vigentes; el correo y teléfono
        /// del comprador salen del release OCDS de cada llamado.
        /// </summary>
        private async Task<List<HqMercadoNecesidad>> BuscarArceAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            await using var stream = await _http.GetStreamAsync("https://www.comprasestatales.gub.uy/consultas/rss/tipo-pub/VIG", ct);
            var xml = XDocument.Load(stream);
            var lista = new List<HqMercadoNecesidad>();

            foreach (var it in xml.Descendants("item"))
            {
                var tituloRss = it.Element("title")?.Value ?? "";
                var descHtml = it.Element("description")?.Value ?? "";
                var objeto = SinHtml(descHtml.Split("<br", 2)[0]) ?? "";
                var link = it.Element("link")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(objeto) || string.IsNullOrWhiteSpace(link)) continue;
                if (!RegexTi.IsMatch(Normalizar(objeto))) continue;

                DateTime? cierre = null;
                var m = RegexPlazoArce.Match(SinHtml(descHtml) ?? "");
                if (m.Success && DateTime.TryParseExact($"{m.Groups[1].Value} {m.Groups[2].Value}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var c))
                    cierre = DateTime.SpecifyKind(c.AddHours(3), DateTimeKind.Utc); // hora de Uruguay (UTC-3)
                if (cierre.HasValue && cierre < ahora) continue;
                DateTime? publicado = DateTimeOffset.TryParse(it.Element("pubDate")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dp) ? dp.UtcDateTime : null;

                // "Licitación Pública 123/2026 - Organismo | Unidad"
                var partes = tituloRss.Split(" - ", 2);
                var idCompra = link.Split('/').Last();
                (string? nombre, string? email, string? tel) contacto = (null, null, null);
                if (Regex.IsMatch(idCompra, @"^\d+$"))
                {
                    try { contacto = ContactoComprador(JsonDocument.Parse(await _http.GetStringAsync($"https://www.comprasestatales.gub.uy/ocds/release/llamado-{idCompra}", ct)).RootElement); }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { /* sin contacto */ }
                }

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "ARCE",
                    ExternoId = Recortar(idCompra, 150)!,
                    Titulo = Recortar(objeto, 400)!,
                    Descripcion = Recortar($"{partes[0]}\n{SinHtml(descHtml.Replace("<br/>", "\n"))}", 4000),
                    Url = Recortar(link.Replace("http://", "https://"), 600),
                    Solicitante = Recortar(partes.Length > 1 ? partes[1].Split(" | ")[0] : null, 250),
                    Pais = "Uruguay",
                    Alcance = "LatAm",
                    FechaPublicacion = publicado,
                    FechaCierre = cierre,
                    Habilidades = Recortar(partes[0], 400),
                    ContactoNombre = Recortar(contacto.nombre, 150),
                    ContactoEmail = Recortar(contacto.email, 150),
                    ContactoTelefono = Recortar(contacto.tel, 40),
                    ContactoOrigen = contacto.email != null || contacto.tel != null ? "Oficial · organismo comprador (ARCE Uruguay)" : null
                });
            }
            return lista;
        }

        /// <summary>Filtro estricto de TI en es/pt/en para avisos multisectoriales (PNUD): "sistema" o "application" solos no alcanzan.</summary>
        private static readonly Regex RegexTiEstricto = new(
            @" (software\w*|sistemas? (de informacion|informatic\w*|de gestion|web|de informacao|informatizad\w*)|information (system|management)\w*|plataformas? (digital|web|virtual|informatica|lms|de gestion)\w*|digital platform\w*|lms|e learning|sitio web|pagina web|portal web|micrositio|website|web (site|portal|application|platform)\w*|aplicacion(es)? (web|movil)|aplicativo\w*|mobile app\w*|base de datos|database|erp|crm|desarrollo de (sistema|software|aplicacion|plataforma)\w*|desenvolvimento de (sistema|software)\w*|digitaliza\w*|e government|gobierno digital) ",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, string> PaisesSudamericaPnud = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ARGENTINA"] = "Argentina", ["BOLIVIA"] = "Bolivia", ["BRAZIL"] = "Brasil", ["CHILE"] = "Chile", ["COLOMBIA"] = "Colombia",
            ["ECUADOR"] = "Ecuador", ["GUYANA"] = "Guyana", ["PARAGUAY"] = "Paraguay", ["PERU"] = "Perú", ["SURINAME"] = "Surinam",
            ["URUGUAY"] = "Uruguay", ["VENEZUELA"] = "Venezuela"
        };

        /// <summary>
        /// PNUD (Naciones Unidas) — feed público de llamados. Cubre todos los países de Sudamérica, incluidos los que
        /// no publican datos abiertos (Argentina, Bolivia, Venezuela, Guyana, Surinam). Filtra TI en es/pt/en.
        /// </summary>
        private async Task<List<HqMercadoNecesidad>> BuscarPnudAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            await using var stream = await _http.GetStreamAsync("https://procurement-notices.undp.org/rss_feeds/rss.xml", ct);
            var xml = XDocument.Load(stream); // respeta el encoding declarado (ISO-8859-1)
            XNamespace u = "http://procurement-notices.undp.org/rss_feed/spec/";
            var lista = new List<HqMercadoNecesidad>();

            foreach (var it in xml.Descendants().Where(e => e.Name.LocalName == "item"))
            {
                string? Val(string nombre) => it.Elements().FirstOrDefault(e => e.Name.LocalName == nombre)?.Value?.Trim();
                var pais = it.Element(u + "duty_station_cty")?.Value?.Trim() ?? "";
                if (!PaisesSudamericaPnud.TryGetValue(pais, out var paisEs)) continue;
                var titulo = WebUtility.HtmlDecode(Val("subject") ?? Val("title") ?? "");
                var link = Val("link");
                if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(link)) continue;
                var norm = Normalizar(titulo);
                if (!RegexTiEstricto.IsMatch(norm)) continue;

                DateTime? cierre = DateTimeOffset.TryParse(it.Element(u + "deadline")?.Value, out var dc) ? dc.UtcDateTime : null;
                if (cierre.HasValue && cierre < ahora) continue;
                DateTime? publicado = DateTimeOffset.TryParse(Val("date"), out var dp) ? dp.UtcDateTime : null;

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "PNUD",
                    ExternoId = Recortar(link, 150)!,
                    Titulo = Recortar(titulo, 400)!,
                    Descripcion = Recortar(string.Join("\n", new[]
                    {
                        $"PNUD {paisEs} · {it.Element(u + "area_desc")?.Value?.Trim()}",
                        Val("description"),
                        "El contacto y el pliego están en el aviso; las ofertas se presentan por el portal Quantum del PNUD."
                    }.Where(x => !string.IsNullOrWhiteSpace(x))), 4000),
                    Url = Recortar(link, 600),
                    Solicitante = Recortar($"PNUD · {WebUtility.HtmlDecode(it.Element(u + "duty_station")?.Value?.Trim() ?? paisEs)}", 250),
                    Pais = paisEs,
                    FechaPublicacion = publicado,
                    FechaCierre = cierre,
                    Habilidades = Recortar(it.Element(u + "area_desc")?.Value?.Trim(), 400)
                });
            }
            return lista;
        }

        /// <summary>
        /// Chile — API de Mercado Público. Necesita un ticket gratuito (api.mercadopublico.cl) cargado en la consulta
        /// de la fuente. Trae las licitaciones activas, filtra TI y pide el detalle de las que encajan.
        /// </summary>
        private async Task<List<HqMercadoNecesidad>> BuscarMercadoPublicoAsync(HqMercadoFuente f, DateTime ahora, CancellationToken ct)
        {
            var ticket = f.Consulta?.Trim();
            if (string.IsNullOrWhiteSpace(ticket))
                throw new InvalidOperationException("Falta el ticket de Mercado Público: pedilo gratis en api.mercadopublico.cl y cargalo en la consulta de la fuente");
            var t = Uri.EscapeDataString(ticket);
            using var doc = JsonDocument.Parse(await _http.GetStringAsync($"https://api.mercadopublico.cl/servicios/v1/publico/licitaciones.json?estado=activas&ticket={t}", ct));
            var root = doc.RootElement;
            if (root.TryGetProperty("Mensaje", out var msg) && !root.TryGetProperty("Listado", out _))
                throw new InvalidOperationException($"Mercado Público: {msg.GetString()}");
            var lista = new List<HqMercadoNecesidad>();
            if (!root.TryGetProperty("Listado", out var items) || items.ValueKind != JsonValueKind.Array) return lista;

            var candidatas = items.EnumerateArray()
                .Select(x => (codigo: Str(x, "CodigoExterno"), nombre: Str(x, "Nombre"), cierre: Str(x, "FechaCierre")))
                .Where(x => x.codigo != null && x.nombre != null && RegexTi.IsMatch(Normalizar(x.nombre)))
                .Take(40).ToList();

            foreach (var (codigo, nombre, cierreTxt) in candidatas)
            {
                DateTime? cierre = DateTime.TryParse(cierreTxt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dc) ? dc.ToUniversalTime() : null;
                string? descripcion = null, organismo = null, region = null;
                decimal? usd = null;
                DateTime? publicado = null;
                try
                {
                    await Task.Delay(350, ct); // la API no admite pedidos simultáneos
                    using var det = JsonDocument.Parse(await _http.GetStringAsync($"https://api.mercadopublico.cl/servicios/v1/publico/licitaciones.json?codigo={Uri.EscapeDataString(codigo!)}&ticket={t}", ct));
                    if (det.RootElement.TryGetProperty("Listado", out var l) && l.ValueKind == JsonValueKind.Array && l.GetArrayLength() > 0)
                    {
                        var d = l[0];
                        descripcion = Str(d, "Descripcion");
                        if (d.TryGetProperty("Comprador", out var comp)) { organismo = Str(comp, "NombreOrganismo"); region = Str(comp, "RegionUnidad"); }
                        if (Dec(d, "MontoEstimado") is > 0 and var monto) usd = Math.Round(monto * UsdPorClp, 0);
                        if (d.TryGetProperty("Fechas", out var fe) && DateTime.TryParse(Str(fe, "FechaPublicacion"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var fp))
                            publicado = fp.ToUniversalTime();
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException) { /* queda sin detalle */ }

                lista.Add(new HqMercadoNecesidad
                {
                    Fuente = "MercadoPublico",
                    ExternoId = Recortar(codigo, 150)!,
                    Titulo = Recortar(nombre, 400)!,
                    Descripcion = Recortar(descripcion, 4000),
                    Url = $"https://www.mercadopublico.cl/Procurement/Modules/RFB/DetailsAcquisition.aspx?idlicitacion={Uri.EscapeDataString(codigo!)}",
                    Solicitante = Recortar(string.Join(" · ", new[] { organismo, region }.Where(x => !string.IsNullOrWhiteSpace(x))), 250),
                    Pais = "Chile",
                    Alcance = "LatAm",
                    PresupuestoMax = usd,
                    Moneda = usd.HasValue ? "USD" : null,
                    FechaPublicacion = publicado,
                    FechaCierre = cierre
                });
            }
            return lista;
        }

        /// <summary>RSS / Atom genérico (Google Alerts, feeds de grupos, portales de empleo).</summary>
        private async Task<List<HqMercadoNecesidad>> BuscarRssAsync(HqMercadoFuente f, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(f.Consulta) || !Uri.TryCreate(f.Consulta.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new InvalidOperationException("La fuente RSS necesita una URL http(s) válida");

            var xml = XDocument.Parse(await _http.GetStringAsync(uri, ct));
            XNamespace atom = "http://www.w3.org/2005/Atom";
            var lista = new List<HqMercadoNecesidad>();

            foreach (var it in xml.Descendants("item").Take(100))
            {
                var link = it.Element("link")?.Value?.Trim();
                lista.Add(NuevaRss(f, it.Element("guid")?.Value ?? link, it.Element("title")?.Value, it.Element("description")?.Value, link, it.Element("pubDate")?.Value));
            }
            foreach (var e in xml.Descendants(atom + "entry").Take(100))
            {
                var link = e.Element(atom + "link")?.Attribute("href")?.Value;
                lista.Add(NuevaRss(f, e.Element(atom + "id")?.Value ?? link, e.Element(atom + "title")?.Value,
                    e.Element(atom + "content")?.Value ?? e.Element(atom + "summary")?.Value, link, e.Element(atom + "published")?.Value ?? e.Element(atom + "updated")?.Value));
            }
            return lista.Where(n => !string.IsNullOrWhiteSpace(n.Titulo)).ToList();
        }

        private static HqMercadoNecesidad NuevaRss(HqMercadoFuente f, string? id, string? titulo, string? desc, string? link, string? fecha)
        {
            var idTxt = string.IsNullOrWhiteSpace(id) ? $"{titulo}|{link}" : id.Trim();
            return new HqMercadoNecesidad
            {
                Fuente = "RSS",
                ExternoId = idTxt.Length <= 150 ? idTxt : Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(idTxt))),
                Titulo = Recortar(SinHtml(titulo), 400) ?? "",
                Descripcion = Recortar(SinHtml(desc), 4000),
                Url = Recortar(DesenvolverGoogle(link), 600),
                Solicitante = f.Nombre,
                FechaPublicacion = DateTimeOffset.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d.UtcDateTime : null
            };
        }

        /// <summary>Google Alerts envuelve los links en google.com/url?url=…</summary>
        private static string? DesenvolverGoogle(string? link)
        {
            if (string.IsNullOrWhiteSpace(link) || !link.Contains("google.com/url", StringComparison.OrdinalIgnoreCase)) return link;
            var m = Regex.Match(link, @"[?&]url=([^&]+)");
            return m.Success ? WebUtility.UrlDecode(m.Groups[1].Value) : link;
        }

        private static string? SinHtml(string? s) =>
            s == null ? null : WebUtility.HtmlDecode(Regex.Replace(s, "<[^>]+>", " ")).Replace("​", "").Trim();

        private static string? Str(JsonElement e, string prop) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static bool Bool(JsonElement e, string prop) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;

        private static decimal? Dec(JsonElement e, string prop) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

        private static string? Recortar(string? s, int max) =>
            s == null ? null : (s.Length <= max ? s : s[..(max - 1)] + "…");

        // ───────────────────────── Consulta ─────────────────────────

        internal static MercadoNecesidadDto Map(HqMercadoNecesidad n)
        {
            var regla = Reglas.FirstOrDefault(r => r.Categoria == n.Categoria) ?? SoftwareAMedida;
            var (setup, mensual) = PrecioMicroapp(regla.Categoria, n.Moneda == "USD" ? n.PresupuestoMax : null);
            return new MercadoNecesidadDto
            {
            NecesidadId = n.NecesidadId,
            Fuente = n.Fuente,
            Titulo = n.Titulo,
            Descripcion = n.Descripcion,
            Url = n.Url,
            Solicitante = n.Solicitante,
            Pais = n.Pais,
            PresupuestoMin = n.PresupuestoMin,
            PresupuestoMax = n.PresupuestoMax,
            Moneda = n.Moneda,
            FechaPublicacion = n.FechaPublicacion,
            FechaCierre = n.FechaCierre,
            Habilidades = n.Habilidades,
            Vertical = n.Vertical,
            Categoria = n.Categoria,
            ProductoSugerido = n.ProductoSugerido,
            Puntaje = n.Puntaje,
            MotivoPuntaje = n.MotivoPuntaje,
            Confianza = n.Confianza,
            ConfianzaMotivo = n.ConfianzaMotivo,
            Competidores = n.Competidores,
            Alcance = n.Alcance,
            Estado = n.Estado,
            ResponsableNombre = n.ResponsableNombre,
            ContactoNombre = n.ContactoNombre,
            ContactoEmail = n.ContactoEmail,
            ContactoTelefono = n.ContactoTelefono,
            ContactoOrigen = n.ContactoOrigen,
            PrecioSetupSugeridoUsd = setup,
            PrecioMensualSugeridoUsd = mensual,
            KitSugerido = regla.Kit,
            Notas = n.Notas,
            ClienteId = n.ClienteId,
            RoadmapId = n.RoadmapId,
            FechaCaptura = n.FechaCaptura
            };
        }

        public async Task<List<MercadoNecesidadDto>> ListarAsync(int empresaId, string? estado, string? vertical, string? fuente, int? minPuntaje, string? q, int? dias,
            string? alcance = null, int? minConfianza = null, bool conContacto = false)
        {
            var query = _db.HqMercadoNecesidades.AsNoTracking().Where(n => n.EmpresaId == empresaId && n.Activo);
            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "Activas") query = query.Where(n => n.Estado != "Descartada" && n.Estado != "Ganada");
                else if (estado == "Gestion") query = query.Where(n => EstadosGestion.Contains(n.Estado));
                else query = query.Where(n => n.Estado == estado);
            }
            if (!string.IsNullOrWhiteSpace(vertical)) query = query.Where(n => n.Vertical == vertical);
            if (!string.IsNullOrWhiteSpace(fuente)) query = query.Where(n => n.Fuente == fuente);
            if (minPuntaje is > 0) query = query.Where(n => n.Puntaje >= minPuntaje);
            if (!string.IsNullOrWhiteSpace(alcance)) query = query.Where(n => n.Alcance == alcance);
            if (minConfianza is > 0) query = query.Where(n => n.Confianza >= minConfianza);
            if (conContacto) query = query.Where(n => n.ContactoEmail != null || n.ContactoTelefono != null);
            if (dias is > 0)
            {
                var desde = DateTime.UtcNow.AddDays(-dias.Value);
                query = query.Where(n => (n.FechaPublicacion ?? n.FechaCaptura) >= desde);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(n => n.Titulo.Contains(t) || (n.Descripcion != null && n.Descripcion.Contains(t)) || (n.Solicitante != null && n.Solicitante.Contains(t)));
            }
            var list = await query
                .OrderByDescending(n => n.Puntaje)
                .ThenByDescending(n => n.FechaPublicacion ?? n.FechaCaptura)
                .Take(300)
                .ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<MercadoResumenDto> ResumenAsync(int empresaId)
        {
            var ahora = DateTime.UtcNow;
            var todas = await _db.HqMercadoNecesidades.AsNoTracking().Where(n => n.EmpresaId == empresaId && n.Activo).ToListAsync();
            var activas = todas.Where(n => n.Estado != "Descartada").ToList();
            var fuentes = await _db.HqMercadoFuentes.AsNoTracking().Where(f => f.EmpresaId == empresaId).ToListAsync();

            var dto = new MercadoResumenDto
            {
                Total = activas.Count,
                Nuevas7d = activas.Count(n => n.FechaCaptura >= ahora.AddDays(-7)),
                Calientes = activas.Count(n => n.Puntaje >= 70 && (n.Estado == "Nueva" || n.Estado == "Evaluando")),
                EnGestion = activas.Count(n => EstadosGestion.Contains(n.Estado)),
                Ganadas = activas.Count(n => n.Estado == "Ganada"),
                ConvertidasCrm = activas.Count(n => n.ClienteId.HasValue),
                Confiables = activas.Count(n => n.Confianza >= 70 && n.Puntaje >= 40 && n.Estado != "Ganada"),
                UltimaSync = fuentes.Max(f => f.UltimaSync),
                PorFuente = activas.GroupBy(n => n.Fuente)
                    .Select(g => new MercadoConteoDto { Clave = g.Key, Cantidad = g.Count(), PuntajePromedio = (int)Math.Round(g.Average(n => n.Puntaje)) })
                    .OrderByDescending(x => x.Cantidad).ToList(),
                PorVertical = activas.GroupBy(n => n.Vertical ?? "A_MEDIDA")
                    .Select(g => new MercadoConteoDto { Clave = g.Key, Cantidad = g.Count(), PuntajePromedio = (int)Math.Round(g.Average(n => n.Puntaje)) })
                    .OrderByDescending(x => x.Cantidad).ToList(),
                PorAlcance = activas.GroupBy(n => n.Alcance ?? "Internacional")
                    .Select(g => new MercadoConteoDto { Clave = g.Key, Cantidad = g.Count(), PuntajePromedio = (int)Math.Round(g.Average(n => n.Puntaje)) })
                    .OrderByDescending(x => x.Cantidad).ToList()
            };

            // Oportunidades de producto rápido: categorías con pedidos repetidos y buen encaje en los últimos 60 días
            var recientes = activas.Where(n => (n.FechaPublicacion ?? n.FechaCaptura) >= ahora.AddDays(-60) && n.Puntaje >= 30).ToList();
            dto.Oportunidades = recientes
                .GroupBy(n => n.Categoria ?? SoftwareAMedida.Categoria)
                .Where(g => g.Count() >= 2)
                .Select(g =>
                {
                    var regla = Reglas.FirstOrDefault(r => r.Categoria == g.Key) ?? SoftwareAMedida;
                    var presupuestos = g.Where(n => n.Moneda == "USD" && n.PresupuestoMax >= 50).Select(n => n.PresupuestoMax!.Value).OrderBy(x => x).ToList();
                    decimal? mediana = presupuestos.Count > 0 ? presupuestos[presupuestos.Count / 2] : null;
                    var (setup, mensual) = PrecioMicroapp(g.Key, mediana);
                    return new MercadoOportunidadDto
                    {
                        Categoria = g.Key,
                        Vertical = regla.Vertical,
                        Cantidad = g.Count(),
                        CalientesAltas = g.Count(n => n.Puntaje >= 70),
                        PresupuestoMedianoUsd = mediana,
                        PrecioSetupUsd = setup,
                        PrecioMensualUsd = mensual,
                        Mrr10ClientesUsd = mensual * 10,
                        ProductoVordia = regla.Producto,
                        ProductoRapido = regla.Kit,
                        Ejemplos = g.OrderByDescending(n => n.Puntaje).Take(3).Select(n => n.Titulo).ToList(),
                        RoadmapId = g.Select(n => n.RoadmapId).FirstOrDefault(r => r.HasValue)
                    };
                })
                .OrderByDescending(o => o.Cantidad * 10 + o.CalientesAltas * 15)
                .Take(8)
                .ToList();

            return dto;
        }

        /// <summary>
        /// Precio sugerido de la microapp: la implementación se acerca a lo que el mercado ya ofrece pagar
        /// (sin bajar de la mitad ni pasar de 4 veces la referencia) y la mensualidad es la de referencia.
        /// </summary>
        private static (decimal setup, decimal mensual) PrecioMicroapp(string categoria, decimal? medianaUsd)
        {
            var (setup, mensual) = PreciosMicroapp.TryGetValue(categoria, out var p) ? p : PreciosMicroapp["Software a medida"];
            if (medianaUsd is >= 50)
                setup = Math.Round(Math.Clamp(medianaUsd.Value * 0.8m, setup * 0.5m, setup * 4m) / 10m, 0) * 10m;
            return (setup, mensual);
        }

        // ───────────────────────── Gestión ─────────────────────────

        public async Task<MercadoNecesidadDto> CrearManualAsync(MercadoManualRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Titulo)) throw new InvalidOperationException("El pedido necesita un título");
            var ahora = DateTime.UtcNow;
            var n = new HqMercadoNecesidad
            {
                EmpresaId = request.EmpresaId,
                Fuente = "Manual",
                ExternoId = Guid.NewGuid().ToString("N"),
                Titulo = Recortar(request.Titulo.Trim(), 400)!,
                Descripcion = request.Descripcion,
                Url = Recortar(request.Url, 600),
                Solicitante = Recortar(request.Solicitante, 250),
                Pais = Recortar(string.IsNullOrWhiteSpace(request.Pais) ? "Paraguay" : request.Pais, 60),
                PresupuestoMax = request.PresupuestoMax,
                Moneda = request.PresupuestoMax.HasValue ? (request.Moneda ?? "USD") : null,
                FechaPublicacion = ahora,
                Habilidades = string.IsNullOrWhiteSpace(request.Canal) ? null : $"Canal: {request.Canal}",
                ResponsableNombre = request.ResponsableNombre,
                FechaCaptura = ahora
            };

            var senales = (request.Senales ?? new List<string>()).Where(SenalesManual.ContainsKey).Distinct().ToList();
            n.Confianza = Math.Clamp(35 + senales.Sum(k => SenalesManual[k].Peso), 5, 95);
            n.ConfianzaMotivo = senales.Count == 0
                ? "Sin señales marcadas: verificá al comprador antes de invertir tiempo"
                : Recortar(string.Join(" · ", senales.Select(k => (SenalesManual[k].Peso > 0 ? "✓ " : "⚠ ") + SenalesManual[k].Etiqueta)), 300);
            Enriquecer(n, ahora);
            _db.HqMercadoNecesidades.Add(n);
            await _db.SaveChangesAsync();
            return Map(n);
        }

        public async Task<MercadoNecesidadDto?> ActualizarAsync(int necesidadId, MercadoActualizarRequest request)
        {
            var n = await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == necesidadId && x.EmpresaId == request.EmpresaId && x.Activo);
            if (n == null) return null;
            if (!string.IsNullOrWhiteSpace(request.Estado))
            {
                if (!Estados.Contains(request.Estado)) throw new InvalidOperationException($"Estado inválido: {request.Estado}");
                n.Estado = request.Estado;
            }
            if (request.Notas != null) n.Notas = request.Notas;
            if (request.ResponsableNombre != null) n.ResponsableNombre = request.ResponsableNombre;
            n.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(n);
        }

        /// <summary>Crea el prospecto en el CRM Comercial con todo el contexto del pedido.</summary>
        public async Task<MercadoConversionResultDto> ConvertirACrmAsync(int necesidadId, MercadoConversionRequest request)
        {
            var n = await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == necesidadId && x.EmpresaId == request.EmpresaId && x.Activo)
                    ?? throw new InvalidOperationException("Necesidad no encontrada");
            if (n.ClienteId.HasValue) return new MercadoConversionResultDto { ClienteId = n.ClienteId, Mensaje = "Ya estaba en el CRM" };

            var presupuesto = n.PresupuestoMax.HasValue ? $"Presupuesto: {n.Moneda} {n.PresupuestoMax:N0}\n" : "";
            var cliente = await _crm.CrearClienteAsync(new CrmClienteUpsertRequest
            {
                EmpresaId = request.EmpresaId,
                RazonSocial = Recortar(n.Solicitante is { Length: > 0 } s && !s.StartsWith("Cliente en") ? s : n.Titulo, 200)!,
                Rubro = Recortar(n.Categoria, 100),
                EstadoPipeline = "Prospecto",
                SistemaServicio = Recortar(n.ProductoSugerido, 200),
                ProximosPasos = Recortar(n.Fuente == "DNCP"
                    ? $"Revisar pliego y preparar oferta antes del {n.FechaCierre:dd/MM/yyyy}"
                    : "Contactar con propuesta de implementación rápida sobre Vordia", 500),
                FechaEstimadaCierre = n.FechaCierre,
                Origen = "mercado",
                OrigenDetalle = Recortar($"{n.Fuente} · {n.Url}", 120),
                MensajeInicial = Recortar($"{n.Titulo}\n\n{n.Descripcion}", 4000),
                Notas = $"Detectado en Necesidades del Mercado ({n.Fuente}). Puntaje {n.Puntaje}/100: {n.MotivoPuntaje}\n{presupuesto}{n.Url}",
                UsuarioNombre = request.UsuarioNombre
            });

            n.ClienteId = cliente.ClienteId;
            if (n.Estado == "Nueva") n.Estado = "Evaluando";
            n.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new MercadoConversionResultDto { ClienteId = cliente.ClienteId, Mensaje = "Prospecto creado en el CRM Comercial" };
        }

        public async Task<MercadoConversionResultDto> ConvertirAProductoAsync(int necesidadId, MercadoConversionRequest request)
        {
            var n = await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == necesidadId && x.EmpresaId == request.EmpresaId && x.Activo)
                    ?? throw new InvalidOperationException("Necesidad no encontrada");
            if (n.RoadmapId.HasValue) return new MercadoConversionResultDto { RoadmapId = n.RoadmapId, Mensaje = "Ya tenía una idea en el Roadmap" };
            var regla = Reglas.FirstOrDefault(r => r.Categoria == n.Categoria) ?? SoftwareAMedida;
            var idea = CrearIdea(request.EmpresaId, $"Producto rápido: {n.Categoria}", regla, new[] { n }, request.UsuarioNombre);
            await _db.SaveChangesAsync();
            n.RoadmapId = idea.RoadmapId;
            await _db.SaveChangesAsync();
            return new MercadoConversionResultDto { RoadmapId = idea.RoadmapId, Mensaje = "Idea de producto rápido creada en el Roadmap" };
        }

        /// <summary>Empaqueta una categoría de pedidos repetidos como producto rápido en el Roadmap.</summary>
        public async Task<MercadoConversionResultDto> OportunidadAProductoAsync(MercadoOportunidadProductoRequest request)
        {
            var desde = DateTime.UtcNow.AddDays(-60);
            var grupo = await _db.HqMercadoNecesidades
                .Where(n => n.EmpresaId == request.EmpresaId && n.Activo && n.Estado != "Descartada" && n.Categoria == request.Categoria
                            && (n.FechaPublicacion ?? n.FechaCaptura) >= desde && n.Puntaje >= 30)
                .OrderByDescending(n => n.Puntaje).Take(30).ToListAsync();
            if (grupo.Count == 0) throw new InvalidOperationException("No hay pedidos en esa categoría");
            var existente = grupo.Select(n => n.RoadmapId).FirstOrDefault(r => r.HasValue);
            if (existente.HasValue) return new MercadoConversionResultDto { RoadmapId = existente, Mensaje = "Ese producto ya está en el Roadmap" };

            var regla = Reglas.FirstOrDefault(r => r.Categoria == request.Categoria) ?? SoftwareAMedida;
            var idea = CrearIdea(request.EmpresaId, $"Producto rápido: {request.Categoria} ({grupo.Count} pedidos)", regla, grupo, request.UsuarioNombre);
            await _db.SaveChangesAsync();
            foreach (var n in grupo) n.RoadmapId = idea.RoadmapId;
            await _db.SaveChangesAsync();
            return new MercadoConversionResultDto { RoadmapId = idea.RoadmapId, Mensaje = $"Producto rápido creado en el Roadmap con {grupo.Count} pedidos de respaldo" };
        }

        private CrmRoadmap CrearIdea(int empresaId, string titulo, Regla regla, IEnumerable<HqMercadoNecesidad> respaldo, string? usuario)
        {
            var lista = respaldo.ToList();
            var presupuestos = lista.Where(n => n.Moneda == "USD" && n.PresupuestoMax >= 50).Select(n => n.PresupuestoMax!.Value).OrderBy(x => x).ToList();
            decimal? mediana = presupuestos.Count > 0 ? presupuestos[presupuestos.Count / 2] : null;
            var (setup, mensual) = PrecioMicroapp(regla.Categoria, mediana);
            var precio = (mediana.HasValue ? $"Presupuesto mediano del mercado: USD {mediana:N0}.\n" : "")
                         + $"Precio sugerido de la microapp: USD {setup:N0} de implementación + USD {mensual:N0}/mes (10 clientes = USD {mensual * 10:N0}/mes recurrente).\n";
            var ejemplos = string.Join("\n", lista.Take(8).Select(n => $"- [{n.Fuente}] {n.Titulo}{(n.Url != null ? " — " + n.Url : "")}"));
            var idea = new CrmRoadmap
            {
                EmpresaId = empresaId,
                Tipo = "Idea",
                Titulo = Recortar(titulo, 200)!,
                Descripcion = $"{regla.Kit}\nBase sugerida: {regla.Producto}.\n{precio}\nPedidos detectados que lo respaldan:\n{ejemplos}",
                Estado = "Priorizado",
                Prioridad = lista.Count >= 5 || lista.Any(n => n.Puntaje >= 70) ? "P1" : "P2",
                ResponsableNombre = Recortar(usuario, 150),
                FechaRegistro = DateTime.UtcNow.Date,
                FechaMeta = DateTime.UtcNow.Date.AddDays(14),
                ProximoPaso = "Armar demo/plantilla, precio cerrado y pitch; ofrecerlo a los pedidos de respaldo",
                FechaProximoPaso = DateTime.UtcNow.Date.AddDays(3),
                Notas = "Generado desde Necesidades del Mercado",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _db.CrmRoadmap.Add(idea);
            return idea;
        }

        // ───────────────────────── Fuentes ─────────────────────────

        public async Task<List<MercadoFuenteDto>> ListarFuentesAsync(int empresaId)
        {
            var fuentes = await _db.HqMercadoFuentes.AsNoTracking().Where(f => f.EmpresaId == empresaId).OrderBy(f => f.Tipo).ThenBy(f => f.Nombre).ToListAsync();
            var conteo = await _db.HqMercadoNecesidades.AsNoTracking().Where(n => n.EmpresaId == empresaId && n.Activo)
                .GroupBy(n => n.Fuente).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
            return fuentes.Select(f => new MercadoFuenteDto
            {
                FuenteId = f.FuenteId,
                Tipo = f.Tipo,
                Nombre = f.Nombre,
                Consulta = f.Consulta,
                Activa = f.Activa,
                UltimaSync = f.UltimaSync,
                UltimoResultado = f.UltimoResultado,
                Necesidades = conteo.GetValueOrDefault(f.Tipo)
            }).ToList();
        }

        public async Task<MercadoFuenteDto> GuardarFuenteAsync(MercadoFuenteRequest request, int? fuenteId = null)
        {
            if (request.Tipo is not ("Freelancer" or "DNCP" or "SECOP" or "BancoMundial" or "PNCP" or "ContractsFinder" or "SEACE" or "ARCE" or "PNUD" or "MercadoPublico" or "RSS")) throw new InvalidOperationException("Tipo de fuente inválido");
            if (string.IsNullOrWhiteSpace(request.Nombre)) throw new InvalidOperationException("La fuente necesita un nombre");
            if (request.Tipo == "RSS" && !Uri.TryCreate(request.Consulta ?? "", UriKind.Absolute, out _))
                throw new InvalidOperationException("Pegá la URL completa del feed RSS (por ejemplo, de Google Alerts)");
            if (request.Tipo == "Freelancer" && string.IsNullOrWhiteSpace(request.Consulta))
                throw new InvalidOperationException("Indicá las palabras clave a buscar");

            HqMercadoFuente f;
            if (fuenteId.HasValue)
            {
                f = await _db.HqMercadoFuentes.FirstOrDefaultAsync(x => x.FuenteId == fuenteId && x.EmpresaId == request.EmpresaId)
                    ?? throw new InvalidOperationException("Fuente no encontrada");
            }
            else
            {
                f = new HqMercadoFuente { EmpresaId = request.EmpresaId, FechaCreacion = DateTime.UtcNow };
                _db.HqMercadoFuentes.Add(f);
            }
            f.Tipo = request.Tipo;
            f.Nombre = Recortar(request.Nombre.Trim(), 120)!;
            f.Consulta = Recortar(request.Consulta?.Trim(), 500);
            f.Activa = request.Activa;
            await _db.SaveChangesAsync();
            return new MercadoFuenteDto { FuenteId = f.FuenteId, Tipo = f.Tipo, Nombre = f.Nombre, Consulta = f.Consulta, Activa = f.Activa, UltimaSync = f.UltimaSync, UltimoResultado = f.UltimoResultado };
        }

        public async Task<bool> EliminarFuenteAsync(int fuenteId, int empresaId)
        {
            var f = await _db.HqMercadoFuentes.FirstOrDefaultAsync(x => x.FuenteId == fuenteId && x.EmpresaId == empresaId);
            if (f == null) return false;
            _db.HqMercadoFuentes.Remove(f);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
