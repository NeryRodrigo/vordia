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
        Task<List<AgroAnimalDto>> ListarAnimalesAsync(int empresaId, int? establecimientoId = null, string? estado = null, string? etapa = null);
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

        // Rediseño por módulos
        Task<AgroConfiguracionDto> ObtenerConfiguracionAsync(int empresaId);
        Task<AgroConfiguracionDto> GuardarConfiguracionAsync(AgroConfiguracionRequest request);
        Task<AgroEjecutivoDto?> EjecutivoAsync(int empresaId, int? establecimientoId = null);
        Task<AgroFeedDto?> FeedAsync(int empresaId, int? establecimientoId = null, int dias = 70);
        Task<AgroPrincipalDto?> PrincipalAsync(int empresaId, int dias = 30);
        Task<List<AgroInsumoDto>> ListarInventarioAsync(int empresaId);
        Task<AgroInsumoDto> CrearInsumoAsync(AgroInsumoRequest request);
        Task<List<AgroMovimientoDto>> ListarMovimientosAsync(int empresaId, int insumoId);
        Task<AgroInsumoDto> RegistrarMovimientoAsync(int insumoId, AgroMovimientoRequest request);
        Task<AgroLoteDto> GuardarLoteAsync(AgroLoteRequest request, int? loteId = null);
        Task<List<AgroSanidadDto>> ListarSanidadAsync(int empresaId, int? establecimientoId = null, string? estado = null);
        Task<AgroSanidadDto> RegistrarSanidadAsync(AgroSanidadCreateRequest request);
        Task<bool> AplicarSanidadAsync(int sanidadId, int empresaId);
        Task<AgroAnimalDto> CambiarEtapaAsync(int animalId, AgroCambioEtapaRequest request);
        Task<AgroFichaAnimalDto?> FichaAnimalAsync(int empresaId, int animalId);
    }
}
