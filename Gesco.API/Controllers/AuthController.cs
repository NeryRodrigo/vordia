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
        private readonly IEmpresaScope _scope;

        public AuthController(IAuthService authService, IJwtTokenService jwt, IEmpresaScope scope)
        {
            _scope = scope;
            _authService = authService;
            _jwt = jwt;
        }

        /// <summary>
        /// Alta de usuarios: solo administradores. Un administrador de cliente solo puede crear usuarios
        /// de su propia empresa y nunca SUPERADMIN; el equipo de VORDIA (empresa operadora) puede crear en cualquiera.
        /// </summary>
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (!rol.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && !rol.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            if (!int.TryParse(User.FindFirstValue("empresaId"), out var empresaActor)) return Forbid();
            if (!await EsOperadorVordiaAsync(empresaActor))
            {
                if (request.EmpresaId != empresaActor) return Forbid();
                if (string.Equals(request.Rol, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)) return Forbid();
            }
            if (request.EmpresaId <= 0) return BadRequest(new { mensaje = "Indicá la empresa del usuario" });
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

        /// <summary>
        /// Deshabilitado: aceptaba cualquier correo sin verificar el token de Google, lo que permitía entrar
        /// a cuentas ajenas. Se vuelve a habilitar solo con verificación real del ID token de Google.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("google-login")]
        public IActionResult GoogleLogin([FromBody] GoogleLoginRequest request) =>
            StatusCode(StatusCodes.Status410Gone, new { mensaje = "El ingreso con Google no está disponible. Ingresá con tu correo y contraseña." });

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

        private Task<bool> EsOperadorVordiaAsync(int empresaId) => _scope.EsOperadorVordiaAsync(empresaId);

        // Lista pública para la pantalla de login (entorno de trabajo)
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
