using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmTareas")]
    public class CrmTarea
    {
        [Key]
        public int TareaId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        /// <summary>Pendiente | En curso | Hecha | Cancelada</summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        /// <summary>Baja | Media | Alta</summary>
        [Required]
        [StringLength(20)]
        public string Prioridad { get; set; } = "Media";

        public int? ClienteId { get; set; }
        public int? ContratoId { get; set; }
        public int? AsignadoUsuarioId { get; set; }

        [StringLength(150)]
        public string? AsignadoNombre { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaCompletada { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual CrmCliente? Cliente { get; set; }
    }
}
