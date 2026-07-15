using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Sucursal
    {
        public bool Activo;

        public int SucursalId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string PuntoExpedicion { get; set; } = "001"; // Ej: 001-001...

        public Empresa Empresa { get; set; } = null!;
    }
}
