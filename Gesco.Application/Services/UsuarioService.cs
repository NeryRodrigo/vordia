using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Helpers;
using Gesco.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IApplicationDbContext _context;

        public UsuarioService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UsuarioDto>> GetUsuariosPorEmpresaAsync(int empresaId)
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.EmpresaId == empresaId || u.Email.ToLower() == "rody.moran.py@gmail.com" || u.Email.ToLower().Contains("rody.moran"))
                .OrderByDescending(u => u.UsuarioId)
                .Select(u => new UsuarioDto
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Rol = u.Rol,
                    Estado = u.Estado,
                    RequiereCambioPass = u.RequiereCambioPass,
                    Iniciales = u.Nombre.Length >= 2 ? u.Nombre.Substring(0, 2).ToUpper() : "U",
                    ModulosIds = u.UsuarioModulos.Select(um => um.ModuloId).ToList(),
                    PermisosIds = new List<string>()
                })
                .ToListAsync();

            var ids = usuarios.Select(u => u.UsuarioId).ToList();
            var raw = await _context.Usuarios
                .Where(u => ids.Contains(u.UsuarioId))
                .Select(u => new { u.UsuarioId, u.PermisosJson })
                .ToListAsync();
            var map = raw.ToDictionary(x => x.UsuarioId, x => PermisosJsonHelper.Parse(x.PermisosJson));
            foreach (var u in usuarios)
            {
                if (map.TryGetValue(u.UsuarioId, out var perms))
                    u.PermisosIds = perms;
            }

            return usuarios;
        }

        public async Task<bool> CambiarRolAsync(CambiarRolRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null) return false;

            usuario.Rol = request.Rol;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            var permisos = _context.UsuarioModulos.Where(um => um.UsuarioId == usuarioId);
            _context.UsuarioModulos.RemoveRange(permisos);

            _context.Usuarios.Remove(usuario);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarUsuarioCompletoAsync(ActualizarUsuarioRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null) return false;

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == request.Email && u.UsuarioId != request.UsuarioId);

            if (emailExiste) throw new Exception("El correo ya está asignado a otro colaborador.");

            usuario.Nombre = request.Nombre;
            usuario.Email = request.Email;
            usuario.Rol = request.Rol;
            usuario.Estado = request.Estado;

            var permisosActuales = _context.UsuarioModulos.Where(um => um.UsuarioId == request.UsuarioId);
            _context.UsuarioModulos.RemoveRange(permisosActuales);

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

        public async Task<List<string>> GetPermisosUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return new List<string>();
            return PermisosJsonHelper.Parse(usuario.PermisosJson);
        }

        public async Task<bool> GuardarPermisosUsuarioAsync(int usuarioId, List<string> permisosIds)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;
            usuario.PermisosJson = PermisosJsonHelper.Serialize(permisosIds);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
