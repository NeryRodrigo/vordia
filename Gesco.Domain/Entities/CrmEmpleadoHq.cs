using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmEmpleadosHq")]
    public class CrmEmpleadoHq
    {
        [Key]
        public int EmpleadoHqId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";
        [Required, StringLength(120)]
        public string RolCargo { get; set; } = "";
        [StringLength(150)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string? Telefono { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalarioMensualGs { get; set; }
        public DateTime? FechaIngreso { get; set; }
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Activo";
        [StringLength(500)]
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
