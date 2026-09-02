using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// VORDIA STAY — plan tarifario por tipo de habitación / vigencia.
    /// No modifica Habitacion; se resuelve por TipoHabitacion o HabitacionId opcional.
    /// </summary>
    [Table("StayTarifas")]
    public class StayTarifa
    {
        [Key]
        public int StayTarifaId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(120)]
        public string Nombre { get; set; } = "Tarifa estándar";

        /// <summary>Coincide con Habitacion.Tipo (Privada, Compartida, Mensualero, etc.).</summary>
        [Required]
        [StringLength(50)]
        public string TipoHabitacion { get; set; } = "Habitación";

        /// <summary>Si se setea, la tarifa aplica solo a esa unidad.</summary>
        public int? HabitacionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioPorNocheGs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioPorSemanaGs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioPorMesGs { get; set; }

        public DateTime? VigenciaDesde { get; set; }
        public DateTime? VigenciaHasta { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(300)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
