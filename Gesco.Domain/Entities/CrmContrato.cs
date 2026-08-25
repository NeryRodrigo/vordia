using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmContratos")]
    public class CrmContrato
    {
        [Key]
        public int ContratoId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        [StringLength(200)]
        public string SistemaServicio { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FeeAcordado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FeeMensual { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FeeAnual { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? ProximoVencimiento { get; set; }

        /// <summary>Al dia | Pendiente | Vencido | Pagado</summary>
        [Required]
        [StringLength(30)]
        public string EstadoPago { get; set; } = "Pendiente";

        /// <summary>Borrador | Activo | Suspendido | Finalizado</summary>
        [Required]
        [StringLength(30)]
        public string EstadoContrato { get; set; } = "Activo";

        public bool EsRecurrente { get; set; } = true;

        /// <summary>Mensual | Anual</summary>
        [Required]
        [StringLength(20)]
        public string Periodicidad { get; set; } = "Mensual";

        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual CrmCliente? Cliente { get; set; }
    }
}
