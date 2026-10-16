using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    /// <summary>VORDIA HQ — radar de pedidos públicos de sistemas y aplicaciones.</summary>
    public interface IMercadoService
    {
        Task<MercadoSyncResultDto> SincronizarAsync(int empresaId, CancellationToken ct = default);
        Task<int> SincronizarTodasAsync(CancellationToken ct = default);
        Task<List<MercadoNecesidadDto>> ListarAsync(int empresaId, string? estado, string? vertical, string? fuente, int? minPuntaje, string? q, int? dias,
            string? alcance = null, int? minConfianza = null, bool conContacto = false);
        Task<MercadoResumenDto> ResumenAsync(int empresaId);
        Task<MercadoNecesidadDto> CrearManualAsync(MercadoManualRequest request);
        Task<MercadoNecesidadDto?> ActualizarAsync(int necesidadId, MercadoActualizarRequest request);
        Task<MercadoConversionResultDto> ConvertirACrmAsync(int necesidadId, MercadoConversionRequest request);
        Task<MercadoConversionResultDto> ConvertirAProductoAsync(int necesidadId, MercadoConversionRequest request);
        Task<MercadoConversionResultDto> OportunidadAProductoAsync(MercadoOportunidadProductoRequest request);
        Task<List<MercadoFuenteDto>> ListarFuentesAsync(int empresaId);
        Task<MercadoFuenteDto> GuardarFuenteAsync(MercadoFuenteRequest request, int? fuenteId = null);
        Task<bool> EliminarFuenteAsync(int fuenteId, int empresaId);
    }
}
