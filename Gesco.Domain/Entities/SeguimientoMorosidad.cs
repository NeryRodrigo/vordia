using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class SeguimientoMorosidad
    {
        [Key]
        public int SeguimientoId { get; set; }

        public int CuotaId { get; set; }
        public virtual CuotaMorosidad Cuota { get; set; }

        public DateTime FechaRegistro { get; set; }

        [Required]
        public string Observacion { get; set; }
    }
}
