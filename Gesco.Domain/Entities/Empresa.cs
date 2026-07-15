using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Empresa
    {
        public string NombreFantasia;
        public bool Activo;

        [Key]
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string RUC { get; set; } = string.Empty;
        public string? Direccion { get; set; }

        // --- NUEVO PARA GESCO MULTIRUBRO ---
        [Required]
        public string Rubro { get; set; } = "INMOBILIARIA"; // Ejemplo: "INMOBILIARIA", "RETAIL", "CONSTRUCCION"

        // Relaciones
        public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
