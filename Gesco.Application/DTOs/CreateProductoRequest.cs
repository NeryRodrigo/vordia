using System;

namespace Gesco.Application.DTOs
{
    public class CreateProductoRequest
    {
        public string? CodigoBarra { get; set; }
        public string? Sku { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal Costo { get; set; }
        public int IvaPorcentaje { get; set; }
        public int? CategoriaId { get; set; }
        public int? MarcaId { get; set; }
    }

    public class UpdateProductoRequest : CreateProductoRequest
    {
        public int ProductoId { get; set; }
        public bool Activo { get; set; }
    }
}
