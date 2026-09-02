using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmProveedores")]
    public class CrmProveedor
    {
        [Key]
        public int ProveedorId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(200)]
        public string Nombre { get; set; } = "";
        [Required, StringLength(80)]
        public string Categoria { get; set; } = "General";
        [StringLength(150)]
        public string? ContactoNombre { get; set; }
        [StringLength(150)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string? Telefono { get; set; }
        [StringLength(30)]
        public string? Ruc { get; set; }
        [StringLength(30)]
        public string? ProductoCodigo { get; set; }
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
