using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("FontanaPersonal")]
    public class FontanaPersonal
    {
        [Key]
        public int PersonalId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Funcion { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Telefono { get; set; }

        /// <summary>JSON array: ["Fontana 1","Fontana 2"]</summary>
        [Required]
        [StringLength(500)]
        public string PropiedadesJson { get; set; } = "[]";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salario { get; set; }

        public DateTime FechaPago { get; set; }

        /// <summary>PAGADO | PENDIENTE</summary>
        [Required]
        [StringLength(20)]
        public string EstadoPago { get; set; } = "PENDIENTE";

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
