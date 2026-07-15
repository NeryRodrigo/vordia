using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class MorosidadInquilino
    {
        [Key]
        public int MorosidadId { get; set; }

        public int InquilinoId { get; set; }
        public virtual Persona Inquilino { get; set; }

        public int UnidadId { get; set; }
        public virtual UnidadCondominio Unidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoPendiente { get; set; }

        public int CantidadCuotas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInteresDiario { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InteresAcumuladoTotal { get; set; }

        public DateTime? UltimaFechaCalculoInteres { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string Estado { get; set; } = "Pendiente"; // Pendiente, En Proceso Judicial, Pagado

        [NotMapped]
        public bool EstaDesalojado { get; set; }

        public string? ObservacionesGenerales { get; set; }

        public virtual ICollection<CuotaMorosidad> Cuotas { get; set; } = new List<CuotaMorosidad>();
    }
}
