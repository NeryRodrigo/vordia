using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class ContratoInquilino
    {
        public int PropiedadId;
        public decimal MontoMensualGs;
        public int DiaVencimiento;
        public bool IncluyeAgua;
        public string Estado;

        [Key]
        public int ContratoId { get; set; }

        public int UnidadId { get; set; }
        public virtual UnidadCondominio Unidad { get; set; }

        public int InquilinoId { get; set; }
        public virtual Persona Inquilino { get; set; }

        public DateTime FechaIngreso { get; set; }
        public DateTime FechaInicioContrato { get; set; }
        public DateTime FechaFinContrato { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoMensualAlquiler { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositoGarantia { get; set; }

        public int CantidadOcupantes { get; set; }
        public string? VehiculoInfo { get; set; }
        public string? Observaciones { get; set; }
        public string? ServiciosIncluidos { get; set; }
        public bool EstaActivo { get; set; } = true;

        public virtual ICollection<PagoRecibo> Pagos { get; set; } = new List<PagoRecibo>();
    }
}
