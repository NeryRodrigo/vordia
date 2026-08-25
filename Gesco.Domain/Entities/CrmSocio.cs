using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmSocios")]
    public class CrmSocio
    {
        [Key]
        public int SocioId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Porcentaje { get; set; }

        public int? UsuarioId { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
