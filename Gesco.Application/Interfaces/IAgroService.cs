using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IAgroService
    {
        Task<object> PingAsync();
        Task<object?> ResolverContextoAgrofieldAsync();
        Task<List<AgroEstablecimientoDto>> ListarEstablecimientosAsync(int empresaId);
        Task<AgroDashboardDto?> DashboardAsync(int empresaId, int? establecimientoId = null);
        Task<List<AgroAnimalDto>> ListarAnimalesAsync(int empresaId, int? establecimientoId = null, string? estado = null);
        Task<AgroAnimalDto> UpsertAnimalAsync(AgroAnimalUpsertRequest request, int? animalId = null);
        Task<List<AgroPesajeDto>> ListarPesajesAsync(int empresaId, int animalId);
        Task<AgroPesajeDto> RegistrarPesajeAsync(AgroPesajeCreateRequest request);
        Task<List<AgroRacionDto>> ListarRacionesAsync(int empresaId, int? loteId = null);
        Task<AgroRacionDto> RegistrarRacionAsync(AgroRacionCreateRequest request);
        Task<List<AgroLoteDto>> ListarLotesAsync(int empresaId, int? establecimientoId = null);
        Task<List<AgroDispositivoDto>> ListarDispositivosAsync(int empresaId, int? establecimientoId = null);
        Task<List<AgroAlertaDto>> ListarAlertasAsync(int empresaId, int? establecimientoId = null, bool soloAbiertas = true);
        Task<bool> ResolverAlertaAsync(int alertaId, int empresaId);
        Task<List<AgroZonaDto>> ListarZonasAsync(int empresaId, int? establecimientoId = null);
        Task<AgroAnimalDto?> SimularUbicacionAsync(AgroSimularUbicacionRequest request);
    }
}
