using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmRoadmap")]
    public class CrmRoadmap
    {
        [Key]
        public int RoadmapId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        /// <summary>Idea | Alianza</summary>
        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "Idea";

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        /// <summary>Borrador | En evaluacion | Priorizado | En curso | Convertido | Descartado</summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Borrador";

        /// <summary>P0 | P1 | P2</summary>
        [Required]
        [StringLength(10)]
        public string Prioridad { get; set; } = "P2";

        public int? ResponsableUsuarioId { get; set; }

        [StringLength(150)]
        public string? ResponsableNombre { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow.Date;
        public DateTime? FechaMeta { get; set; }

        [StringLength(500)]
        public string? ProximoPaso { get; set; }

        public DateTime? FechaProximoPaso { get; set; }

        /// <summary>Producto | CRM | Proyecto</summary>
        [StringLength(30)]
        public string? ConvertidoA { get; set; }

        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }

        public virtual ICollection<CrmRoadmapHistorial> Historial { get; set; } = new List<CrmRoadmapHistorial>();
    }

    [Table("CrmRoadmapHistorial")]
    public class CrmRoadmapHistorial
    {
        [Key]
        public int HistorialId { get; set; }

        [Required]
        public int RoadmapId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "nota";

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Detalle { get; set; }
        public string? EstadoAnterior { get; set; }
        public string? EstadoNuevo { get; set; }
        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RoadmapId))]
        public virtual CrmRoadmap? Roadmap { get; set; }
    }
}
