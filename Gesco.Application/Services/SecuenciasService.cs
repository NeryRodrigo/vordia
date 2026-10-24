using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class SecuenciaRequest
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Producto { get; set; } = "";
        public string Oferta { get; set; } = "";
        public string Firma { get; set; } = "";
        public string? ResponderA { get; set; }
        public int Pasos { get; set; } = 3;
        public int DiasEntrePasos { get; set; } = 3;
        public int LimiteDiario { get; set; } = 30;
    }

    public class ContactoManualRequest
    {
        public int EmpresaId { get; set; }
        public string Email { get; set; } = "";
        public string? Nombre { get; set; }
        public string? Empresa { get; set; }
        public string? Contexto { get; set; }
    }

    public class SecuenciaResumenDto
    {
        public HqSecuencia Secuencia { get; set; } = new();
        public Dictionary<string, int> PorEstado { get; set; } = new();
        public int EnviadosHoy { get; set; }
        public int EnviadosTotal { get; set; }
    }

    public record ProcesoResultado(int Enviados, int Simulados, int Errores, int Bajas, string? Motivo);

    public interface ISecuenciasService
    {
        Task<List<SecuenciaResumenDto>> ListarAsync(int empresaId);
        Task<HqSecuencia> GuardarAsync(SecuenciaRequest r, int? secuenciaId = null);
        Task<bool> ActivarAsync(int secuenciaId, int empresaId, bool activa);
        Task<List<HqSecuenciaContacto>> ContactosAsync(int secuenciaId, int empresaId);
        Task<int> ImportarAsync(int secuenciaId, int empresaId, string origen, int max);
        Task<HqSecuenciaContacto> AgregarManualAsync(int secuenciaId, ContactoManualRequest r);
        Task<int> RedactarAsync(int secuenciaId, int empresaId, int max, CancellationToken ct = default);
        Task<HqSecuenciaContacto?> EditarBorradorAsync(int contactoId, int empresaId, string asunto, string cuerpo);
        Task<HqSecuenciaContacto?> CambiarEstadoAsync(int contactoId, int empresaId, string estado);
        Task<int> AprobarTodosAsync(int secuenciaId, int empresaId);
        Task<ProcesoResultado> ProcesarAsync(int? empresaId = null, CancellationToken ct = default);
        bool IaConfigurada { get; }
        string IaProveedores { get; }
    }

    /// <summary>
    /// Secuencias de correo comercial de VORDIA HQ. El primer correo lo redacta la IA y lo aprueba una persona;
    /// los seguimientos salen solos, en horario hábil de Paraguay, con límite diario por secuencia,
    /// link de baja firmado y corte inmediato si el contacto pidió la baja o respondió.
    /// </summary>
    public class SecuenciasService : ISecuenciasService
    {
        private static readonly string[] Productos = { "HABITAT", "STAY", "DINE", "RETAIL", "BOUTIQUE", "OPS", "AGRO" };
        /// <summary>Por pasada del motor (cada 15 minutos) para repartir los envíos en el día.</summary>
        private const int EnviosPorPasada = 5;

        private readonly IApplicationDbContext _db;
        private readonly IEmailService _email;
        private readonly IMercadoVentasService _ventas;
        private readonly IRedaccionIaService _ia;
        private readonly bool _modoPrueba;

        public SecuenciasService(IApplicationDbContext db, IEmailService email, IMercadoVentasService ventas, IRedaccionIaService ia, IConfiguration config)
        {
            _db = db;
            _email = email;
            _ventas = ventas;
            _ia = ia;
            _modoPrueba = string.Equals(config["Secuencias:ModoPrueba"], "true", StringComparison.OrdinalIgnoreCase);
        }

        public bool IaConfigurada => _ia.Configurada;
        public string IaProveedores => _ia.Proveedores;

        private static string? Corto(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);
        private static bool EmailValido(string? e) => !string.IsNullOrWhiteSpace(e) && e.Contains('@') && e.Contains('.') && e.Length <= 150;

        public async Task<List<SecuenciaResumenDto>> ListarAsync(int empresaId)
        {
            var hoy = InicioDiaUtc(DateTime.UtcNow);
            var secuencias = await _db.HqSecuencias.AsNoTracking().Where(s => s.EmpresaId == empresaId).OrderByDescending(s => s.FechaCreacion).ToListAsync();
            var ids = secuencias.Select(s => s.SecuenciaId).ToList();
            var estados = await _db.HqSecuenciaContactos.AsNoTracking().Where(c => ids.Contains(c.SecuenciaId))
                .GroupBy(c => new { c.SecuenciaId, c.Estado }).Select(g => new { g.Key.SecuenciaId, g.Key.Estado, N = g.Count() }).ToListAsync();
            var envios = await _db.HqSecuenciaEnvios.AsNoTracking().Where(e => ids.Contains(e.SecuenciaId) && e.Resultado != "Error")
                .GroupBy(e => e.SecuenciaId).Select(g => new { Id = g.Key, Total = g.Count(), Hoy = g.Count(e => e.Fecha >= hoy) }).ToListAsync();
            return secuencias.Select(s => new SecuenciaResumenDto
            {
                Secuencia = s,
                PorEstado = estados.Where(x => x.SecuenciaId == s.SecuenciaId).ToDictionary(x => x.Estado, x => x.N),
                EnviadosHoy = envios.FirstOrDefault(e => e.Id == s.SecuenciaId)?.Hoy ?? 0,
                EnviadosTotal = envios.FirstOrDefault(e => e.Id == s.SecuenciaId)?.Total ?? 0
            }).ToList();
        }

        public async Task<HqSecuencia> GuardarAsync(SecuenciaRequest r, int? secuenciaId = null)
        {
            var producto = (r.Producto ?? "").Trim().ToUpperInvariant();
            if (!Productos.Contains(producto)) throw new InvalidOperationException("Producto inválido");
            if (string.IsNullOrWhiteSpace(r.Nombre) || string.IsNullOrWhiteSpace(r.Oferta) || string.IsNullOrWhiteSpace(r.Firma))
                throw new InvalidOperationException("Nombre, oferta y firma son obligatorios");
            if (!string.IsNullOrWhiteSpace(r.ResponderA) && !EmailValido(r.ResponderA)) throw new InvalidOperationException("El correo para respuestas no es válido");

            var s = secuenciaId.HasValue
                ? await _db.HqSecuencias.FirstOrDefaultAsync(x => x.SecuenciaId == secuenciaId && x.EmpresaId == r.EmpresaId) ?? throw new KeyNotFoundException()
                : new HqSecuencia { EmpresaId = r.EmpresaId, FechaCreacion = DateTime.UtcNow };
            s.Nombre = Corto(r.Nombre, 120)!;
            s.Producto = producto;
            s.Oferta = Corto(r.Oferta, 1000)!;
            s.Firma = Corto(r.Firma, 120)!;
            s.ResponderA = Corto(r.ResponderA, 150);
            s.Pasos = Math.Clamp(r.Pasos, 1, 4);
            s.DiasEntrePasos = Math.Clamp(r.DiasEntrePasos, 2, 14);
            // Máximo prudente por casilla para no caer en spam
            s.LimiteDiario = Math.Clamp(r.LimiteDiario, 1, 50);
            if (!secuenciaId.HasValue) _db.HqSecuencias.Add(s);
            await _db.SaveChangesAsync();
            return s;
        }

        public async Task<bool> ActivarAsync(int secuenciaId, int empresaId, bool activa)
        {
            var s = await _db.HqSecuencias.FirstOrDefaultAsync(x => x.SecuenciaId == secuenciaId && x.EmpresaId == empresaId);
            if (s == null) return false;
            s.Activa = activa;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<List<HqSecuenciaContacto>> ContactosAsync(int secuenciaId, int empresaId) =>
            _db.HqSecuenciaContactos.AsNoTracking()
                .Where(c => c.SecuenciaId == secuenciaId && c.EmpresaId == empresaId)
                .OrderBy(c => c.Estado == "Redactado" ? 0 : c.Estado == "Borrador" ? 1 : 2).ThenByDescending(c => c.FechaAlta)
                .Take(1000).ToListAsync();

        private async Task<HashSet<string>> ExcluidosAsync(int secuenciaId, int empresaId)
        {
            var bajas = await _db.HqMercadoBajas.Where(b => b.EmpresaId == empresaId).Select(b => b.Email).ToListAsync();
            var ya = await _db.HqSecuenciaContactos.Where(c => c.SecuenciaId == secuenciaId).Select(c => c.Email).ToListAsync();
            return new HashSet<string>(bajas.Concat(ya).Select(e => e.ToLowerInvariant()));
        }

        /// <summary>Trae contactos con correo desde el CRM (prospectos del producto) o desde Necesidades del Mercado.</summary>
        public async Task<int> ImportarAsync(int secuenciaId, int empresaId, string origen, int max)
        {
            var s = await _db.HqSecuencias.AsNoTracking().FirstOrDefaultAsync(x => x.SecuenciaId == secuenciaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException();
            var excluidos = await ExcluidosAsync(secuenciaId, empresaId);
            max = Math.Clamp(max, 1, 200);
            var nuevos = new List<HqSecuenciaContacto>();

            if (origen == "crm")
            {
                var clientes = await _db.CrmClientes.AsNoTracking()
                    .Where(c => c.EmpresaId == empresaId && c.Activo && (c.EstadoPipeline == "Prospecto" || c.EstadoPipeline == "Contactado")
                                && (c.ContactoEmail != null || c.Email != null)
                                && (c.OrigenDetalle == s.Producto || c.SistemaServicio!.Contains(s.Producto)))
                    .OrderByDescending(c => c.FechaCreacion).Take(max * 3).ToListAsync();
                foreach (var c in clientes)
                {
                    var email = (c.ContactoEmail ?? c.Email ?? "").Trim();
                    if (!EmailValido(email) || !excluidos.Add(email.ToLowerInvariant())) continue;
                    nuevos.Add(new HqSecuenciaContacto
                    {
                        EmpresaId = empresaId, SecuenciaId = secuenciaId, ClienteId = c.ClienteId, Email = email,
                        Nombre = Corto(c.ContactoNombre, 150), Empresa = Corto(c.RazonSocial, 200),
                        Contexto = Corto(string.Join(" · ", new[] { c.SistemaServicio, c.MensajeInicial, c.Rubro }.Where(x => !string.IsNullOrWhiteSpace(x))), 2000)
                    });
                    if (nuevos.Count >= max) break;
                }
            }
            else if (origen == "mercado")
            {
                // Solo pedidos privados del producto (cargados a mano o por alertas). Las licitaciones públicas
                // se responden con la consulta formal de Necesidades del Mercado, no con una secuencia comercial.
                var necesidades = await _db.HqMercadoNecesidades.AsNoTracking()
                    .Where(n => n.EmpresaId == empresaId && n.Activo && n.ContactoEmail != null
                                && (n.Fuente == "Manual" || n.Fuente == "RSS") && n.Vertical == s.Producto
                                && n.Estado != "Descartada" && n.Estado != "Ganada" && n.Estado != "Perdida")
                    .OrderByDescending(n => n.Puntaje).Take(max * 3).ToListAsync();
                foreach (var n in necesidades)
                {
                    var email = n.ContactoEmail!.Trim();
                    if (!EmailValido(email) || !excluidos.Add(email.ToLowerInvariant())) continue;
                    nuevos.Add(new HqSecuenciaContacto
                    {
                        EmpresaId = empresaId, SecuenciaId = secuenciaId, NecesidadId = n.NecesidadId, Email = email,
                        Nombre = Corto(n.ContactoNombre, 150), Empresa = Corto(n.Solicitante, 200),
                        Contexto = Corto($"Publicó el pedido o llamado: \"{n.Titulo}\"" + (n.Pais != null ? $" ({n.Pais})" : ""), 2000)
                    });
                    if (nuevos.Count >= max) break;
                }
            }
            else throw new InvalidOperationException("Origen inválido");

            _db.HqSecuenciaContactos.AddRange(nuevos);
            await _db.SaveChangesAsync();
            return nuevos.Count;
        }

        public async Task<HqSecuenciaContacto> AgregarManualAsync(int secuenciaId, ContactoManualRequest r)
        {
            if (!await _db.HqSecuencias.AnyAsync(x => x.SecuenciaId == secuenciaId && x.EmpresaId == r.EmpresaId)) throw new KeyNotFoundException();
            var email = (r.Email ?? "").Trim();
            if (!EmailValido(email)) throw new InvalidOperationException("Correo inválido");
            if ((await ExcluidosAsync(secuenciaId, r.EmpresaId)).Contains(email.ToLowerInvariant()))
                throw new InvalidOperationException("Ese correo ya está en la secuencia o pidió no recibir más correos");
            var c = new HqSecuenciaContacto
            {
                EmpresaId = r.EmpresaId, SecuenciaId = secuenciaId, Email = email,
                Nombre = Corto(r.Nombre, 150), Empresa = Corto(r.Empresa, 200), Contexto = Corto(r.Contexto, 2000)
            };
            _db.HqSecuenciaContactos.Add(c);
            await _db.SaveChangesAsync();
            return c;
        }

        /// <summary>La IA redacta el primer correo de los contactos en borrador; quedan para revisar y aprobar.</summary>
        public async Task<int> RedactarAsync(int secuenciaId, int empresaId, int max, CancellationToken ct = default)
        {
            var s = await _db.HqSecuencias.AsNoTracking().FirstOrDefaultAsync(x => x.SecuenciaId == secuenciaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException();
            var pendientes = await _db.HqSecuenciaContactos.Where(c => c.SecuenciaId == secuenciaId && c.EmpresaId == empresaId && c.Estado == "Borrador")
                .OrderBy(c => c.FechaAlta).Take(Math.Clamp(max, 1, 20)).ToListAsync(ct);
            foreach (var c in pendientes)
            {
                var r = await _ia.RedactarAsync(new DatosRedaccion(s.Producto, s.Oferta, s.Firma, 1, s.Pasos, c.Nombre, c.Empresa, c.Contexto, null), ct);
                c.Asunto = r.Asunto;
                c.Cuerpo = r.Cuerpo;
                c.Estado = "Redactado";
                c.Detalle = r.ConIa ? "Redactado con IA" : "Plantilla (IA no disponible)";
            }
            await _db.SaveChangesAsync(ct);
            return pendientes.Count;
        }

        public async Task<HqSecuenciaContacto?> EditarBorradorAsync(int contactoId, int empresaId, string asunto, string cuerpo)
        {
            var c = await _db.HqSecuenciaContactos.FirstOrDefaultAsync(x => x.ContactoId == contactoId && x.EmpresaId == empresaId);
            if (c == null) return null;
            if (c.Estado is not ("Redactado" or "Aprobado" or "Borrador")) throw new InvalidOperationException("Este contacto ya está en curso: no se puede editar el primer correo");
            if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(cuerpo)) throw new InvalidOperationException("Asunto y cuerpo son obligatorios");
            c.Asunto = Corto(asunto, 200);
            c.Cuerpo = cuerpo.Trim();
            if (c.Estado == "Borrador") c.Estado = "Redactado";
            await _db.SaveChangesAsync();
            return c;
        }

        public async Task<HqSecuenciaContacto?> CambiarEstadoAsync(int contactoId, int empresaId, string estado)
        {
            var c = await _db.HqSecuenciaContactos.FirstOrDefaultAsync(x => x.ContactoId == contactoId && x.EmpresaId == empresaId);
            if (c == null) return null;
            switch (estado)
            {
                case "Aprobado":
                    if (string.IsNullOrWhiteSpace(c.Asunto) || string.IsNullOrWhiteSpace(c.Cuerpo)) throw new InvalidOperationException("Primero hay que redactar el correo");
                    if (c.Estado is not ("Redactado" or "Aprobado")) throw new InvalidOperationException("Solo se aprueban correos redactados");
                    c.Estado = "Aprobado";
                    c.ProximoEnvio = DateTime.UtcNow;
                    break;
                case "Descartado":
                    c.Estado = "Descartado";
                    c.ProximoEnvio = null;
                    break;
                case "Respondio":
                    c.Estado = "Respondio";
                    c.ProximoEnvio = null;
                    await MarcarRespuestaEnCrmAsync(c);
                    break;
                default:
                    throw new InvalidOperationException("Estado inválido");
            }
            await _db.SaveChangesAsync();
            return c;
        }

        public async Task<int> AprobarTodosAsync(int secuenciaId, int empresaId)
        {
            var redactados = await _db.HqSecuenciaContactos.Where(c => c.SecuenciaId == secuenciaId && c.EmpresaId == empresaId && c.Estado == "Redactado").ToListAsync();
            foreach (var c in redactados)
            {
                c.Estado = "Aprobado";
                c.ProximoEnvio = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return redactados.Count;
        }

        private async Task MarcarRespuestaEnCrmAsync(HqSecuenciaContacto c)
        {
            if (c.ClienteId is not int clienteId) return;
            var cliente = await _db.CrmClientes.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.EmpresaId == c.EmpresaId);
            if (cliente == null) return;
            if (cliente.EstadoPipeline == "Prospecto") cliente.EstadoPipeline = "Contactado";
            cliente.ProximosPasos = "Respondió la secuencia de correo: coordinar demo";
            cliente.FechaActualizacion = DateTime.UtcNow;
            _db.CrmHistorial.Add(new CrmHistorial
            {
                ClienteId = clienteId, EmpresaId = c.EmpresaId, Tipo = "email",
                Titulo = "Respondió la secuencia de correo", Detalle = $"Correo: {c.Email}",
                Fecha = DateTime.UtcNow, UsuarioNombre = "Secuencias HQ"
            });
        }

        // ───────────── Motor de envío ─────────────

        private static TimeZoneInfo ZonaPy()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Asuncion"); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("PY", TimeSpan.FromHours(-3), "Paraguay", "Paraguay");
            }
        }

        private static DateTime InicioDiaUtc(DateTime utc)
        {
            var zona = ZonaPy();
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zona).Date;
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona);
        }

        /// <summary>Lunes a viernes de 8 a 18, hora de Paraguay.</summary>
        public static bool HorarioHabil(DateTime utc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaPy());
            return local.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && local.Hour >= 8 && local.Hour < 18;
        }

        public async Task<ProcesoResultado> ProcesarAsync(int? empresaId = null, CancellationToken ct = default)
        {
            var ahora = DateTime.UtcNow;
            if (!HorarioHabil(ahora)) return new ProcesoResultado(0, 0, 0, 0, "Fuera de horario: se envía de lunes a viernes de 8 a 18 (hora de Paraguay)");

            int enviados = 0, simulados = 0, errores = 0, bajas = 0;
            var hoy = InicioDiaUtc(ahora);
            var secuencias = await _db.HqSecuencias.Where(s => s.Activa && (empresaId == null || s.EmpresaId == empresaId)).ToListAsync(ct);

            foreach (var s in secuencias)
            {
                var yaHoy = await _db.HqSecuenciaEnvios.CountAsync(e => e.SecuenciaId == s.SecuenciaId && e.Fecha >= hoy && e.Resultado != "Error", ct);
                var cupo = Math.Min(EnviosPorPasada, s.LimiteDiario - yaHoy);
                if (cupo <= 0) continue;

                var debidos = await _db.HqSecuenciaContactos
                    .Where(c => c.SecuenciaId == s.SecuenciaId && (c.Estado == "Aprobado" || c.Estado == "EnCurso") && c.ProximoEnvio != null && c.ProximoEnvio <= ahora)
                    .OrderBy(c => c.ProximoEnvio).Take(cupo).ToListAsync(ct);

                foreach (var c in debidos)
                {
                    var email = c.Email.Trim().ToLowerInvariant();
                    if (await _db.HqMercadoBajas.AnyAsync(b => b.EmpresaId == s.EmpresaId && b.Email == email, ct))
                    {
                        c.Estado = "Baja";
                        c.ProximoEnvio = null;
                        bajas++;
                        continue;
                    }

                    var paso = c.PasoActual + 1;
                    string asunto, cuerpo;
                    if (paso == 1)
                    {
                        asunto = c.Asunto!;
                        cuerpo = c.Cuerpo!;
                    }
                    else
                    {
                        var r = await _ia.RedactarAsync(new DatosRedaccion(s.Producto, s.Oferta, s.Firma, paso, s.Pasos, c.Nombre, c.Empresa, c.Contexto, c.Asunto), ct);
                        asunto = r.Asunto;
                        cuerpo = r.Cuerpo;
                    }
                    var cuerpoFinal = $"{cuerpo}\n\n{s.Firma}\nVORDIA";

                    string resultado;
                    string? detalle = null;
                    if (_modoPrueba)
                    {
                        resultado = "Simulado";
                        simulados++;
                    }
                    else
                    {
                        var (ok, error) = await _email.EnviarCorreoComercialAsync(c.Email, asunto, cuerpoFinal, s.ResponderA, _ventas.UrlBaja(s.EmpresaId, c.Email));
                        resultado = ok ? "Enviado" : "Error";
                        detalle = error;
                        if (ok) enviados++; else errores++;
                    }

                    _db.HqSecuenciaEnvios.Add(new HqSecuenciaEnvio
                    {
                        EmpresaId = s.EmpresaId, SecuenciaId = s.SecuenciaId, ContactoId = c.ContactoId, Paso = paso,
                        Asunto = asunto.Length <= 200 ? asunto : asunto[..200], Cuerpo = cuerpoFinal,
                        Resultado = resultado, Detalle = Corto(detalle, 300), Fecha = ahora
                    });

                    if (resultado == "Error")
                    {
                        c.Estado = "Error";
                        c.Detalle = Corto(detalle, 300);
                        c.ProximoEnvio = null;
                        continue;
                    }

                    c.PasoActual = paso;
                    c.UltimoEnvio = ahora;
                    if (paso == 1) c.Detalle = null;
                    if (paso >= s.Pasos)
                    {
                        c.Estado = "Terminado";
                        c.ProximoEnvio = null;
                    }
                    else
                    {
                        c.Estado = "EnCurso";
                        c.ProximoEnvio = ahora.AddDays(s.DiasEntrePasos);
                    }

                    if (c.ClienteId is int clienteId && paso == 1)
                    {
                        _db.CrmHistorial.Add(new CrmHistorial
                        {
                            ClienteId = clienteId, EmpresaId = s.EmpresaId, Tipo = "email",
                            Titulo = $"Secuencia «{s.Nombre}»: primer correo",
                            Detalle = asunto, Fecha = ahora, UsuarioNombre = "Secuencias HQ"
                        });
                    }
                    if (c.NecesidadId is int necesidadId && paso == 1)
                    {
                        var n = await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == necesidadId, ct);
                        if (n != null && n.Estado is "Nueva" or "Evaluando") n.Estado = "Contactada";
                    }
                }
                await _db.SaveChangesAsync(ct);
            }
            return new ProcesoResultado(enviados, simulados, errores, bajas, null);
        }
    }
}
