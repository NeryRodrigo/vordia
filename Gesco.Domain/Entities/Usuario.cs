using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Aquí guardamos la clave cifrada

        [Required]
        [StringLength(20)]
        public string Rol { get; set; }

        // --- NUEVOS CAMPOS PARA GESCO ---

        [Required]
        public int EmpresaId { get; set; }

        // Propiedad de navegación a Empresa (Para saber el rubro, nombre, etc)
        [ForeignKey("EmpresaId")]
        public virtual Empresa Empresa { get; set; }

        // Flag para el flujo de contraseña temporal
        public bool RequiereCambioPass { get; set; } = true;

        // Estado para gestión (ACTIVO, INACTIVO, BLOQUEADO)
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "ACTIVO";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? UltimaConexion { get; set; }

        /// <summary>
        /// JSON array de ids de permisos granulares (ej: ["fontana_ver","condo_registrar_pago"]).
        /// Null = sin matriz guardada (el front puede tratarlo como acceso total o vacío según rol).
        /// </summary>
        public string? PermisosJson { get; set; }

        // Relación con los módulos específicos que este usuario tiene permitidos
        public virtual ICollection<UsuarioModulo> UsuarioModulos { get; set; }
    }
}
