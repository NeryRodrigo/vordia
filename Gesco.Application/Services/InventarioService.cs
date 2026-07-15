using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly IApplicationDbContext _context;

        public InventarioService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductoDto>> GetProductosAsync(int empresaId)
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.Stocks)
                    .ThenInclude(s => s.Almacen)
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .ToListAsync();

            return productos.Select(p => new ProductoDto
            {
                ProductoId = p.ProductoId,
                CodigoBarra = p.CodigoBarra,
                Sku = p.Sku,
                Descripcion = p.Descripcion,
                PrecioVenta = p.PrecioVenta,
                Costo = p.Costo,
                StockActual = p.StockActual,
                IvaPorcentaje = p.IvaPorcentaje,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria?.Nombre,
                MarcaId = p.MarcaId,
                MarcaNombre = p.Marca?.Nombre,
                Stocks = p.Stocks.Select(s => new StockAlmacenDto
                {
                    AlmacenId = s.AlmacenId,
                    AlmacenNombre = s.Almacen.Nombre,
                    Cantidad = s.Cantidad
                }).ToList()
            }).ToList();
        }

        public async Task<ProductoDto> GetProductoByIdAsync(int empresaId, int productoId)
        {
            var p = await _context.Productos
                .Include(x => x.Categoria)
                .Include(x => x.Marca)
                .Include(x => x.Stocks)
                    .ThenInclude(s => s.Almacen)
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.ProductoId == productoId);

            if (p == null)
                throw new Exception("Producto no encontrado o no pertenece a la empresa.");

            return new ProductoDto
            {
                ProductoId = p.ProductoId,
                CodigoBarra = p.CodigoBarra,
                Sku = p.Sku,
                Descripcion = p.Descripcion,
                PrecioVenta = p.PrecioVenta,
                Costo = p.Costo,
                StockActual = p.StockActual,
                IvaPorcentaje = p.IvaPorcentaje,
                Activo = p.Activo,
                CategoriaId = p.CategoriaId,
                CategoriaNombre = p.Categoria?.Nombre,
                MarcaId = p.MarcaId,
                MarcaNombre = p.Marca?.Nombre,
                Stocks = p.Stocks.Select(s => new StockAlmacenDto
                {
                    AlmacenId = s.AlmacenId,
                    AlmacenNombre = s.Almacen.Nombre,
                    Cantidad = s.Cantidad
                }).ToList()
            };
        }

        public async Task<int> CreateProductoAsync(int empresaId, CreateProductoRequest request)
        {
            var producto = new Producto
            {
                EmpresaId = empresaId,
                CodigoBarra = request.CodigoBarra,
                Sku = request.Sku,
                Descripcion = request.Descripcion,
                PrecioVenta = request.PrecioVenta,
                Costo = request.Costo,
                IvaPorcentaje = request.IvaPorcentaje,
                CategoriaId = request.CategoriaId,
                MarcaId = request.MarcaId,
                StockActual = 0,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return producto.ProductoId;
        }

        public async Task UpdateProductoAsync(int empresaId, UpdateProductoRequest request)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.ProductoId == request.ProductoId);

            if (producto == null)
                throw new Exception("Producto no encontrado.");

            producto.CodigoBarra = request.CodigoBarra;
            producto.Sku = request.Sku;
            producto.Descripcion = request.Descripcion;
            producto.PrecioVenta = request.PrecioVenta;
            producto.Costo = request.Costo;
            producto.IvaPorcentaje = request.IvaPorcentaje;
            producto.CategoriaId = request.CategoriaId;
            producto.MarcaId = request.MarcaId;
            producto.Activo = request.Activo;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteProductoAsync(int empresaId, int productoId)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.ProductoId == productoId);

            if (producto == null) return false;

            // Soft delete
            producto.Activo = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AjustarStockAsync(int empresaId, int productoId, decimal nuevoStock)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.ProductoId == productoId);

            if (producto == null)
                throw new Exception("Producto no encontrado.");

            producto.StockActual = nuevoStock;
            await _context.SaveChangesAsync();
        }
    }
}
