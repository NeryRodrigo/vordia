using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class CambiarRolRequest
    {
        public int UsuarioId { get; set; }
        public string Rol { get; set; }
    }
}
