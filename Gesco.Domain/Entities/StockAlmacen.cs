using System;

namespace Gesco.Domain.Entities
{
    public class StockAlmacen
    {
        public int StockAlmacenId { get; set; }
        public int ProductoId { get; set; }
        public int AlmacenId { get; set; }
        public decimal Cantidad { get; set; }
        
        public Producto Producto { get; set; } = null!;
        public Almacen Almacen { get; set; } = null!;
    }
}
