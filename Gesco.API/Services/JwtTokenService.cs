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
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(AuthUserResponse user)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado");
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
