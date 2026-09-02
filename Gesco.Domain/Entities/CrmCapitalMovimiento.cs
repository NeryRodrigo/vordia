using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmCapitalMovimientos")]
    public class CrmCapitalMovimiento
    {
        [Key]
        public int CapitalId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(30)]
        public string Tipo { get; set; } = "Aporte";
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGs { get; set; }
        public DateTime Fecha { get; set; }
        [Required, StringLength(300)]
        public string Concepto { get; set; } = "";
        public int? SocioId { get; set; }
        [StringLength(150)]
        public string? UsuarioNombre { get; set; }
        [StringLength(500)]
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
