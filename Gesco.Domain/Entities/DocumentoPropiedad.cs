using System;

namespace Gesco.Domain.Entities
{
    public class DocumentoPropiedad
    {
        public int DocumentoPropiedadId { get; set; }
        public int PropiedadId { get; set; }
        public string Categoria { get; set; } = string.Empty; // "Venta", "Captar", "Alquiler", "InquilinoFisica", "InquilinoJuridica"
        public string TipoDocumento { get; set; } = string.Empty; // "Carta de intención de compra", "Cédula", etc.
        public string Url { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; }

        public Propiedad? Propiedad { get; set; }
    }
}
