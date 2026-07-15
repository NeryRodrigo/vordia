using System;
using System.Collections.Generic;

namespace Gesco.Domain.Entities
{
    public class CompraCabecera
    {
        public int CompraCabeceraId { get; set; }
        public int EmpresaId { get; set; }
        public int ProveedorId { get; set; }
        public string NroFactura { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public decimal Total { get; set; }

        public Empresa Empresa { get; set; } = null!;
        public Proveedor Proveedor { get; set; } = null!;
        public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    }
}
