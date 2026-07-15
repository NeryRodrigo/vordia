using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class DocumentoInquilino
    {
        [Key]
        public int DocumentoId { get; set; }
        public int ContratoId { get; set; }
        public virtual ContratoInquilino Contrato { get; set; }

        [Required]
        public string TipoDocumento { get; set; } // "Cedula", "Contrato", "Garantia", etc.
        public string? Url { get; set; }
        public bool EsEntregado { get; set; }
    }
}
