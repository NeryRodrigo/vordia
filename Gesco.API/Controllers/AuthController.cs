using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gesco.Application.Interfaces;
using Gesco.Application.DTOs;
using Gesco.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtTokenService _jwt;

        public AuthController(IAuthService authService, IJwtTokenService jwt)
        {
            _authService = authService;
            _jwt = jwt;
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var resultado = await _authService.RegisterAsync(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                if (result == null)
                    return Unauthorized(new { mensaje = "Credenciales inválidas" });

                result.Token = _jwt.CreateToken(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(request);
                result.Token = _jwt.CreateToken(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPatch("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!int.TryParse(claimId, out var authUserId) || authUserId <= 0)
                return Unauthorized(new { mensaje = "Sesión inválida" });

            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var esAdmin = rol.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
                || rol.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase);

            // Propio usuario o administrador
            if (request.UsuarioId != authUserId && !esAdmin)
                return Forbid();

            var resultado = await _authService.UpdatePasswordAsync(request);

            if (resultado)
            {
                return Ok(new { mensaje = "Contraseña actualizada correctamente. Bienvenido a GESCO." });
            }

            return BadRequest(new { mensaje = "No se pudo actualizar la contraseña." });
        }

        [Authorize]
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
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpGet("empresas")]
        public async Task<IActionResult> GetEmpresas()
        {
            var empresas = await _authService.GetEmpresasActivasAsync();
            return Ok(empresas);
        }
    }
}
