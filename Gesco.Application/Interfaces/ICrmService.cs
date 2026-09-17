using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ICrmService
    {
        Task<List<CrmClienteDto>> ListarClientesAsync(int empresaId, string? estado = null, string? q = null);
        Task<CrmClienteDto?> ObtenerClienteAsync(int clienteId, int empresaId);
        Task<CrmClienteDto> CrearClienteAsync(CrmClienteUpsertRequest request);
        Task<CrmClienteDto> CrearLeadWebAsync(CrmLeadWebRequest request);
        Task<CrmClienteDto?> ActualizarClienteAsync(int clienteId, CrmClienteUpsertRequest request);
        Task<bool> EliminarClienteAsync(int clienteId, int empresaId);
        Task<CrmClienteDto?> CambiarEstadoAsync(int clienteId, int empresaId, CrmCambiarEstadoRequest request);
        Task<List<CrmHistorialDto>> ListarHistorialAsync(int clienteId, int empresaId);
        Task<CrmHistorialDto> AgregarHistorialAsync(int clienteId, CrmHistorialCreateRequest request);
        Task<List<CrmPipelineResumenDto>> ObtenerPipelineAsync(int empresaId);
    }
}
