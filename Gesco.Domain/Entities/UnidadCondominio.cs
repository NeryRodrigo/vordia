using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class UnidadCondominio
    {
        [Key]
        public int UnidadId { get; set; }
        public int EmpresaId { get; set; }
        public virtual Empresa Empresa { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreCondominio { get; set; } // "Aguz 1", "Aguz 2", etc.

        [Required]
        [StringLength(50)]
        public string CodigoUnidad { get; set; } // "Duplex 01"

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } // "Disponible", "Ocupado", "Reparacion"

        public string? NisAnde { get; set; }
        public string? MedidorAgua { get; set; }
        public string? Observaciones { get; set; }

        public virtual ICollection<ContratoInquilino> Contratos { get; set; } = new List<ContratoInquilino>();
        public virtual ICollection<GastoMantenimiento> Gastos { get; set; } = new List<GastoMantenimiento>();
    }
}
