using Gesco.Application.DTOs;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IComprasService
    {
        Task<int> RegistrarCompraAsync(int empresaId, RegistrarCompraRequest request);
        Task<List<Proveedor>> GetProveedoresAsync(int empresaId);
        Task<List<CompraCabeceraDto>> GetComprasAsync(int empresaId);
    }
}
