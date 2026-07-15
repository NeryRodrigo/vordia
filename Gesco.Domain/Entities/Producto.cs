using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public int EmpresaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? MarcaId { get; set; }
        public string? Sku { get; set; }
        public string? CodigoBarra { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal Costo { get; set; }
        public decimal StockActual { get; set; } // Maintain this as total stock for quick queries, while StockAlmacen holds per-warehouse stock
        public int IvaPorcentaje { get; set; } // 0, 5 o 10
        public bool Activo { get; set; } = true;

        public Empresa Empresa { get; set; } = null!;
        public Categoria? Categoria { get; set; }
        public Marca? Marca { get; set; }
        public ICollection<StockAlmacen> Stocks { get; set; } = new List<StockAlmacen>();
    }
}
