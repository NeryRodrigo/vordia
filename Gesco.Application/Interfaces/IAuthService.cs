using Gesco.Application.DTOs;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IAuthService
    {
        Task<object> RegisterAsync(RegisterRequest request);
        Task<object> LoginAsync(LoginRequest request);
        Task<object> GoogleLoginAsync(GoogleLoginRequest request);
        Task<bool> UpdatePasswordAsync(UpdatePasswordRequest request);
        Task<List<ModuloDto>> GetModulosPorUsuarioAsync(int usuarioId);
        Task<List<Empresa>> GetEmpresasActivasAsync();
    }
}
