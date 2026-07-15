using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Gesco.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Ahora inyectamos el SERVICIO, no el Contexto
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Llamamos al servicio actualizado
                var resultado = await _authService.RegisterAsync(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                // Si el correo ya existe o falla la transacción, el servicio lanza la excepción
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                if (result == null)
                    return Unauthorized(new { mensaje = "Credenciales inválidas" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Aquí capturamos el mensaje de "Cuenta bloqueada" y lo enviamos
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPatch("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var resultado = await _authService.UpdatePasswordAsync(request);

            if (resultado)
            {
                return Ok(new { mensaje = "Contraseña actualizada correctamente. Bienvenido a GESCO." });
            }

            return BadRequest(new { mensaje = "No se pudo actualizar la contraseña." });
        }

        [HttpGet("modulos/{usuarioId}")]
        public async Task<IActionResult> GetModulos(int usuarioId)
        {
            try
            {
                var modulos = await _authService.GetModulosPorUsuarioAsync(usuarioId);
                if (modulos == null)
                    return Ok(new List<ModuloDto>());
                return Ok(modulos);
            }
            catch (Exception ex)
            {
                // Devolvemos un 500 pero con el string exacto para ver qué ocurre.
                return StatusCode(500, new { mensaje = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("empresas")]
        public async Task<IActionResult> GetEmpresas()
        {
            var empresas = await _authService.GetEmpresasActivasAsync();
            return Ok(empresas);
        }
    }
}
