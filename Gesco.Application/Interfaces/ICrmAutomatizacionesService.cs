using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    /// <summary>
    /// Motor de automatizaciones de Vordia HQ: administra las reglas y las ejecuta,
    /// tanto a pedido desde la pantalla como cada ciclo del servicio en segundo plano.
    /// </summary>
    public interface ICrmAutomatizacionesService
    {
        Task<List<CrmAutomatizacionDto>> ListarAsync(int empresaId, bool? soloActivas = null);

        Task<CrmAutomatizacionDto?> ObtenerAsync(int automatizacionId, int empresaId);

        Task<CrmAutomatizacionDto> CrearAsync(CrmAutomatizacionUpsertRequest request);

        Task<CrmAutomatizacionDto?> ActualizarAsync(int automatizacionId, CrmAutomatizacionUpsertRequest request);

        /// <summary>Enciende o pausa la regla sin borrarla.</summary>
        Task<CrmAutomatizacionDto?> CambiarEstadoAsync(int automatizacionId, CrmAutomatizacionEstadoRequest request);

        Task<bool> EliminarAsync(int automatizacionId, int empresaId);

        Task<List<CrmAutomatizacionEjecucionDto>> ListarEjecucionesAsync(
            int empresaId,
            int? automatizacionId = null,
            int top = 50);

        /// <summary>
        /// Corre el motor. Con automatizacionId corre una sola regla (aunque esté pausada,
        /// para poder probarla); sin él, todas las reglas encendidas de la empresa.
        /// Con simular = true informa qué haría sin enviar correos ni crear tareas.
        /// </summary>
        Task<CrmAutomatizacionCorridaDto> EjecutarAsync(
            int empresaId,
            int? automatizacionId = null,
            bool simular = false);

        /// <summary>Recorre todas las empresas que tengan reglas encendidas. La usa el servicio en segundo plano.</summary>
        Task<CrmAutomatizacionCorridaDto> EjecutarTodasAsync();

        CrmAutomatizacionCatalogoDto Catalogo();
    }
}
