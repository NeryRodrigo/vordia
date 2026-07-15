using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class ComprasService : IComprasService
    {
        private readonly IApplicationDbContext _context;

        public ComprasService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> RegistrarCompraAsync(int empresaId, RegistrarCompraRequest request)
        {
            var cabecera = new CompraCabecera
            {
                EmpresaId = empresaId,
                ProveedorId = request.ProveedorId,
                NroFactura = request.NroFactura,
                FechaFactura = request.FechaFactura,
                Total = request.Detalles.Sum(d => d.Cantidad * d.CostoUnitario)
            };

            foreach (var det in request.Detalles)
            {
                var detalle = new CompraDetalle
                {
                    ProductoId = det.ProductoId,
                    Cantidad = det.Cantidad,
                    CostoUnitario = det.CostoUnitario,
                    Subtotal = det.Cantidad * det.CostoUnitario
                };
                cabecera.Detalles.Add(detalle);

                // Incremento de stock global
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == det.ProductoId && p.EmpresaId == empresaId);
                if (producto != null)
                {
                    producto.StockActual += det.Cantidad;
                    // Opcionalmente actualizar el costo promedio del producto
                    producto.Costo = det.CostoUnitario;
                }
            }

            _context.CompraCabeceras.Add(cabecera);
            await _context.SaveChangesAsync();

            return cabecera.CompraCabeceraId;
        }

        public async Task<List<Proveedor>> GetProveedoresAsync(int empresaId)
        {
            return await _context.Proveedores
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .ToListAsync();
        }

        public async Task<List<CompraCabeceraDto>> GetComprasAsync(int empresaId)
        {
            return await _context.CompraCabeceras
                .Where(c => c.EmpresaId == empresaId)
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(c => c.FechaFactura)
                .Select(c => new CompraCabeceraDto
                {
                    CompraCabeceraId = c.CompraCabeceraId,
                    ProveedorId = c.ProveedorId,
                    ProveedorRazonSocial = c.Proveedor.RazonSocial,
                    NroFactura = c.NroFactura,
                    FechaFactura = c.FechaFactura,
                    Total = c.Total,
                    Detalles = c.Detalles.Select(d => new CompraDetalleDto
                    {
                        ProductoId = d.ProductoId,
                        Descripcion = d.Producto.Descripcion,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario,
                        Subtotal = d.Subtotal
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}
