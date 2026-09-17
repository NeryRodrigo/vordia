using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Archivo o enlace del repositorio de Vordia HQ: contratos firmados, actas,
    /// propuestas, facturas. El binario vive en wwwroot/crm-documentos y acá queda
    /// la ruta relativa; si es un enlace externo (Drive, Dropbox) se guarda la URL.
    /// </summary>
    [Table("CrmDocumentos")]
    public class CrmDocumento
    {
        [Key]
        public int DocumentoId { get; set; }
        public int EmpresaId { get; set; }
        /// <summary>Cliente del CRM al que pertenece. Null si es un documento propio de Vordia.</summary>
        public int? ClienteId { get; set; }
        public int? ProyectoId { get; set; }

        [Required, StringLength(200)]
        public string Titulo { get; set; } = "";

        /// <summary>Contrato | Propuesta | Acta | Legal | Factura | Tecnico | Otro</summary>
        [Required, StringLength(40)]
        public string Categoria { get; set; } = "Otro";

        [StringLength(260)]
        public string? NombreArchivo { get; set; }

        /// <summary>Ruta relativa /crm-documentos/... o URL completa si es enlace externo.</summary>
        [Required, StringLength(500)]
        public string Url { get; set; } = "";

        public bool EsEnlaceExterno { get; set; }

        [StringLength(12)]
        public string? Extension { get; set; }

        public long TamanoBytes { get; set; }

        /// <summary>Fecha en que el documento caduca. Null para los que no vencen.</summary>
        public DateTime? FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        [StringLength(150)]
        public string? SubidoPor { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual CrmCliente? Cliente { get; set; }
    }
}
