using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("FontanaMovimientos")]
    public class FontanaMovimiento
    {
        [Key]
        public int MovimientoId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        /// <summary>ingreso | gasto</summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "ingreso";

        [Required]
        [StringLength(50)]
        public string Propiedad { get; set; } = "Fontana 1";

        [Required]
        [StringLength(80)]
        public string Categoria { get; set; } = "Otros ingresos";

        [Required]
        [StringLength(300)]
        public string Concepto { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(50)]
        public string? MetodoPago { get; set; }

        [StringLength(120)]
        public string? Origen { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
