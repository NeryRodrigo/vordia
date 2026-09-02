using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmProductos")]
    public class CrmProducto
    {
        [Key]
        public int ProductoId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(30)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [StringLength(500)]
        public string? Significado { get; set; }
        [StringLength(1000)]
        public string? QueSera { get; set; }
        public string? QueIncluye { get; set; }
        [StringLength(1000)]
        public string? IdeaCentral { get; set; }
        [Required, StringLength(30)]
        public string Estado { get; set; } = "Planificado";
        [Required, StringLength(10)]
        public string Prioridad { get; set; } = "P2";
        public decimal FeeEstimadoGs { get; set; }
        public int Orden { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
