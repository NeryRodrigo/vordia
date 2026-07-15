using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ICloudExtractionService
    {
        /// <summary>
        /// Inicia un bot invisible (RPA) para navegar a una URL de un sistema Cloud (ej. Ábaco),
        /// intercepta las respuestas JSON de las grillas de datos y devuelve un diccionario bruto.
        /// </summary>
        /// <param name="loginUrl">URL para inicio de sesión en el sistema origen.</param>
        /// <param name="dataUrl">URL donde está la tabla/grilla de datos que queremos capturar.</param>
        /// <param name="username">Usuario de la competencia.</param>
        /// <param name="password">Contraseña de la competencia.</param>
        /// <returns>JSON en string capturado directamente desde la red del sistema ajeno.</returns>
        Task<string> ExtraerDatosJsonNubeAsync(string loginUrl, string dataUrl, string username, string password);
    }
}
