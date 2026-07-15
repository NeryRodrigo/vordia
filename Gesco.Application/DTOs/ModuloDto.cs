using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class ModuloDto
    {
        public int ModuloId { get; set; }
        public string NombreModulo { get; set; }
        public string Icono { get; set; } // ej: "LayoutDashboard", "Users"
        public string RutaFrontend { get; set; } // ej: "/dashboard/usuarios"
        public bool IsContratado { get; set; } = false;
    }
}
