using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmProyectos")]
    public class CrmProyecto
    {
        [Key]
        public int ProyectoId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(200)]
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        [StringLength(30)]
        public string? ProductoCodigo { get; set; }
        [Required, StringLength(30)]
        public string Estado { get; set; } = "Planificado";
        [Required, StringLength(10)]
        public string Prioridad { get; set; } = "P1";
        public int? ClienteId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        [StringLength(150)]
        public string? ResponsableNombre { get; set; }
        public int AvancePct { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ClienteId))]
        public CrmCliente? Cliente { get; set; }
    }
}
