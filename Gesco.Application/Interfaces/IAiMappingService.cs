using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IAiMappingService
    {
        /// <summary>
        /// Recibe una muestra de datos (ej. un JSON crudo) o el DDL de la base de datos de origen,
        /// y utiliza Inteligencia Artificial para determinar a qué columnas de la base de GESCO corresponden.
        /// </summary>
        /// <param name="rawDataOrigen">Datos crudos exportados o capturados vía Scraping</param>
        /// <param name="tablaDestinoGesco">El nombre de la entidad/tabla en GESCO (ej. "Personas", "Facturas")</param>
        /// <returns>Un diccionario donde la Key es la columna origen y el Value es la propiedad destino en GESCO</returns>
        Task<Dictionary<string, string>> GenerarMapeoSemanticoAsync(string rawDataOrigen, string tablaDestinoGesco);
    }
}
