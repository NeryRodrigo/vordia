using System;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Domain.Entities
{
    public class Actividad
    {
        [Key]
        public int ActividadId { get; set; }
        
        public int EmpresaId { get; set; }
        
        [Required]
        public DateTime Fecha { get; set; }
        
        [Required]
        [StringLength(150)]
        public string Titulo { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;
        
        public int? ResponsableId { get; set; } // Opcional, EmpleadoId
        
        [StringLength(150)]
        public string ResponsableNombre { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Disponible"; // "en Proceso", "Disponible", "Cerrado"
    }
}
