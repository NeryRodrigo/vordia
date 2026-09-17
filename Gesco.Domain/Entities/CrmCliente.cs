using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("CrmClientes")]
    public class CrmCliente
    {
        [Key]
        public int ClienteId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(200)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(200)]
        public string? NombreComercial { get; set; }

        [StringLength(100)]
        public string? Rubro { get; set; }

        [StringLength(50)]
        public string? Documento { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(150)]
        public string? ContactoNombre { get; set; }

        [StringLength(100)]
        public string? ContactoCargo { get; set; }

        [StringLength(150)]
        public string? ContactoEmail { get; set; }

        [StringLength(50)]
        public string? ContactoTelefono { get; set; }

        /// <summary>Prospecto | Contactado | En negociacion | Propuesta enviada | Cerrado | Cliente activo</summary>
        [Required]
        [StringLength(40)]
        public string EstadoPipeline { get; set; } = "Prospecto";

        [StringLength(200)]
        public string? SistemaServicio { get; set; }

        [StringLength(500)]
        public string? ProximosPasos { get; set; }

        public DateTime? FechaEstimadaCierre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FeeEstimadoMensual { get; set; }

        public string? Notas { get; set; }

        /// <summary>web | manual | whatsapp | referido</summary>
        [StringLength(40)]
        public string? Origen { get; set; }

        /// <summary>Solución o sector que eligió el visitante en el formulario</summary>
        [StringLength(120)]
        public string? OrigenDetalle { get; set; }

        public string? MensajeInicial { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        public int? UsuarioResponsableId { get; set; }

        public virtual ICollection<CrmHistorial> Historial { get; set; } = new List<CrmHistorial>();
    }
}
