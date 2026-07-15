using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class ConfiguracionMulta
    {
        [Key]
        public int Id { get; set; }
        
        public int EmpresaId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoMulta { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TipoAplicacion { get; set; } = "FinMesAtrasado"; // "FinMesAtrasado" o "CincoDias"
        
        public int DiasGracia { get; set; } = 5;
    }
}
