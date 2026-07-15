using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class ReservaService : IReservaService
    {
        private readonly IApplicationDbContext _context;

        public ReservaService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reserva>> GetAllByEmpresaAsync(int empresaId)
        {
            return await _context.Reservas
                .Include(r => r.Habitacion)
                .Include(r => r.Huesped)
                .Where(r => r.EmpresaId == empresaId)
                .ToListAsync();
        }

        public async Task<Reserva?> GetByIdAsync(int reservaId)
        {
            return await _context.Reservas
                .Include(r => r.Habitacion)
                .Include(r => r.Huesped)
                .FirstOrDefaultAsync(r => r.ReservaId == reservaId);
        }

        public async Task<Reserva> CreateAsync(Reserva reserva)
        {
            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            return reserva;
        }

        public async Task UpdateAsync(Reserva reserva)
        {
            _context.Reservas.Update(reserva);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int reservaId)
        {
            var reserva = await _context.Reservas.FindAsync(reservaId);
            if (reserva != null)
            {
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }
        }
    }
}
