using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IInventarioService
    {
        Task<List<ProductoDto>> GetProductosAsync(int empresaId);
        Task<ProductoDto> GetProductoByIdAsync(int empresaId, int productoId);
        Task<int> CreateProductoAsync(int empresaId, CreateProductoRequest request);
        Task UpdateProductoAsync(int empresaId, UpdateProductoRequest request);
        Task<bool> DeleteProductoAsync(int empresaId, int productoId);
        Task AjustarStockAsync(int empresaId, int productoId, decimal nuevoStock);
    }
}
