using DocumentFormat.OpenXml.Math;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace Gesco.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context; // Tu DbContext

        public AuthService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
                throw new Exception("El correo ya existe.");

            // Usamos una transacción para asegurar que se cree el usuario Y sus módulos
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int empresaIdAsignada = request.EmpresaId;
                
                // Si es self-service y no tiene empresa, la creamos al vuelo
                if (empresaIdAsignada <= 0)
                {
                    var nuevaEmpresa = new Empresa
                    {
                        RazonSocial = string.IsNullOrEmpty(request.EmpresaNombre) ? "Nueva Empresa" : request.EmpresaNombre,
                        NombreFantasia = string.IsNullOrEmpty(request.EmpresaNombre) ? "Nueva Empresa" : request.EmpresaNombre,
                        RUC = "000000-0",
                        Activo = true,
                        Rubro = "RETAIL" // Default
                    };
                    _context.Empresas.Add(nuevaEmpresa);
                    await _context.SaveChangesAsync();
                    empresaIdAsignada = nuevaEmpresa.EmpresaId;
                }

                var nuevoUsuario = new Usuario
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Rol = request.Rol ?? "AGENTE",
                    EmpresaId = empresaIdAsignada,
                    RequiereCambioPass = true, // ACTIVAMOS LA BANDERA
                    Estado = "ACTIVO"
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // Asignamos los módulos seleccionados
                if (request.ModulosIds != null)
                {
                    foreach (var modId in request.ModulosIds)
                    {
                        _context.UsuarioModulos.Add(new UsuarioModulo
                        {
                            UsuarioId = nuevoUsuario.UsuarioId,
                            ModuloId = modId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new { mensaje = "Usuario registrado con éxito en GESCO" };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthUserResponse?> LoginAsync(LoginRequest request)
        {
            var usuario = await _context.Usuarios
                                        .Include(u => u.Empresa)
                                        .FirstOrDefaultAsync(u => u.Email == request.Email);

            // 1. Validar credenciales básicas
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                return null;

            // 2. VALIDACIÓN DE ESTADO (EL BLOQUEO)
            if (usuario.Estado != "ACTIVO")
            {
                // Lanzamos una excepción con el mensaje exacto para el Frontend
                throw new Exception($"Tu cuenta se encuentra en estado {usuario.Estado}. Contacta con el administrador de GESCO.");
            }

            // 3. Si está activo, devolvemos el objeto de éxito
            return new AuthUserResponse
            {
                Id = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol,
                EmpresaId = usuario.EmpresaId,
                RequiereCambioPass = usuario.RequiereCambioPass,
                Rubro = usuario.Empresa.Rubro
            };
        }

        public async Task<AuthUserResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
                throw new Exception("El correo de Google no es válido.");

            var usuario = await _context.Usuarios
                                        .Include(u => u.Empresa)
                                        .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (usuario == null)
            {
                var primeraEmpresa = await _context.Empresas.FirstOrDefaultAsync();
                int empresaId = primeraEmpresa?.EmpresaId ?? 1;

                bool esAdminGmail = request.Email.ToLower() == "rody.moran.py@gmail.com" || request.Email.ToLower().Contains("rody.moran") || request.Email.ToLower().Contains("admin");
                int empIdFinal = esAdminGmail && await _context.Empresas.AnyAsync(e => e.EmpresaId == 7) ? 7 : empresaId;

                usuario = new Usuario
                {
                    Nombre = string.IsNullOrEmpty(request.Nombre) ? request.Email.Split('@')[0] : request.Nombre,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Rol = esAdminGmail ? "ADMIN" : "AGENTE",
                    EmpresaId = empIdFinal,
                    RequiereCambioPass = false,
                    Estado = "ACTIVO"
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                usuario = await _context.Usuarios
                                        .Include(u => u.Empresa)
                                        .FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);
            }
            else
            {
                if (usuario.Email.ToLower() == "rody.moran.py@gmail.com" || usuario.Email.ToLower().Contains("rody.moran"))
                {
                    bool cambios = false;
                    if (usuario.Rol != "ADMIN") { usuario.Rol = "ADMIN"; cambios = true; }
                    if (usuario.EmpresaId != 7 && await _context.Empresas.AnyAsync(e => e.EmpresaId == 7)) { usuario.EmpresaId = 7; cambios = true; }
                    if (cambios) await _context.SaveChangesAsync();
                }
            }

            if (usuario.Estado != "ACTIVO")
            {
                throw new Exception($"Tu cuenta se encuentra en estado {usuario.Estado}. Contacta con el administrador de GESCO.");
            }

            if (usuario.RequiereCambioPass)
            {
                usuario.RequiereCambioPass = false;
                await _context.SaveChangesAsync();
            }

            return new AuthUserResponse
            {
                Id = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol,
                EmpresaId = usuario.EmpresaId,
                RequiereCambioPass = false,
                Rubro = usuario.Empresa?.Rubro ?? "RETAIL"
            };
        }

        public async Task<bool> UpdatePasswordAsync(UpdatePasswordRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);

            if (usuario == null) return false;

            // 1. Ciframos la nueva contraseña definitiva
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);

            // 2. Apagamos la bandera de cambio obligatorio
            usuario.RequiereCambioPass = false;

            // 3. Opcional: Actualizamos la fecha de conexión
            usuario.UltimaConexion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ModuloDto>> GetModulosPorUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
            if (usuario == null) return new List<ModuloDto>();

            var todosLosModulos = await _context.Modulos.ToListAsync();
            var licenciasActivas = await _context.LicenciasModulos
                .Where(l => l.EmpresaId == usuario.EmpresaId && l.Activo && (l.FechaExpiracion == null || l.FechaExpiracion > DateTime.Now))
                .Select(l => l.ModuloId)
                .ToListAsync();

            var result = new List<ModuloDto>();

            foreach (var mod in todosLosModulos)
            {
                bool isContratado = false;
                
                // Bypass: Condominio Agus (7), Vordia Interno, o Rol ADMIN
                var rubroEmpresa = usuario.Empresa?.Rubro?.ToUpperInvariant() ?? "";
                if (usuario.EmpresaId == 7
                    || rubroEmpresa == "VORDIA"
                    || usuario.Rol == "ADMIN"
                    || usuario.Rol == "Admin"
                    || usuario.Email.ToLower().Contains("rody.moran"))
                {
                    isContratado = true;
                }
                else
                {
                    // Core modules can be free or based on license. Let's assume all modules require license except Config, or if they have the license ID.
                    // For now, if they have a license in the DB, it's contracted.
                    if (licenciasActivas.Contains(mod.ModuloId))
                    {
                        isContratado = true;
                    }
                }

                result.Add(new ModuloDto
                {
                    ModuloId = mod.ModuloId,
                    NombreModulo = mod.NombreModulo,
                    Icono = mod.Icono,
                    RutaFrontend = mod.RutaFrontend,
                    IsContratado = isContratado
                });
            }

            return result;
        }

        public async Task<List<Empresa>> GetEmpresasActivasAsync()
        {
            return await _context.Empresas.ToListAsync();
        }
    }
}
