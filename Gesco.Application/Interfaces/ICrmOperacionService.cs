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

        // Capital / Empleados / Objetivos (HQ)
        Task<CrmCapitalResumenDto> ObtenerCapitalAsync(int empresaId);
        Task<CrmCapitalDto> RegistrarCapitalAsync(CrmCapitalUpsertRequest request);
        Task<bool> EliminarCapitalAsync(int capitalId, int empresaId);

        Task<CrmPropuestasResumenDto> ListarPropuestasAsync(int empresaId, int? clienteId = null, string? estado = null);
        Task<CrmPropuestaDto> UpsertPropuestaAsync(CrmPropuestaUpsertRequest request, int? propuestaId = null);
        Task<bool> EliminarPropuestaAsync(int propuestaId, int empresaId);

        Task<CrmGastosResumenDto> ListarGastosAsync(int empresaId, string? estado = null);
        Task<CrmGastoDto> UpsertGastoAsync(CrmGastoUpsertRequest request, int? gastoId = null);
        Task<CrmGastoDto?> MarcarGastoPagadoAsync(int gastoId, CrmGastoPagoRequest request);
        Task<bool> EliminarGastoAsync(int gastoId, int empresaId);

        Task<CrmEmpleadosResumenDto> ListarEmpleadosHqAsync(int empresaId);
        Task<CrmEmpleadoHqDto> UpsertEmpleadoHqAsync(CrmEmpleadoHqUpsertRequest request, int? empleadoId = null);
        Task<bool> EliminarEmpleadoHqAsync(int empleadoId, int empresaId);

        Task<List<CrmObjetivoDto>> ListarObjetivosAsync(int empresaId, string? ambito = null, int? tareaId = null);
        Task<CrmObjetivoDto> UpsertObjetivoAsync(CrmObjetivoUpsertRequest request, int? objetivoId = null);
        Task<bool> EliminarObjetivoAsync(int objetivoId, int empresaId);

        Task<List<CrmProductoDto>> ListarProductosAsync(int empresaId);
        Task<CrmProductoDto> UpsertProductoAsync(CrmProductoUpsertRequest request, int? productoId = null);
        Task<bool> EliminarProductoAsync(int productoId, int empresaId);

        Task<List<CrmProveedorDto>> ListarProveedoresAsync(int empresaId, string? estado = null);
        Task<CrmProveedorDto> UpsertProveedorAsync(CrmProveedorUpsertRequest request, int? proveedorId = null);
        Task<bool> EliminarProveedorAsync(int proveedorId, int empresaId);

        Task<List<CrmProyectoDto>> ListarProyectosAsync(int empresaId, string? estado = null, string? productoCodigo = null);
        Task<CrmProyectoDto> UpsertProyectoAsync(CrmProyectoUpsertRequest request, int? proyectoId = null);
        Task<bool> EliminarProyectoAsync(int proyectoId, int empresaId);
    }
}
