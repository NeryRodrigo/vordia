using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("FichasIncompletas")]
    public class FichaIncompleta
    {
        [Key]
        public int FichaIncompletaId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int UnidadId { get; set; }

        [StringLength(50)]
        public string? CodigoUnidad { get; set; }

        [StringLength(150)]
        public string? NombreCondominio { get; set; }

        [StringLength(200)]
        public string? NombreInquilino { get; set; }

        /// <summary>edit | contratar | renovar</summary>
        [Required]
        [StringLength(20)]
        public string Modo { get; set; } = "edit";

        [Required]
        public string PayloadJson { get; set; } = "{}";

        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
