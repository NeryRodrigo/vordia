using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class CuotaMorosidad
    {
        [Key]
        public int CuotaId { get; set; }

        public int MorosidadId { get; set; }
        public virtual MorosidadInquilino Morosidad { get; set; }

        public int NumeroCuota { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public bool EstaPagado { get; set; }

        public DateTime? FechaPago { get; set; }

        public string? Observaciones { get; set; }

        public virtual ICollection<SeguimientoMorosidad> Seguimientos { get; set; } = new List<SeguimientoMorosidad>();
    }
}
