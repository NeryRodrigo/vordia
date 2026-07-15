using Gesco.Application.DTOs;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ICajaPosService
    {
        Task<int> RealizarVentaAsync(int empresaId, RealizarVentaRequest request);
        Task<string> GetSiguienteFacturaAsync(int empresaId);
        Task<byte[]> GenerarFacturaPdfAsync(int ventaId);
        Task<byte[]> GenerarCompraPdfAsync(int compraId);
    }
}
