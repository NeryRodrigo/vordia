using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Application.Services;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IModuloService _moduloService;

        public UsuarioController(IUsuarioService usuarioService, IModuloService moduloService)
        {
            _usuarioService = usuarioService;
            _moduloService = moduloService;
        }

        [HttpGet("lista/{empresaId}")]
        public async Task<IActionResult> GetLista(int empresaId)
        {
            var usuarios = await _usuarioService.GetUsuariosPorEmpresaAsync(empresaId);
            return Ok(usuarios);
        }

        [HttpGet("modulos-por-rubro/{rubro}")]
        public async Task<IActionResult> GetModulosPorRubro(string rubro)
        {
            try
            {
                var modulos = await _moduloService.GetModulosPorRubroAsync(rubro);
                return Ok(modulos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al obtener módulos de GESCO", detalle = ex.Message });
            }
        }

        [HttpPatch("cambiar-rol")]
        public async Task<IActionResult> CambiarRol([FromBody] CambiarRolRequest request)
        {
            var resultado = await _usuarioService.CambiarRolAsync(request);
            if (resultado) return Ok(new { mensaje = "Rol actualizado correctamente" });

            return BadRequest(new { mensaje = "No se pudo actualizar el rol" });
        }

        [HttpDelete("{usuarioId}")]
        public async Task<IActionResult> Eliminar(int usuarioId)
        {
            var resultado = await _usuarioService.EliminarUsuarioAsync(usuarioId);
            if (resultado) return Ok(new { mensaje = "Usuario eliminado de GESCO" });

            return BadRequest(new { mensaje = "Error al eliminar el usuario" });
        }

        [HttpGet("modulos-por-empresa/{empresaId}")]
        public async Task<IActionResult> GetModulosPorEmpresa(int empresaId)
        {
            var modulos = await _moduloService.GetModulosByEmpresaIdAsync(empresaId);
            return Ok(modulos);
        }

        [HttpPut("actualizar-completo")]
        public async Task<IActionResult> ActualizarCompleto([FromBody] ActualizarUsuarioRequest request)
        {
            try
            {
                var resultado = await _usuarioService.ActualizarUsuarioCompletoAsync(request);
                if (resultado) return Ok(new { mensaje = "Perfil actualizado correctamente" });

                return NotFound(new { mensaje = "Usuario no encontrado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
