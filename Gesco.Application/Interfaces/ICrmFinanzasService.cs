using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ICrmFinanzasService
    {
        Task<List<CrmContratoDto>> ListarContratosAsync(int empresaId, string? estadoContrato = null);
        Task<CrmContratoDto?> ObtenerContratoAsync(int contratoId, int empresaId);
        Task<CrmContratoDto> CrearContratoAsync(CrmContratoUpsertRequest request);
        Task<CrmContratoDto?> ActualizarContratoAsync(int contratoId, CrmContratoUpsertRequest request);
        Task<bool> EliminarContratoAsync(int contratoId, int empresaId);

        Task<List<CrmPagoDto>> ListarPagosAsync(int empresaId, int? contratoId = null);
        Task<CrmPagoDto> RegistrarPagoAsync(CrmPagoCreateRequest request);
        Task<CrmPagoDto?> ActualizarPagoAsync(int pagoId, CrmPagoCreateRequest request);
        Task<bool> EliminarPagoAsync(int pagoId, int empresaId);
        Task<CrmIngresosResumenDto> ObtenerIngresosAsync(int empresaId);
    }
}
