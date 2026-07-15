using System;
using System.Collections.Generic;

namespace Gesco.Domain.Entities
{
    public class Almacen
    {
        public int AlmacenId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public bool Activo { get; set; } = true;

        public Empresa Empresa { get; set; } = null!;
        public ICollection<StockAlmacen> Stocks { get; set; } = new List<StockAlmacen>();
    }
}
