using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IStayService
    {
        // Tarifas
        Task<List<StayTarifa>> GetTarifasAsync(int empresaId, bool soloActivas = true);
        Task<StayTarifa> UpsertTarifaAsync(StayTarifa tarifa);
        Task<bool> DesactivarTarifaAsync(int tarifaId, int empresaId);

        // Pagos
        Task<List<StayPago>> GetPagosByReservaAsync(int empresaId, int reservaId);
        Task<StayPago> RegistrarPagoAsync(StayPago pago);

        // Bloqueos / calendario
        Task<List<StayBloqueo>> GetBloqueosAsync(int empresaId, DateTime? desde, DateTime? hasta, int? habitacionId);
        Task<StayBloqueo> CrearBloqueoAsync(StayBloqueo bloqueo);
        Task<bool> DesactivarBloqueoAsync(int bloqueoId, int empresaId);

        // Huéspedes
        Task<List<StayHuesped>> GetHuespedesAsync(int empresaId);
        Task<StayHuesped> UpsertHuespedAsync(StayHuesped huesped);

        // Disponibilidad (Reserva existente + bloqueos)
        Task<object> GetDisponibilidadAsync(int empresaId, DateTime desde, DateTime hasta);

        // Check-in / Check-out sobre Reserva (sin cambiar schema)
        Task<Reserva?> CheckInAsync(int empresaId, int reservaId);
        Task<Reserva?> CheckOutAsync(int empresaId, int reservaId);
    }
}
