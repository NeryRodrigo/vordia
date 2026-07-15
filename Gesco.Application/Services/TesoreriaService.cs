using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class TesoreriaService : ITesoreriaService
    {
        private readonly IApplicationDbContext _context;

        public TesoreriaService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FlujoCajaResponse> GetFlujoCajaAsync(int empresaId, int mes, int anio)
        {
            var response = new FlujoCajaResponse();

            // 1. Obtener Ventas (Ingresos)
            var ventas = await _context.VentaCabeceras
                .Where(v => v.EmpresaId == empresaId && v.Fecha.Month == mes && v.Fecha.Year == anio)
                .ToListAsync();

            var compras = await _context.CompraCabeceras
                .Include(c => c.Proveedor)
                .Where(c => c.EmpresaId == empresaId && c.FechaFactura.Month == mes && c.FechaFactura.Year == anio)
                .ToListAsync();

            // Procesar Ingresos
            foreach (var v in ventas)
            {
                response.Movimientos.Add(new MovimientoFinanciero
                {
                    Fecha = v.Fecha,
                    Tipo = "INGRESO",
                    Categoria = "Venta POS",
                    Descripcion = $"Factura Nro {v.NroFactura}",
                    Monto = v.Total,
                    IdOrigen = v.VentaCabeceraId
                });
                response.TotalIngresos += v.Total;
            }

            // Procesar Egresos
            foreach (var c in compras)
            {
                response.Movimientos.Add(new MovimientoFinanciero
                {
                    Fecha = c.FechaFactura,
                    Tipo = "EGRESO",
                    Categoria = "Pago Proveedor",
                    Descripcion = $"Fact. {c.NroFactura} - {c.Proveedor?.RazonSocial}",
                    Monto = c.Total,
                    IdOrigen = c.CompraCabeceraId
                });
                response.TotalEgresos += c.Total;
            }

            // Ordenar movimientos por fecha descendente
            response.Movimientos = response.Movimientos.OrderByDescending(m => m.Fecha).ToList();

            return response;
        }

        public async Task EliminarMovimientoAsync(int empresaId, string tipo, int idOrigen)
        {
            if (tipo == "INGRESO")
            {
                // Es una VentaCabecera
                var venta = await _context.VentaCabeceras
                    .Include(v => v.Detalles)
                    .FirstOrDefaultAsync(v => v.VentaCabeceraId == idOrigen && v.EmpresaId == empresaId);

                if (venta != null)
                {
                    // Restaurar stock
                    foreach (var det in venta.Detalles)
                    {
                        var prod = await _context.Productos.FindAsync(det.ProductoId);
                        if (prod != null) prod.StockActual += det.Cantidad;
                    }
                    _context.VentaCabeceras.Remove(venta);
                    await _context.SaveChangesAsync();
                }
            }
            else if (tipo == "EGRESO")
            {
                // Es una CompraCabecera
                var compra = await _context.CompraCabeceras
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.CompraCabeceraId == idOrigen && c.EmpresaId == empresaId);

                if (compra != null)
                {
                    // Descontar stock (ya que la compra lo sumó)
                    foreach (var det in compra.Detalles)
                    {
                        var prod = await _context.Productos.FindAsync(det.ProductoId);
                        if (prod != null) prod.StockActual -= det.Cantidad;
                    }
                    _context.CompraCabeceras.Remove(compra);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task EditarMovimientoAsync(int empresaId, string tipo, int idOrigen, decimal nuevoMonto, string nuevaDescripcion)
        {
            if (tipo == "INGRESO")
            {
                var venta = await _context.VentaCabeceras
                    .FirstOrDefaultAsync(v => v.VentaCabeceraId == idOrigen && v.EmpresaId == empresaId);
                if (venta != null)
                {
                    venta.Total = nuevoMonto;
                    // Guardar la nueva descripción temporalmente en NroFactura o si hubiese un campo de notas. Como MVP, lo dejamos así.
                    await _context.SaveChangesAsync();
                }
            }
            else if (tipo == "EGRESO")
            {
                var compra = await _context.CompraCabeceras
                    .FirstOrDefaultAsync(c => c.CompraCabeceraId == idOrigen && c.EmpresaId == empresaId);
                if (compra != null)
                {
                    compra.Total = nuevoMonto;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
