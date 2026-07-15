using Gesco.Application.DTOs;

namespace Gesco.Application.Interfaces
{
    public interface IGastoMantenimientoService
    {
        Task<IReadOnlyList<GastoServicioMantenimientoDto>> GetServiciosByUnidadAsync(int unidadId, string? tipoServicio = null);
        Task<GastoServicioMantenimientoDto?> GetServicioByIdAsync(int gastoId);
        Task<GastoServicioMantenimientoDto> CreateServicioAsync(GastoServicioMantenimientoRequest request);
        Task<GastoServicioMantenimientoDto?> UpdateServicioAsync(int gastoId, GastoServicioMantenimientoRequest request);
        Task<bool> DeleteServicioAsync(int gastoId);
    }
}
