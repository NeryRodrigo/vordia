using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class VentaDetalle
    {
        public int VentaDetalleId { get; set; }
        public int VentaCabeceraId { get; set; }
        public int? ProductoId { get; set; } // Opcional por si venden un servicio genérico
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public int IvaPorcentaje { get; set; } // 10, 5 o 0

        // Propiedad de navegación hacia el padre y el producto
        [ValidateNever]
        [JsonIgnore] 
        public VentaCabecera? VentaCabecera { get; set; }
        
        [ValidateNever]
        [JsonIgnore]
        public Producto? Producto { get; set; }
    }
}
