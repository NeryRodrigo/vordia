using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class GastoMantenimiento
    {
        [Key]
        public int GastoId { get; set; }

        public int UnidadId { get; set; }
        public virtual UnidadCondominio Unidad { get; set; }

        [Required]
        public string Descripcion { get; set; } // "Pintura", "Plomeria"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }
        public string? Proveedor { get; set; }

        /// <summary>NIS (luz), ISSAN u otro identificador del servicio.</summary>
        public string? IdentificadorServicio { get; set; }

        public string? NroComprobante { get; set; }
        public int? MesReferencia { get; set; }
        public int? AnioReferencia { get; set; }
        public string? MetodoPago { get; set; }
    }
}
