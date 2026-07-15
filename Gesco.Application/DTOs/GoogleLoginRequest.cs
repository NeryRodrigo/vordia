using System;

namespace Gesco.Application.DTOs
{
    public class GoogleLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? GoogleId { get; set; }
        public string? FotoUrl { get; set; }
    }
}
