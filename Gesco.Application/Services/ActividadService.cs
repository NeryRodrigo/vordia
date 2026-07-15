using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;

namespace Gesco.Application.Services
{
    public class ActividadService : IActividadService
    {
        private readonly IApplicationDbContext _context;

        public ActividadService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Actividad>> ObtenerActividadesAsync(int empresaId)
        {
            return await _context.Actividades
                .Where(a => a.EmpresaId == empresaId)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }

        public async Task<Actividad?> ObtenerActividadPorIdAsync(int id)
        {
            return await _context.Actividades.FindAsync(id);
        }

        public async Task<Actividad> CrearActividadAsync(Actividad actividad)
        {
            if (actividad.Fecha == default)
            {
                actividad.Fecha = DateTime.Now;
            }
            
            // Si viene un ResponsableId y no se especificó nombre, rellenar el ResponsableNombre automáticamente
            if (actividad.ResponsableId.HasValue && string.IsNullOrEmpty(actividad.ResponsableNombre))
            {
                var emp = await _context.Empleados.FindAsync(actividad.ResponsableId.Value);
                if (emp != null)
                {
                    actividad.ResponsableNombre = emp.NombreCompleto;
                }
            }
            if (actividad.ResponsableNombre == null)
            {
                actividad.ResponsableNombre = string.Empty;
            }

            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();
            return actividad;
        }

        public async Task<bool> ActualizarActividadAsync(Actividad actividad)
        {
            var dbAct = await _context.Actividades.FindAsync(actividad.ActividadId);
            if (dbAct == null) return false;

            dbAct.Fecha = actividad.Fecha;
            dbAct.Titulo = actividad.Titulo;
            dbAct.Descripcion = actividad.Descripcion;
            dbAct.ResponsableId = actividad.ResponsableId;
            dbAct.Estado = actividad.Estado;

            if (actividad.ResponsableId.HasValue && string.IsNullOrEmpty(actividad.ResponsableNombre))
            {
                var emp = await _context.Empleados.FindAsync(actividad.ResponsableId.Value);
                if (emp != null)
                {
                    dbAct.ResponsableNombre = emp.NombreCompleto;
                }
            }
            else
            {
                dbAct.ResponsableNombre = actividad.ResponsableNombre ?? string.Empty;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarActividadAsync(int id)
        {
            var dbAct = await _context.Actividades.FindAsync(id);
            if (dbAct == null) return false;

            _context.Actividades.Remove(dbAct);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
