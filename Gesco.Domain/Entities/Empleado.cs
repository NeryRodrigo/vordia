using System;

namespace Gesco.Domain.Entities
{
    public class Empleado
    {
        public int EmpleadoId { get; set; }
        public int EmpresaId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty; // CI
        public string Cargo { get; set; } = string.Empty;
        public decimal SalarioBase { get; set; }
        public DateTime FechaIngreso { get; set; }
        public bool Activo { get; set; } = true;
    }
}
