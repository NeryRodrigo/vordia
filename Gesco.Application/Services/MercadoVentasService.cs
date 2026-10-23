using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    /// <summary>
    /// VORDIA HQ — de la necesidad detectada a la venta: datos de contacto, mensajes por WhatsApp
    /// o correo, y pedidos rápidos con link público para que el cliente acepte desde el celular.
    /// </summary>
    public class MercadoVentasService : IMercadoVentasService
    {
        private static readonly string[] CanalesValidos = { "WhatsApp", "Correo", "Plataforma", "Llamada" };
        private static readonly string[] EstadosAbiertos = { "Borrador", "Enviado", "Visto" };

        private readonly IApplicationDbContext _db;
        private readonly IEmailService _email;
        private readonly ICrmService _crm;

        private readonly IConfiguration _config;

        public MercadoVentasService(IApplicationDbContext db, IEmailService email, ICrmService crm, IConfiguration config)
        {
            _config = config;
            _db = db;
            _email = email;
            _crm = crm;
        }

        private static string? Limpio(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);

        private async Task<HqMercadoNecesidad> NecesidadAsync(int necesidadId, int empresaId) =>
            await _db.HqMercadoNecesidades.FirstOrDefaultAsync(n => n.NecesidadId == necesidadId && n.EmpresaId == empresaId && n.Activo)
            ?? throw new InvalidOperationException("Necesidad no encontrada");

        /// <summary>El primer contacto mueve la necesidad a "Contactada" (sin retroceder estados más avanzados).</summary>
        private static void MarcarContactada(HqMercadoNecesidad n)
        {
            if (n.Estado is "Nueva" or "Evaluando") n.Estado = "Contactada";
            n.FechaActualizacion = DateTime.UtcNow;
        }

        private static MercadoContactoDto Map(HqMercadoContacto c) => new()
        {
            ContactoId = c.ContactoId,
            Canal = c.Canal,
            Destino = c.Destino,
            Asunto = c.Asunto,
            Mensaje = c.Mensaje,
            Resultado = c.Resultado,
            Detalle = c.Detalle,
            UsuarioNombre = c.UsuarioNombre,
            Fecha = c.Fecha
        };

        // ───────────────────────── Contacto ─────────────────────────

        public async Task<MercadoNecesidadDto?> GuardarContactoAsync(int necesidadId, MercadoContactoInfoRequest request)
        {
            var n = await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == necesidadId && x.EmpresaId == request.EmpresaId && x.Activo);
            if (n == null) return null;
            n.ContactoNombre = Limpio(request.ContactoNombre, 150);
            n.ContactoEmail = Limpio(request.ContactoEmail, 150);
            n.ContactoTelefono = Limpio(request.ContactoTelefono, 40);
            if (n.ContactoEmail != null || n.ContactoTelefono != null)
                n.ContactoOrigen ??= "Cargado a mano";
            n.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MercadoService.Map(n);
        }

        public async Task<List<MercadoContactoDto>> ListarContactosAsync(int necesidadId, int empresaId)
        {
            var list = await _db.HqMercadoContactos.AsNoTracking()
                .Where(c => c.NecesidadId == necesidadId && c.EmpresaId == empresaId)
                .OrderByDescending(c => c.Fecha).Take(50).ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<MercadoContactoDto> RegistrarContactoAsync(int necesidadId, MercadoContactoRegistroRequest request)
        {
            var canal = CanalesValidos.FirstOrDefault(c => c.Equals(request.Canal, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Canal inválido");
            var n = await NecesidadAsync(necesidadId, request.EmpresaId);
            var c = new HqMercadoContacto
            {
                EmpresaId = request.EmpresaId,
                NecesidadId = n.NecesidadId,
                Canal = canal,
                Destino = Limpio(request.Destino, 150),
                Asunto = Limpio(request.Asunto, 200),
                Mensaje = request.Mensaje,
                Resultado = "Registrado",
                UsuarioNombre = Limpio(request.UsuarioNombre, 150),
                Fecha = DateTime.UtcNow
            };
            _db.HqMercadoContactos.Add(c);
            MarcarContactada(n);
            await _db.SaveChangesAsync();
            return Map(c);
        }

        public async Task<MercadoContactoDto> EnviarCorreoAsync(int necesidadId, MercadoCorreoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Para) || !request.Para.Contains('@')) throw new InvalidOperationException("Indicá un correo válido");
            if (string.IsNullOrWhiteSpace(request.Asunto) || string.IsNullOrWhiteSpace(request.Mensaje)) throw new InvalidOperationException("Asunto y mensaje son obligatorios");
            var n = await NecesidadAsync(necesidadId, request.EmpresaId);
            var para = request.Para.Trim();
            if (await _db.HqMercadoBajas.AnyAsync(b => b.EmpresaId == request.EmpresaId && b.Email == para.ToLower()))
                throw new InvalidOperationException("Este contacto pidió no recibir más correos comerciales: no se puede enviar.");

            var bajaUrl = $"{SitioPublico}/baja/{TokenBaja(request.EmpresaId, para)}";
            var (enviado, error) = await _email.EnviarCorreoComercialAsync(para, request.Asunto.Trim(), request.Mensaje, request.ResponderA, bajaUrl);
            var c = new HqMercadoContacto
            {
                EmpresaId = request.EmpresaId,
                NecesidadId = n.NecesidadId,
                Canal = "Correo",
                Destino = Limpio(request.Para, 150),
                Asunto = Limpio(request.Asunto, 200),
                Mensaje = request.Mensaje,
                Resultado = enviado ? "Enviado" : "Error",
                Detalle = Limpio(error, 300),
                UsuarioNombre = Limpio(request.UsuarioNombre, 150),
                Fecha = DateTime.UtcNow
            };
            _db.HqMercadoContactos.Add(c);
            if (enviado)
            {
                n.ContactoEmail ??= Limpio(request.Para, 150);
                MarcarContactada(n);
                if (request.PedidoId.HasValue)
                {
                    var p = await _db.HqMercadoPedidos.FirstOrDefaultAsync(x => x.PedidoId == request.PedidoId && x.EmpresaId == request.EmpresaId && x.Activo);
                    if (p != null) await MarcarEnviadoAsync(p, n, request.UsuarioNombre);
                }
            }
            await _db.SaveChangesAsync();
            return Map(c);
        }

        // ───────────────────────── Pedidos ─────────────────────────

        private static MercadoPedidoDto Map(HqMercadoPedido p, string? necesidadTitulo = null) => new()
        {
            PedidoId = p.PedidoId,
            NecesidadId = p.NecesidadId,
            NecesidadTitulo = necesidadTitulo,
            Token = p.Token,
            Numero = p.Numero,
            ClienteNombre = p.ClienteNombre,
            ClienteEmail = p.ClienteEmail,
            ClienteTelefono = p.ClienteTelefono,
            Producto = p.Producto,
            Incluye = p.Incluye,
            Moneda = p.Moneda,
            PrecioSetup = p.PrecioSetup,
            PrecioMensual = p.PrecioMensual,
            PlazoDias = p.PlazoDias,
            ValidoHasta = p.ValidoHasta,
            CondicionesPago = p.CondicionesPago,
            Estado = p.Estado,
            FechaEnvio = p.FechaEnvio,
            FechaVisto = p.FechaVisto,
            FechaRespuesta = p.FechaRespuesta,
            RespuestaNombre = p.RespuestaNombre,
            RespuestaComentario = p.RespuestaComentario,
            FechaPago = p.FechaPago,
            FechaCreacion = p.FechaCreacion
        };

        private static bool Vencido(HqMercadoPedido p) => EstadosAbiertos.Contains(p.Estado) && p.ValidoHasta < DateTime.UtcNow;

        public async Task<MercadoPedidoDto> CrearPedidoAsync(MercadoPedidoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClienteNombre)) throw new InvalidOperationException("Indicá a quién va el pedido");
            if (string.IsNullOrWhiteSpace(request.Producto)) throw new InvalidOperationException("Indicá el producto");
            if (request.PrecioSetup < 0 || request.PrecioMensual < 0 || request.PrecioSetup + request.PrecioMensual <= 0)
                throw new InvalidOperationException("El pedido necesita un precio");

            HqMercadoNecesidad? n = null;
            if (request.NecesidadId.HasValue) n = await NecesidadAsync(request.NecesidadId.Value, request.EmpresaId);

            var ahora = DateTime.UtcNow;
            var anio = ahora.Year;
            var delAnio = await _db.HqMercadoPedidos.CountAsync(x => x.EmpresaId == request.EmpresaId && x.FechaCreacion.Year == anio);
            var p = new HqMercadoPedido
            {
                EmpresaId = request.EmpresaId,
                NecesidadId = n?.NecesidadId,
                Token = Guid.NewGuid().ToString("N"),
                Numero = $"VOR-{anio}-{delAnio + 1:0000}",
                ClienteNombre = Limpio(request.ClienteNombre, 200)!,
                ClienteEmail = Limpio(request.ClienteEmail, 150),
                ClienteTelefono = Limpio(request.ClienteTelefono, 40),
                Producto = Limpio(request.Producto, 200)!,
                Incluye = request.Incluye?.Trim(),
                Moneda = Limpio(request.Moneda, 10) ?? "USD",
                PrecioSetup = Math.Round(request.PrecioSetup, 2),
                PrecioMensual = Math.Round(request.PrecioMensual, 2),
                PlazoDias = Math.Clamp(request.PlazoDias, 1, 365),
                ValidoHasta = ahora.Date.AddDays(Math.Clamp(request.ValidezDias, 1, 60)).AddDays(1).AddSeconds(-1),
                CondicionesPago = Limpio(request.CondicionesPago, 500),
                Estado = "Borrador",
                UsuarioNombre = Limpio(request.UsuarioNombre, 150),
                FechaCreacion = ahora,
                Activo = true
            };
            _db.HqMercadoPedidos.Add(p);

            if (n != null)
            {
                n.ContactoNombre ??= Limpio(request.ClienteNombre, 150);
                n.ContactoEmail ??= p.ClienteEmail;
                n.ContactoTelefono ??= p.ClienteTelefono;
                if (n.Estado is "Nueva" or "Evaluando" or "Contactada") n.Estado = "Propuesta";
                n.FechaActualizacion = ahora;
            }
            await _db.SaveChangesAsync();
            return Map(p, n?.Titulo);
        }

        public async Task<List<MercadoPedidoDto>> ListarPedidosAsync(int empresaId)
        {
            var pedidos = await _db.HqMercadoPedidos.Where(p => p.EmpresaId == empresaId && p.Activo)
                .OrderByDescending(p => p.FechaCreacion).Take(200).ToListAsync();

            // Los vencidos se marcan al consultar
            var vencidos = pedidos.Where(Vencido).ToList();
            foreach (var p in vencidos) p.Estado = "Vencido";
            if (vencidos.Count > 0) await _db.SaveChangesAsync();

            var ids = pedidos.Where(p => p.NecesidadId.HasValue).Select(p => p.NecesidadId!.Value).Distinct().ToList();
            var titulos = await _db.HqMercadoNecesidades.AsNoTracking().Where(n => ids.Contains(n.NecesidadId))
                .ToDictionaryAsync(n => n.NecesidadId, n => n.Titulo);
            return pedidos.Select(p => Map(p, p.NecesidadId.HasValue ? titulos.GetValueOrDefault(p.NecesidadId.Value) : null)).ToList();
        }

        private async Task MarcarEnviadoAsync(HqMercadoPedido p, HqMercadoNecesidad? n, string? usuario)
        {
            if (p.Estado == "Borrador" || p.Estado == "Vencido") p.Estado = "Enviado";
            p.FechaEnvio ??= DateTime.UtcNow;
            if (n != null)
            {
                if (n.Estado is "Nueva" or "Evaluando" or "Contactada") n.Estado = "Propuesta";
                if (n.ClienteId.HasValue)
                    await _crm.CambiarEstadoAsync(n.ClienteId.Value, n.EmpresaId, new CrmCambiarEstadoRequest
                    {
                        EstadoPipeline = "Propuesta enviada",
                        Nota = $"Pedido {p.Numero} enviado: {p.Producto}",
                        UsuarioNombre = usuario
                    });
            }
        }

        public async Task<MercadoPedidoDto?> CambiarEstadoPedidoAsync(int pedidoId, MercadoPedidoEstadoRequest request)
        {
            var p = await _db.HqMercadoPedidos.FirstOrDefaultAsync(x => x.PedidoId == pedidoId && x.EmpresaId == request.EmpresaId && x.Activo);
            if (p == null) return null;
            var n = p.NecesidadId.HasValue
                ? await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == p.NecesidadId)
                : null;

            switch (request.Estado)
            {
                case "Enviado":
                    if (p.ValidoHasta < DateTime.UtcNow) p.ValidoHasta = DateTime.UtcNow.Date.AddDays(8).AddSeconds(-1);
                    await MarcarEnviadoAsync(p, n, request.UsuarioNombre);
                    if (n != null && !string.IsNullOrWhiteSpace(request.Canal) && CanalesValidos.Contains(request.Canal))
                        _db.HqMercadoContactos.Add(new HqMercadoContacto
                        {
                            EmpresaId = p.EmpresaId,
                            NecesidadId = n.NecesidadId,
                            Canal = request.Canal,
                            Destino = request.Canal == "WhatsApp" ? p.ClienteTelefono : p.ClienteEmail,
                            Asunto = $"Pedido {p.Numero}",
                            Resultado = "Registrado",
                            Detalle = $"{p.Producto} · {p.Moneda} {p.PrecioSetup:N0} + {p.PrecioMensual:N0}/mes",
                            UsuarioNombre = Limpio(request.UsuarioNombre, 150),
                            Fecha = DateTime.UtcNow
                        });
                    break;
                case "Pagado":
                    p.Estado = "Pagado";
                    p.FechaPago = DateTime.UtcNow;
                    if (n != null)
                    {
                        n.Estado = "Ganada";
                        if (n.ClienteId.HasValue)
                            await _crm.CambiarEstadoAsync(n.ClienteId.Value, n.EmpresaId, new CrmCambiarEstadoRequest
                            {
                                EstadoPipeline = "Cliente activo",
                                Nota = $"Pedido {p.Numero} pagado",
                                UsuarioNombre = request.UsuarioNombre
                            });
                    }
                    break;
                case "Rechazado":
                    p.Estado = "Rechazado";
                    p.FechaRespuesta ??= DateTime.UtcNow;
                    break;
                case "Borrador":
                    p.Estado = "Borrador";
                    break;
                default:
                    throw new InvalidOperationException($"Estado inválido: {request.Estado}");
            }
            if (n != null) n.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(p, n?.Titulo);
        }

        // ───────────────────────── Link público ─────────────────────────

        private static MercadoPedidoPublicoDto MapPublico(HqMercadoPedido p) => new()
        {
            Numero = p.Numero,
            ClienteNombre = p.ClienteNombre,
            Producto = p.Producto,
            Incluye = p.Incluye,
            Moneda = p.Moneda,
            PrecioSetup = p.PrecioSetup,
            PrecioMensual = p.PrecioMensual,
            PlazoDias = p.PlazoDias,
            ValidoHasta = p.ValidoHasta,
            CondicionesPago = p.CondicionesPago,
            Estado = p.Estado == "Visto" || p.Estado == "Enviado" || p.Estado == "Borrador" ? "Pendiente" : p.Estado,
            Vencido = Vencido(p) || p.Estado == "Vencido",
            FechaRespuesta = p.FechaRespuesta,
            RespuestaNombre = p.RespuestaNombre
        };

        private async Task<HqMercadoPedido?> PorTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length != 32 || !token.All(Uri.IsHexDigit)) return null;
            return await _db.HqMercadoPedidos.FirstOrDefaultAsync(p => p.Token == token && p.Activo);
        }

        // ───────────── Bajas de comunicaciones comerciales ─────────────

        private string SitioPublico => (_config["Sitio:UrlPublica"] ?? "https://www.vordia.com.py").TrimEnd('/');

        private byte[] SecretoBajas => Encoding.UTF8.GetBytes(_config["Bajas:Secret"] ?? _config["Jwt:Key"] ?? throw new InvalidOperationException("Falta Bajas:Secret"));

        private static string B64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] DeB64Url(string s)
        {
            var t = s.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(t.PadRight(t.Length + (4 - t.Length % 4) % 4, '='));
        }

        /// <summary>Token firmado (HMAC) con empresa y correo: no se puede falsificar ni usar para otro correo.</summary>
        private string TokenBaja(int empresaId, string email)
        {
            var datos = Encoding.UTF8.GetBytes($"{empresaId}|{email.Trim().ToLowerInvariant()}");
            using var h = new HMACSHA256(SecretoBajas);
            return $"{B64Url(datos)}.{B64Url(h.ComputeHash(datos)[..16])}";
        }

        public string UrlBaja(int empresaId, string email) => $"{SitioPublico}/baja/{TokenBaja(empresaId, email)}";

        public async Task<bool> RegistrarBajaAsync(string token)
        {
            var partes = (token ?? "").Split('.');
            if (partes.Length != 2) return false;
            byte[] datos, firma;
            try { datos = DeB64Url(partes[0]); firma = DeB64Url(partes[1]); }
            catch (FormatException) { return false; }
            using (var h = new HMACSHA256(SecretoBajas))
                if (!CryptographicOperations.FixedTimeEquals(h.ComputeHash(datos)[..16], firma)) return false;

            var texto = Encoding.UTF8.GetString(datos).Split('|', 2);
            if (texto.Length != 2 || !int.TryParse(texto[0], out var empresaId)) return false;
            var email = texto[1];
            if (!await _db.HqMercadoBajas.AnyAsync(b => b.EmpresaId == empresaId && b.Email == email))
            {
                _db.HqMercadoBajas.Add(new HqMercadoBaja { EmpresaId = empresaId, Email = email, Origen = "Link", Fecha = DateTime.UtcNow });
                await _db.SaveChangesAsync();
            }
            return true;
        }

        public async Task<MercadoPedidoPublicoDto?> ObtenerPedidoPublicoAsync(string token)
        {
            var p = await PorTokenAsync(token);
            if (p == null) return null;
            if (p.Estado is "Borrador" or "Enviado")
            {
                p.Estado = "Visto";
                p.FechaVisto ??= DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return MapPublico(p);
        }

        public async Task<MercadoPedidoPublicoDto> ResponderPedidoPublicoAsync(string token, bool aceptar, MercadoPedidoRespuestaRequest request)
        {
            var p = await PorTokenAsync(token) ?? throw new KeyNotFoundException("Pedido no encontrado");
            if (!EstadosAbiertos.Contains(p.Estado)) throw new InvalidOperationException("Este pedido ya fue respondido.");
            if (Vencido(p)) throw new InvalidOperationException("Este pedido venció. Pedinos uno actualizado.");
            if (aceptar && string.IsNullOrWhiteSpace(request.Nombre)) throw new InvalidOperationException("Escribí tu nombre para aceptar.");

            var ahora = DateTime.UtcNow;
            p.Estado = aceptar ? "Aceptado" : "Rechazado";
            p.FechaRespuesta = ahora;
            p.FechaVisto ??= ahora;
            p.RespuestaNombre = Limpio(request.Nombre, 150);
            p.RespuestaComentario = Limpio(request.Comentario, 1000);
            if (aceptar)
            {
                p.ClienteEmail ??= Limpio(request.Email, 150);
                p.ClienteTelefono ??= Limpio(request.Telefono, 40);
            }

            var n = p.NecesidadId.HasValue
                ? await _db.HqMercadoNecesidades.FirstOrDefaultAsync(x => x.NecesidadId == p.NecesidadId)
                : null;
            if (n != null && aceptar)
            {
                n.Estado = "Ganada";
                n.ContactoEmail ??= p.ClienteEmail;
                n.ContactoTelefono ??= p.ClienteTelefono;
                n.FechaActualizacion = ahora;
            }
            await _db.SaveChangesAsync();

            if (aceptar && n != null)
            {
                // El cliente queda en el CRM como cerrado (o se crea si todavía no estaba)
                if (n.ClienteId.HasValue)
                {
                    await _crm.CambiarEstadoAsync(n.ClienteId.Value, n.EmpresaId, new CrmCambiarEstadoRequest
                    {
                        EstadoPipeline = "Cerrado",
                        Nota = $"Aceptó el pedido {p.Numero} ({p.Producto}) desde el link · {p.RespuestaNombre}",
                        UsuarioNombre = p.RespuestaNombre
                    });
                }
                else
                {
                    var cliente = await _crm.CrearClienteAsync(new CrmClienteUpsertRequest
                    {
                        EmpresaId = n.EmpresaId,
                        RazonSocial = Limpio(p.ClienteNombre, 200)!,
                        ContactoNombre = Limpio(p.RespuestaNombre, 150),
                        ContactoEmail = p.ClienteEmail,
                        ContactoTelefono = Limpio(p.ClienteTelefono, 50),
                        Rubro = Limpio(n.Categoria, 100),
                        EstadoPipeline = "Cerrado",
                        SistemaServicio = Limpio(p.Producto, 200),
                        FeeEstimadoMensual = p.PrecioMensual,
                        Origen = "mercado",
                        OrigenDetalle = Limpio($"Pedido {p.Numero} aceptado", 120),
                        Notas = $"Aceptó el pedido {p.Numero} desde el link público.\n{p.Moneda} {p.PrecioSetup:N0} implementación + {p.PrecioMensual:N0}/mes."
                    });
                    n.ClienteId = cliente.ClienteId;
                    await _db.SaveChangesAsync();
                }
            }

            // Aviso interno a la casilla de Vordia
            await _email.EnviarNotificacionHqAsync(null,
                aceptar ? $"✅ Pedido {p.Numero} ACEPTADO" : $"Pedido {p.Numero} rechazado",
                $"{p.ClienteNombre} {(aceptar ? "aceptó" : "rechazó")} el pedido {p.Numero}.\n\n" +
                $"Producto: {p.Producto}\nImporte: {p.Moneda} {p.PrecioSetup:N0} implementación + {p.PrecioMensual:N0}/mes\n" +
                $"Respondió: {p.RespuestaNombre}\nCorreo: {p.ClienteEmail}\nTeléfono: {p.ClienteTelefono}\n" +
                (string.IsNullOrWhiteSpace(p.RespuestaComentario) ? "" : $"Comentario: {p.RespuestaComentario}\n") +
                (aceptar ? "\nPróximo paso: coordinar el pago y el inicio de la implementación." : ""));

            return MapPublico(p);
        }
    }
}
