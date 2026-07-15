using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class ProductoDto
    {
        public int ProductoId { get; set; }
        public string? CodigoBarra { get; set; }
        public string? Sku { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal Costo { get; set; }
        public decimal StockActual { get; set; }
        public int IvaPorcentaje { get; set; }
        public bool Activo { get; set; }
        
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        
        public int? MarcaId { get; set; }
        public string? MarcaNombre { get; set; }
        
        public List<StockAlmacenDto> Stocks { get; set; } = new();
    }

    public class StockAlmacenDto
    {
        public int AlmacenId { get; set; }
        public string AlmacenNombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }
}
