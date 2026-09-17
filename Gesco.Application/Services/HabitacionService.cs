using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class HabitacionService : IHabitacionService
    {
        private readonly IApplicationDbContext _context;

        public HabitacionService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Habitacion>> GetAllByEmpresaAsync(int empresaId)
        {
            return await _context.Habitaciones
                .Where(h => h.EmpresaId == empresaId)
                .ToListAsync();
        }

        public async Task<Habitacion?> GetByIdAsync(int habitacionId)
        {
            return await _context.Habitaciones.FindAsync(habitacionId);
        }

        public async Task<Habitacion> CreateAsync(Habitacion habitacion)
        {
            _context.Habitaciones.Add(habitacion);
            await _context.SaveChangesAsync();
            return habitacion;
        }

        public async Task UpdateAsync(Habitacion habitacion)
        {
            _context.Habitaciones.Update(habitacion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int habitacionId)
        {
            var habitacion = await _context.Habitaciones.FindAsync(habitacionId);
            if (habitacion == null) return;

            // Reservas tiene FK Restrict → borrar primero las reservas de la habitación
            var reservas = await _context.Reservas
                .Where(r => r.HabitacionId == habitacionId)
                .ToListAsync();
            if (reservas.Count > 0)
            {
                _context.Reservas.RemoveRange(reservas);
            }

            _context.Habitaciones.Remove(habitacion);
            await _context.SaveChangesAsync();
        }
    }
}
