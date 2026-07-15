using System.Collections.Generic;
using System.Threading.Tasks;
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IReservaService
    {
        Task<IEnumerable<Reserva>> GetAllByEmpresaAsync(int empresaId);
        Task<Reserva?> GetByIdAsync(int reservaId);
        Task<Reserva> CreateAsync(Reserva reserva);
        Task UpdateAsync(Reserva reserva);
        Task DeleteAsync(int reservaId);
    }
}
