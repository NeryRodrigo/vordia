using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// VORDIA STAY — perfil extendido del huésped (Persona del Core = datos fiscales/contacto).
    /// </summary>
    [Table("StayHuespedes")]
    public class StayHuesped
    {
        [Key]
        public int StayHuespedId { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        /// <summary>FK lógica a Personas (huésped principal de Reserva.HuespedId).</summary>
        [Required]
        public int PersonaId { get; set; }

        [StringLength(80)]
        public string? Nacionalidad { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(40)]
        public string? TipoDocumento { get; set; } // CI | Pasaporte | RUC

        [StringLength(120)]
        public string? PaisResidencia { get; set; }

        [StringLength(40)]
        public string? TelefonoEmergencia { get; set; }

        [StringLength(400)]
        public string? Preferencias { get; set; }

        [StringLength(400)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
