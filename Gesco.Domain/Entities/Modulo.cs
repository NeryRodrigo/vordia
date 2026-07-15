using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Modulo
    {
        [Key]
        public int ModuloId { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreModulo { get; set; } // "ACM", "Captaciones", "Caja"

        [Required]
        [StringLength(50)]
        public string RubroAsociado { get; set; } // Para filtrar: Solo mostrar "ACM" si la empresa es "INMOBILIARIA"

        [StringLength(50)]
        public string Icono { get; set; } // Clase de Lucide o FontAwesome: "map-pin", "trending-up"

        [Required]
        [StringLength(100)]
        public string RutaFrontend { get; set; } // La URL interna: "/dashboard/acm"

        // Relación con usuarios
        public virtual ICollection<UsuarioModulo> UsuarioModulos { get; set; } = new List<UsuarioModulo>();
    }
}
