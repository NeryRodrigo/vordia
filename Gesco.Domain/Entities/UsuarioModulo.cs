using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class UsuarioModulo
    {
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }

        public int ModuloId { get; set; }
        public virtual Modulo Modulo { get; set; }
    }
}
