using System;

namespace Gesco.Domain.Entities
{
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        
        public Empresa Empresa { get; set; } = null!;
    }
}
