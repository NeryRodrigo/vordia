using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    /// <summary>
    /// Repositorio de documentos de Vordia HQ. Guarda la ficha y la ruta del archivo;
    /// el binario lo escribe el controlador en wwwroot/crm-documentos.
    /// </summary>
    public interface ICrmDocumentosService
    {
        Task<List<CrmDocumentoDto>> ListarAsync(
            int empresaId,
            int? clienteId = null,
            string? categoria = null,
            string? q = null,
            bool soloPorVencer = false);

        Task<CrmDocumentoDto?> ObtenerAsync(int documentoId, int empresaId);

        Task<CrmDocumentosResumenDto> ResumenAsync(int empresaId);

        Task<CrmDocumentoDto> CrearConArchivoAsync(CrmDocumentoArchivoRequest request);

        Task<CrmDocumentoDto> CrearEnlaceAsync(CrmDocumentoEnlaceRequest request);

        Task<CrmDocumentoDto?> ActualizarMetadatosAsync(int documentoId, CrmDocumentoMetadatosRequest request);

        Task<bool> EliminarAsync(int documentoId, int empresaId);

        /// <summary>Categorías admitidas por la tabla, para armar los combos del front.</summary>
        IReadOnlyList<string> Categorias { get; }
    }
}
