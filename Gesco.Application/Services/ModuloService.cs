using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class ModuloService : IModuloService
    {
        private readonly IApplicationDbContext _context;

        public ModuloService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ModuloDto>> GetModulosPorRubroAsync(string rubro)
        {
            return await _context.Modulos
                .Where(m => m.RubroAsociado == rubro)
                .Select(m => new ModuloDto
                {
                    ModuloId = m.ModuloId,
                    NombreModulo = m.NombreModulo,
                    Icono = m.Icono,
                    RutaFrontend = m.RutaFrontend
                })
                .ToListAsync();
        }

        public async Task<List<ModuloDto>> GetModulosByEmpresaIdAsync(int empresaId)
        {
            // 1. Primero buscamos la empresa para saber qué rubro tiene
            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (empresa == null) return new List<ModuloDto>();

            // 2. Ahora que tenemos el rubro (ej: 'INMOBILIARIA'), buscamos sus módulos
            var modulos = await _context.Modulos
                .Where(m => m.RubroAsociado == empresa.Rubro) // Aquí usamos el rubro de la empresa
                .Select(m => new ModuloDto
                {
                    ModuloId = m.ModuloId,
                    NombreModulo = m.NombreModulo,
                    Icono = m.Icono,
                    RutaFrontend = m.RutaFrontend
                })
                .ToListAsync();

            return modulos;
        }
    }
}
