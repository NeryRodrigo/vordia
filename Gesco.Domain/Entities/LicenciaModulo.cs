using System;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class LicenciaModulo
    {
        [Key]
        public int LicenciaId { get; set; }

        public int EmpresaId { get; set; }
        public virtual Empresa Empresa { get; set; }

        public int ModuloId { get; set; }
        public virtual Modulo Modulo { get; set; }

        public bool Activo { get; set; } = true;
        
        public bool PruebaGratisActiva { get; set; } = false;

        public DateTime? FechaExpiracion { get; set; }
    }
}
