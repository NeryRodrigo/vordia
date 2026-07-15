using System.Collections.Generic;
using System.Threading.Tasks;
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IActividadService
    {
        Task<IEnumerable<Actividad>> ObtenerActividadesAsync(int empresaId);
        Task<Actividad?> ObtenerActividadPorIdAsync(int id);
        Task<Actividad> CrearActividadAsync(Actividad actividad);
        Task<bool> ActualizarActividadAsync(Actividad actividad);
        Task<bool> EliminarActividadAsync(int id);
    }
}
