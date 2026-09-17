using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    public class Reserva
    {
        [Key]
        public int ReservaId { get; set; }

        public int EmpresaId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Empresa? Empresa { get; set; }

        public int HabitacionId { get; set; }
        public virtual Habitacion? Habitacion { get; set; }

        // El huésped principal (Persona con EsCliente = true)
        public int HuespedId { get; set; }
        public virtual Persona? Huesped { get; set; }

        // El recepcionista que realizó la reserva
        public int? UsuarioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Usuario? Usuario { get; set; }

        public DateTime FechaCheckIn { get; set; }
        public DateTime FechaCheckOut { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente"; // "Pendiente", "Confirmada", "CheckIn", "CheckOut", "Cancelada"

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotalGs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagadoGs { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Para cualquier nota especial, canales de origen (Booking.com, Airbnb, Directo), etc.
        public string? OrigenReserva { get; set; }
        public string? NotasAdicionales { get; set; }
    }
}
