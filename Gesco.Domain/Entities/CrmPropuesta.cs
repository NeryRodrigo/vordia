using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Propuesta comercial enviada a un cliente. Al marcarse como ganada, el
    /// cliente pasa a "Cliente activo" en el pipeline; si se pierde, queda
    /// como "Cerrado".
    /// </summary>
    [Table("CrmPropuestas")]
    public class CrmPropuesta
    {
        [Key]
        public int PropuestaId { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        [Required, StringLength(300)]
        public string Descripcion { get; set; } = "";
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGs { get; set; }
        /// <summary>Enviada | Ganada | Perdida</summary>
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Enviada";
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaCierre { get; set; }
        [StringLength(500)]
        public string? Notas { get; set; }
        [StringLength(150)]
        public string? UsuarioNombre { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
