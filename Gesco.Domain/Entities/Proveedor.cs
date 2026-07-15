using System;
using System.Collections.Generic;

namespace Gesco.Domain.Entities
{
    public class Proveedor
    {
        public int ProveedorId { get; set; }
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; } = true;

        public Empresa Empresa { get; set; } = null!;
    }
}
