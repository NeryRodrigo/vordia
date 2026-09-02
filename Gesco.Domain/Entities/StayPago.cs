using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// VORDIA STAY — cobros asociados a una Reserva (análogo a PagoRecibo en Habitat).
    /// </summary>
    [Table("StayPagos")]
    public class StayPago
    {
        [Key]
        public int StayPagoId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ReservaId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGs { get; set; }

        [Required]
        [StringLength(50)]
        public string MetodoPago { get; set; } = "Efectivo";

        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(120)]
        public string Concepto { get; set; } = "Pago reserva";

        [StringLength(80)]
        public string? NroComprobante { get; set; }

        public int? UsuarioId { get; set; }

        [StringLength(400)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
