using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Persona de confianza que vende para VORDIA. No es socio: los socios son
    /// solo los dueños, y son ellos quienes deciden promover a un vendedor.
    /// Cuando eso pasa se le crea un CrmSocio y queda vinculado por SocioId,
    /// sin dejar de ser vendedor.
    /// </summary>
    [Table("CrmVendedores")]
    public class CrmVendedor
    {
        [Key]
        public int VendedorId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";
        [StringLength(150)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string? Telefono { get; set; }
        /// <summary>Comisión que cobra sobre lo que vende, en porcentaje.</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal ComisionPct { get; set; }
        /// <summary>Meta de ventas del mes en guaraníes. 0 = sin meta definida.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MetaMensualGs { get; set; }
        /// <summary>Activo | Inactivo</summary>
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Activo";
        /// <summary>Socio vinculado si los dueños lo promovieron. Null si sigue siendo solo vendedor.</summary>
        public int? SocioId { get; set; }
        public DateTime? FechaSocioDesde { get; set; }
        public DateTime? FechaIngreso { get; set; }
        [StringLength(500)]
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
