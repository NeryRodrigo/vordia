using System;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class PlantillaDocumento
    {
        [Key]
        public int PlantillaId { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required]
        public string Tipo { get; set; } = "PRINCIPAL"; // PRINCIPAL, GARANTÍA, ANEXO

        public string? VariablesSoportadas { get; set; }

        public string? ArchivoUrl { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
