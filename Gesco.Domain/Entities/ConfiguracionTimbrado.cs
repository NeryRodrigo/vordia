using System;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class ConfiguracionTimbrado
    {
        [Key]
        public int TimbradoId { get; set; }
        public int EmpresaId { get; set; }
        public virtual Empresa Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Ruc { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroTimbrado { get; set; }

        public DateTime ValidezDesde { get; set; }
        public DateTime ValidezHasta { get; set; }

        public int NumeroDesde { get; set; }
        public int NumeroHasta { get; set; }
        
        public int UltimoNumeroUsado { get; set; }

        public DateTime FechaCarga { get; set; } = DateTime.Now;
        public bool EstaActivo { get; set; } = true;
    }
}
