using System;

namespace Gesco.Application.DTOs
{
    /* VORDIA HQ — Necesidades del Mercado: contacto con el cliente y pedidos rápidos */

    public class MercadoContactoInfoRequest
    {
        public int EmpresaId { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoTelefono { get; set; }
    }

    /// <summary>Registro de un contacto hecho fuera del sistema (WhatsApp, plataforma, llamada).</summary>
    public class MercadoContactoRegistroRequest
    {
        public int EmpresaId { get; set; }
        public string Canal { get; set; } = "WhatsApp";
        public string? Destino { get; set; }
        public string? Asunto { get; set; }
        public string? Mensaje { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class MercadoCorreoRequest
    {
        public int EmpresaId { get; set; }
        public string Para { get; set; } = "";
        public string Asunto { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? ResponderA { get; set; }
        public string? UsuarioNombre { get; set; }
        /// <summary>Si el correo lleva un pedido, se marca como enviado.</summary>
        public int? PedidoId { get; set; }
    }

    public class MercadoContactoDto
    {
        public int ContactoId { get; set; }
        public string Canal { get; set; } = "";
        public string? Destino { get; set; }
        public string? Asunto { get; set; }
        public string? Mensaje { get; set; }
        public string Resultado { get; set; } = "";
        public string? Detalle { get; set; }
        public string? UsuarioNombre { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class MercadoPedidoRequest
    {
        public int EmpresaId { get; set; }
        public int? NecesidadId { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string? ClienteEmail { get; set; }
        public string? ClienteTelefono { get; set; }
        public string Producto { get; set; } = "";
        public string? Incluye { get; set; }
        public string Moneda { get; set; } = "USD";
        public decimal PrecioSetup { get; set; }
        public decimal PrecioMensual { get; set; }
        public int PlazoDias { get; set; } = 7;
        public int ValidezDias { get; set; } = 7;
        public string? CondicionesPago { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class MercadoPedidoEstadoRequest
    {
        public int EmpresaId { get; set; }
        /// <summary>Enviado | Pagado | Rechazado | Borrador</summary>
        public string Estado { get; set; } = "";
        /// <summary>Canal por el que se envió (WhatsApp, Correo, Plataforma).</summary>
        public string? Canal { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class MercadoPedidoDto
    {
        public int PedidoId { get; set; }
        public int? NecesidadId { get; set; }
        public string? NecesidadTitulo { get; set; }
        public string Token { get; set; } = "";
        public string Numero { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string? ClienteEmail { get; set; }
        public string? ClienteTelefono { get; set; }
        public string Producto { get; set; } = "";
        public string? Incluye { get; set; }
        public string Moneda { get; set; } = "USD";
        public decimal PrecioSetup { get; set; }
        public decimal PrecioMensual { get; set; }
        public int PlazoDias { get; set; }
        public DateTime ValidoHasta { get; set; }
        public string? CondicionesPago { get; set; }
        public string Estado { get; set; } = "";
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaVisto { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? RespuestaNombre { get; set; }
        public string? RespuestaComentario { get; set; }
        public DateTime? FechaPago { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>Lo que ve el cliente en el link público (sin datos internos).</summary>
    public class MercadoPedidoPublicoDto
    {
        public string Numero { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string Producto { get; set; } = "";
        public string? Incluye { get; set; }
        public string Moneda { get; set; } = "USD";
        public decimal PrecioSetup { get; set; }
        public decimal PrecioMensual { get; set; }
        public int PlazoDias { get; set; }
        public DateTime ValidoHasta { get; set; }
        public string? CondicionesPago { get; set; }
        public string Estado { get; set; } = "";
        public bool Vencido { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public string? RespuestaNombre { get; set; }
    }

    public class MercadoPedidoRespuestaRequest
    {
        public string Nombre { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Comentario { get; set; }
        /// <summary>Honeypot: solo un bot lo completa.</summary>
        public string? Sitio { get; set; }
    }
}
