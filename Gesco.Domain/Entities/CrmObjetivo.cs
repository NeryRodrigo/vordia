using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmObjetivos")]
    public class CrmObjetivo
    {
        [Key]
        public int ObjetivoId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(20)]
        public string Ambito { get; set; } = "Empresa"; // Empresa | Crm | Tarea
        [Required, StringLength(200)]
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        /// <summary>Notas de reunión para compartir con socia.</summary>
        public string? NotasReunion { get; set; }
        [Required, StringLength(30)]
        public string Estado { get; set; } = "Pendiente";
        [Required, StringLength(20)]
        public string Prioridad { get; set; } = "Media";
        public DateTime? FechaObjetivo { get; set; }
        /// <summary>Objetivo numérico a alcanzar (ej. 10 clientes nuevos). Null si no aplica.</summary>
        public decimal? MetaValor { get; set; }
        /// <summary>Valor alcanzado hasta hoy (ej. 7).</summary>
        public decimal? ValorActual { get; set; }
        /// <summary>Avance manual 0..100, para objetivos sin meta numérica.</summary>
        public int? AvancePct { get; set; }
        public int? ClienteId { get; set; }
        public int? TareaId { get; set; }
        [StringLength(150)]
        public string? CreadoPor { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
