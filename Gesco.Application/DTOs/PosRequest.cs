using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class RealizarVentaRequest
    {
        public int PersonaId { get; set; } // Opcional o genérico "Cliente Ocasional"
        public string NroFactura { get; set; } = string.Empty;
        public List<VentaDetalleRequest> Detalles { get; set; } = new List<VentaDetalleRequest>();
    }

    public class VentaDetalleRequest
    {
        public int ProductoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int IvaPorcentaje { get; set; }
    }
}
