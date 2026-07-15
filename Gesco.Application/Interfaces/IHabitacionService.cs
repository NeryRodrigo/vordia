using System.Collections.Generic;
using System.Threading.Tasks;
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IHabitacionService
    {
        Task<IEnumerable<Habitacion>> GetAllByEmpresaAsync(int empresaId);
        Task<Habitacion?> GetByIdAsync(int habitacionId);
        Task<Habitacion> CreateAsync(Habitacion habitacion);
        Task UpdateAsync(Habitacion habitacion);
        Task DeleteAsync(int habitacionId);
    }
}
