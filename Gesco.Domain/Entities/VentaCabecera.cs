using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Gesco.Domain.Entities
{
    public class VentaCabecera
    {
        public int VentaCabeceraId { get; set; }
        public int EmpresaId { get; set; }
        public int PersonaId { get; set; } // El Cliente
        public DateTime Fecha { get; set; }
        public string NroFactura { get; set; } = string.Empty; // Ej: 001-001-0000001
        public decimal Total { get; set; }

        // Propiedades de navegación (Relaciones)
        [ValidateNever]
        public Persona? Cliente { get; set; } = null!;
        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    }
}
