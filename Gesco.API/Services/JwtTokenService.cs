using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gesco.Application.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Gesco.API.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(AuthUserResponse user);
    }

    public class JwtTokenService : IJwtTokenService
    {
        public const string SessionEpochClaim = "vordia_sid";

        private readonly IConfiguration _config;
        private readonly ISessionEpochService _sessionEpoch;

        public JwtTokenService(IConfiguration config, ISessionEpochService sessionEpoch)
        {
            _config = config;
            _sessionEpoch = sessionEpoch;
        }

        /// <summary>
        /// Clave de firma de los tokens: JWT_KEY (variable de entorno del servidor, recomendada en producción)
        /// o Jwt:Key de appsettings. Una variable vacía se ignora.
        /// </summary>
        public static string ClaveFirma(IConfiguration config)
        {
            var env = config["JWT_KEY"];
            if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            var key = config["Jwt:Key"];
            return string.IsNullOrWhiteSpace(key) ? "Vordia_Dev_Only_Change_In_Production_Min_32_Chars!" : key;
        }

        public string CreateToken(AuthUserResponse user)
        {
            var key = ClaveFirma(_config);
            var issuer = _config["Jwt:Issuer"] ?? "Vordia";
            var audience = _config["Jwt:Audience"] ?? "VordiaWeb";
            var hours = int.TryParse(_config["Jwt:ExpireHours"], out var h) ? h : 12;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Nombre ?? ""),
                new(ClaimTypes.Role, user.Rol ?? "AGENTE"),
                new("empresaId", user.EmpresaId.ToString()),
                new(SessionEpochClaim, _sessionEpoch.Epoch),
            };
            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(hours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
