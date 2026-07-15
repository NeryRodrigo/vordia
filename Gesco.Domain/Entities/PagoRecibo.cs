using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class PagoRecibo
    {
        [Key]
        public int PagoId { get; set; }

        public int ContratoId { get; set; }
        public virtual ContratoInquilino? Contrato { get; set; }

        public int? CuotaMorosidadId { get; set; }
        public virtual CuotaMorosidad? CuotaMorosidad { get; set; }

        [Required]
        public string TipoPago { get; set; } // "Alquiler", "Luz", "Basura", "Agua"

        public int MesPagado { get; set; }
        public int AnioPagado { get; set; }

        public DateTime FechaPago { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagado { get; set; }

        public string MetodoPago { get; set; } // "Efectivo", "Transferencia", "Giro"

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoPendiente { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Mora { get; set; }

        public bool FacturaEmitida { get; set; }
        public string? NumeroFactura { get; set; }
        public string? ComprobanteUrl { get; set; }
        public string? NroComprobante { get; set; }
        public string? Banco { get; set; }
        public string? Notas { get; set; }
        public string? ResponsableCobro { get; set; }
    }
}
