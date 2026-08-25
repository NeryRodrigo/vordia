using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmHistorial")]
    public class CrmHistorial
    {
        [Key]
        public int HistorialId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        /// <summary>nota | llamada | reunion | email | cambio_estado</summary>
        [Required]
        [StringLength(40)]
        public string Tipo { get; set; } = "nota";

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Detalle { get; set; }

        [StringLength(40)]
        public string? EstadoAnterior { get; set; }

        [StringLength(40)]
        public string? EstadoNuevo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual CrmCliente? Cliente { get; set; }
    }
}
