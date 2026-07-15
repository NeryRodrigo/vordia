using Gesco.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<List<UsuarioDto>> GetUsuariosPorEmpresaAsync(int empresaId);
        Task<bool> CambiarRolAsync(CambiarRolRequest request);
        Task<bool> EliminarUsuarioAsync(int usuarioId);

        Task<bool> ActualizarUsuarioCompletoAsync(ActualizarUsuarioRequest request);
    }
}
