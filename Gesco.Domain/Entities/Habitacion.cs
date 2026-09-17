using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class Habitacion
    {
        [Key]
        public int HabitacionId { get; set; }

        public int EmpresaId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = ""; // Ejemplo: "201", "Dormitorio Mixto"

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = ""; // Ejemplo: "Privada", "Compartida"

        public int CapacidadCamas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioBasePorNocheGs { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Disponible"; // "Disponible", "Ocupada", "Limpieza", "Mantenimiento"

        // Para flexibilidad en atributos (EAV dinámico)
        public string? DetallesJson { get; set; } // {"tieneAire": true, "tieneBañoPrivado": false}
    }
}
