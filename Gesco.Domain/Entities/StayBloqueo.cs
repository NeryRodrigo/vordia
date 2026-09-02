using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// VORDIA STAY — bloqueo de calendario (mantenimiento / owner / out of order).
    /// Las ocupaciones reales siguen en Reserva; esto complementa la disponibilidad.
    /// </summary>
    [Table("StayBloqueos")]
    public class StayBloqueo
    {
        [Key]
        public int StayBloqueoId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int HabitacionId { get; set; }

        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }

        /// <summary>Mantenimiento | Owner | BloqueoManual | Limpieza</summary>
        [Required]
        [StringLength(40)]
        public string Motivo { get; set; } = "BloqueoManual";

        [StringLength(400)]
        public string? Notas { get; set; }

        public int? UsuarioId { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
