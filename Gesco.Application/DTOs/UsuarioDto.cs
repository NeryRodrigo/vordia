using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
        public bool RequiereCambioPass { get; set; }

        // Propiedad calculada para el círculo visual (Avatar)
        // Ejemplo: "Juan Pérez" -> "JP"
        public string Iniciales { get; set; }
        public List<int> ModulosIds { get; set; } = new List<int>();
    }
}
