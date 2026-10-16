using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>Fuente pública donde se buscan pedidos de sistemas y aplicaciones.</summary>
    [Table("HqMercadoFuentes")]
    public class HqMercadoFuente
    {
        [Key] public int FuenteId { get; set; }
        public int EmpresaId { get; set; }
        /// <summary>Freelancer | DNCP | SECOP | BancoMundial | PNCP | ContractsFinder | RSS</summary>
        [Required, StringLength(20)] public string Tipo { get; set; } = "Freelancer";
        [Required, StringLength(120)] public string Nombre { get; set; } = "";
        /// <summary>Palabras clave (Freelancer) o URL del feed (RSS / Google Alerts).</summary>
        [StringLength(500)] public string? Consulta { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime? UltimaSync { get; set; }
        [StringLength(300)] public string? UltimoResultado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Pedido de sistema / aplicación detectado en internet o cargado a mano.</summary>
    [Table("HqMercadoNecesidades")]
    public class HqMercadoNecesidad
    {
        [Key] public int NecesidadId { get; set; }
        public int EmpresaId { get; set; }
        /// <summary>Freelancer | DNCP | RSS | Manual</summary>
        [Required, StringLength(20)] public string Fuente { get; set; } = "Manual";
        [Required, StringLength(150)] public string ExternoId { get; set; } = "";
        [Required, StringLength(400)] public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        [StringLength(600)] public string? Url { get; set; }
        [StringLength(250)] public string? Solicitante { get; set; }
        [StringLength(60)] public string? Pais { get; set; }
        public decimal? PresupuestoMin { get; set; }
        public decimal? PresupuestoMax { get; set; }
        [StringLength(10)] public string? Moneda { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaCierre { get; set; }
        [StringLength(400)] public string? Habilidades { get; set; }
        [StringLength(30)] public string? Vertical { get; set; }
        [StringLength(60)] public string? Categoria { get; set; }
        [StringLength(300)] public string? ProductoSugerido { get; set; }
        public int Puntaje { get; set; }
        [StringLength(500)] public string? MotivoPuntaje { get; set; }
        /// <summary>0-100: qué tan seguro es el comprador y el cobro.</summary>
        public int Confianza { get; set; }
        [StringLength(300)] public string? ConfianzaMotivo { get; set; }
        /// <summary>Propuestas que ya recibió el pedido (competencia).</summary>
        public int? Competidores { get; set; }
        /// <summary>Paraguay | LatAm | Internacional</summary>
        [StringLength(20)] public string? Alcance { get; set; }
        /// <summary>Nueva | Evaluando | Contactada | Propuesta | Ganada | Descartada</summary>
        [Required, StringLength(20)] public string Estado { get; set; } = "Nueva";
        [StringLength(150)] public string? ResponsableNombre { get; set; }
        [StringLength(150)] public string? ContactoNombre { get; set; }
        [StringLength(150)] public string? ContactoEmail { get; set; }
        [StringLength(40)] public string? ContactoTelefono { get; set; }
        /// <summary>De dónde salió el contacto: publicado por la entidad compradora o cargado a mano.</summary>
        [StringLength(60)] public string? ContactoOrigen { get; set; }
        public string? Notas { get; set; }
        public int? ClienteId { get; set; }
        public int? RoadmapId { get; set; }
        public DateTime FechaCaptura { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; } = true;
    }

    /// <summary>Cada vez que se contacta al cliente (WhatsApp, correo, plataforma o llamada).</summary>
    [Table("HqMercadoContactos")]
    public class HqMercadoContacto
    {
        [Key] public int ContactoId { get; set; }
        public int EmpresaId { get; set; }
        public int NecesidadId { get; set; }
        /// <summary>WhatsApp | Correo | Plataforma | Llamada</summary>
        [Required, StringLength(20)] public string Canal { get; set; } = "WhatsApp";
        [StringLength(150)] public string? Destino { get; set; }
        [StringLength(200)] public string? Asunto { get; set; }
        public string? Mensaje { get; set; }
        /// <summary>Registrado | Enviado | Error</summary>
        [Required, StringLength(20)] public string Resultado { get; set; } = "Registrado";
        [StringLength(300)] public string? Detalle { get; set; }
        [StringLength(150)] public string? UsuarioNombre { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Pedido / propuesta rápida con link público para que el cliente lo acepte.</summary>
    [Table("HqMercadoPedidos")]
    public class HqMercadoPedido
    {
        [Key] public int PedidoId { get; set; }
        public int EmpresaId { get; set; }
        public int? NecesidadId { get; set; }
        [Required, StringLength(40)] public string Token { get; set; } = "";
        [Required, StringLength(20)] public string Numero { get; set; } = "";
        [Required, StringLength(200)] public string ClienteNombre { get; set; } = "";
        [StringLength(150)] public string? ClienteEmail { get; set; }
        [StringLength(40)] public string? ClienteTelefono { get; set; }
        [Required, StringLength(200)] public string Producto { get; set; } = "";
        public string? Incluye { get; set; }
        [Required, StringLength(10)] public string Moneda { get; set; } = "USD";
        public decimal PrecioSetup { get; set; }
        public decimal PrecioMensual { get; set; }
        public int PlazoDias { get; set; } = 7;
        public DateTime ValidoHasta { get; set; }
        [StringLength(500)] public string? CondicionesPago { get; set; }
        /// <summary>Borrador | Enviado | Visto | Aceptado | Rechazado | Pagado | Vencido</summary>
        [Required, StringLength(20)] public string Estado { get; set; } = "Borrador";
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaVisto { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        [StringLength(150)] public string? RespuestaNombre { get; set; }
        [StringLength(1000)] public string? RespuestaComentario { get; set; }
        public DateTime? FechaPago { get; set; }
        [StringLength(150)] public string? UsuarioNombre { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
    }
}
