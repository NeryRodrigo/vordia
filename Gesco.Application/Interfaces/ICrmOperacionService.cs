using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ICrmOperacionService
    {
        Task<List<CrmTareaDto>> ListarTareasAsync(int empresaId, string? estado = null);
        Task<CrmTareaDto> CrearTareaAsync(CrmTareaUpsertRequest request);
        Task<CrmTareaDto?> ActualizarTareaAsync(int tareaId, CrmTareaUpsertRequest request);
        Task<CrmTareaDto?> CambiarEstadoTareaAsync(int tareaId, int empresaId, string estado);
        Task<bool> EliminarTareaAsync(int tareaId, int empresaId);

        Task<List<CrmSocioDto>> ListarSociosAsync(int empresaId);
        Task<CrmSocioDto> UpsertSocioAsync(CrmSocioUpsertRequest request, int? socioId = null);
        Task<bool> EliminarSocioAsync(int socioId, int empresaId);

        Task<CrmFlujoCajaDto> ObtenerFlujoCajaAsync(int empresaId);

        Task<List<CrmRoadmapDto>> ListarRoadmapAsync(int empresaId, string? tipo = null, string? estado = null);
        Task<CrmRoadmapDto?> ObtenerRoadmapAsync(int roadmapId, int empresaId);
        Task<CrmRoadmapDto> CrearRoadmapAsync(CrmRoadmapUpsertRequest request);
        Task<CrmRoadmapDto?> ActualizarRoadmapAsync(int roadmapId, CrmRoadmapUpsertRequest request);
        Task<CrmRoadmapDto?> CambiarEstadoRoadmapAsync(int roadmapId, int empresaId, string estado, int? usuarioId = null, string? usuarioNombre = null);
        Task<CrmRoadmapDto?> AgregarNotaRoadmapAsync(int roadmapId, CrmRoadmapNotaRequest request);
        Task<bool> EliminarRoadmapAsync(int roadmapId, int empresaId);
    }
}
