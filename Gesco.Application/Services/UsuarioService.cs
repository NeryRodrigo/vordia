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
    public class UsuarioService : IUsuarioService
    {
        private readonly IApplicationDbContext _context; // Tu DbContext

        public UsuarioService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UsuarioDto>> GetUsuariosPorEmpresaAsync(int empresaId)
        {
            // Traemos todos sin filtros de estado o rol, para que aparezcan Karen y Admin
            return await _context.Usuarios
                .Where(u => u.EmpresaId == empresaId || u.Email.ToLower() == "rody.moran.py@gmail.com" || u.Email.ToLower().Contains("rody.moran"))
                .OrderByDescending(u => u.UsuarioId) // Los últimos creados primero
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Rol = u.Rol,
                    Estado = u.Estado,
                    RequiereCambioPass = u.RequiereCambioPass,
                    Iniciales = u.Nombre.Length >= 2 ? u.Nombre.Substring(0, 2).ToUpper() : "U",
                    ModulosIds = u.UsuarioModulos.Select(um => um.ModuloId).ToList()
                })
                .ToListAsync();
        }

        public async Task<bool> CambiarRolAsync(CambiarRolRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null) return false;

            usuario.Rol = request.Rol;
            await _context.SaveChangesAsync();
            return true;
        }

        // Lógica para Eliminar (Borrado físico)
        public async Task<bool> EliminarUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            // Primero eliminamos sus permisos en la tabla intermedia para evitar error de FK
            var permisos = _context.UsuarioModulos.Where(um => um.UsuarioId == usuarioId);
            _context.UsuarioModulos.RemoveRange(permisos);

            // Luego eliminamos al usuario
            _context.Usuarios.Remove(usuario);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarUsuarioCompletoAsync(ActualizarUsuarioRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null) return false;

            // Validación de Email duplicado (que no sea el de otro usuario)
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == request.Email && u.UsuarioId != request.UsuarioId);

            if (emailExiste) throw new Exception("El correo ya está asignado a otro colaborador.");

            // Actualizamos los campos
            usuario.Nombre = request.Nombre;
            usuario.Email = request.Email;
            usuario.Rol = request.Rol;
            usuario.Estado = request.Estado;

            // --- Sincronización de Módulos ---
            // 1. Eliminamos los permisos actuales
            var permisosActuales = _context.UsuarioModulos.Where(um => um.UsuarioId == request.UsuarioId);
            _context.UsuarioModulos.RemoveRange(permisosActuales);

            // 2. Insertamos los nuevos permisos
            if (request.ModulosIds != null && request.ModulosIds.Count > 0)
            {
                foreach (var modId in request.ModulosIds)
                {
                    _context.UsuarioModulos.Add(new Gesco.Domain.Entities.UsuarioModulo
                    {
                        UsuarioId = request.UsuarioId,
                        ModuloId = modId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
