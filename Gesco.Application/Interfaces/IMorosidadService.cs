using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IMorosidadService
    {
        Task<MorosidadInquilino> RegistrarMorosidadAsync(int inquilinoId, int unidadId, decimal montoTotal, int cantidadCuotas, string observaciones, decimal montoInteresDiario);
        Task<IEnumerable<MorosidadInquilino>> ObtenerMorosidadesAsync(int? unidadId = null);
        Task<MorosidadInquilino?> ObtenerDetalleMorosidadAsync(int morosidadId);
        Task<CuotaMorosidad> CobrarCuotaAsync(int cuotaId, string metodoPago, int contratoActualId, string? notas);
        Task<SeguimientoMorosidad> AgregarSeguimientoAsync(int cuotaId, string observacion);
    }
}
