using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class RegisterRequest
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Aquí el admin pone la temporal
        public string Rol { get; set; }
        public int EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; } // Nombre de la empresa si es registro self-service
        public List<int> ModulosIds { get; set; } // <--- Agregamos esto
    }
}
