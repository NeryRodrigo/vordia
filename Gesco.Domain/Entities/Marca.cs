using System;

namespace Gesco.Domain.Entities
{
    public class Marca
    {
        public int MarcaId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        
        public Empresa Empresa { get; set; } = null!;
    }
}
