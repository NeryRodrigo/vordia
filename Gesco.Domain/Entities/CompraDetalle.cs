using System;

namespace Gesco.Domain.Entities
{
    public class CompraDetalle
    {
        public int CompraDetalleId { get; set; }
        public int CompraCabeceraId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }

        public CompraCabecera CompraCabecera { get; set; } = null!;
        public Producto Producto { get; set; } = null!;
    }
}
