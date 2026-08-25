using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmPagos")]
    public class CrmPago
    {
        [Key]
        public int PagoId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ContratoId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaPago { get; set; }

        [StringLength(20)]
        public string? Periodo { get; set; }

        [StringLength(50)]
        public string? MetodoPago { get; set; }

        [StringLength(200)]
        public string? Concepto { get; set; }

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Registrado";

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }

        [ForeignKey(nameof(ContratoId))]
        public virtual CrmContrato? Contrato { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual CrmCliente? Cliente { get; set; }
    }
}
