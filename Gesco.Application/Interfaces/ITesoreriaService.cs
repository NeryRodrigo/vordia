using Gesco.Application.DTOs;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface ITesoreriaService
    {
        Task<FlujoCajaResponse> GetFlujoCajaAsync(int empresaId, int mes, int anio);
        Task EliminarMovimientoAsync(int empresaId, string tipo, int idOrigen);
        Task EditarMovimientoAsync(int empresaId, string tipo, int idOrigen, decimal nuevoMonto, string nuevaDescripcion);
    }
}
