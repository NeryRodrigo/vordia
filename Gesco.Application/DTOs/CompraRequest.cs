using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class RegistrarCompraRequest
    {
        public int ProveedorId { get; set; }
        public string NroFactura { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public List<CompraDetalleRequest> Detalles { get; set; } = new List<CompraDetalleRequest>();
    }

    public class CompraDetalleRequest
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class CompraCabeceraDto
    {
        public int CompraCabeceraId { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorRazonSocial { get; set; } = string.Empty;
        public string NroFactura { get; set; } = string.Empty;
        public DateTime FechaFactura { get; set; }
        public decimal Total { get; set; }
        public List<CompraDetalleDto> Detalles { get; set; } = new List<CompraDetalleDto>();
    }

    public class CompraDetalleDto
    {
        public int ProductoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
