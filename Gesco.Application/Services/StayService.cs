using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    /// <summary>
    /// VORDIA STAY — capa sobre Habitaciones/Reservas existentes + tablas Stay*.
    /// No toca Habitat (UnidadesCondominio / Contratos).
    /// </summary>
    public class StayService : IStayService
    {
        private readonly IApplicationDbContext _db;

        public StayService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<StayTarifa>> GetTarifasAsync(int empresaId, bool soloActivas = true)
        {
            var q = _db.StayTarifas.AsQueryable().Where(t => t.EmpresaId == empresaId);
            if (soloActivas) q = q.Where(t => t.Activo);
            return await q.OrderBy(t => t.TipoHabitacion).ThenBy(t => t.Nombre).ToListAsync();
        }

        public async Task<StayTarifa> UpsertTarifaAsync(StayTarifa tarifa)
        {
            if (tarifa.StayTarifaId > 0)
            {
                var db = await _db.StayTarifas.FirstOrDefaultAsync(t =>
                    t.StayTarifaId == tarifa.StayTarifaId && t.EmpresaId == tarifa.EmpresaId);
                if (db == null) throw new Exception("Tarifa no encontrada");
                db.Nombre = tarifa.Nombre;
                db.TipoHabitacion = tarifa.TipoHabitacion;
                db.HabitacionId = tarifa.HabitacionId;
                db.PrecioPorNocheGs = tarifa.PrecioPorNocheGs;
                db.PrecioPorSemanaGs = tarifa.PrecioPorSemanaGs;
                db.PrecioPorMesGs = tarifa.PrecioPorMesGs;
                db.VigenciaDesde = tarifa.VigenciaDesde;
                db.VigenciaHasta = tarifa.VigenciaHasta;
                db.Activo = tarifa.Activo;
                db.Notas = tarifa.Notas;
                await _db.SaveChangesAsync();
                return db;
            }

            tarifa.StayTarifaId = 0;
            tarifa.FechaCreacion = DateTime.UtcNow;
            _db.StayTarifas.Add(tarifa);
            await _db.SaveChangesAsync();
            return tarifa;
        }

        public async Task<bool> DesactivarTarifaAsync(int tarifaId, int empresaId)
        {
            var db = await _db.StayTarifas.FirstOrDefaultAsync(t => t.StayTarifaId == tarifaId && t.EmpresaId == empresaId);
            if (db == null) return false;
            db.Activo = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<StayPago>> GetPagosByReservaAsync(int empresaId, int reservaId)
        {
            return await _db.StayPagos
                .Where(p => p.EmpresaId == empresaId && p.ReservaId == reservaId && p.Activo)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
        }

        public async Task<StayPago> RegistrarPagoAsync(StayPago pago)
        {
            var reserva = await _db.Reservas.FirstOrDefaultAsync(r =>
                r.ReservaId == pago.ReservaId && r.EmpresaId == pago.EmpresaId);
            if (reserva == null) throw new Exception("Reserva no encontrada");

            pago.StayPagoId = 0;
            pago.Activo = true;
            pago.FechaCreacion = DateTime.UtcNow;
            if (pago.FechaPago == default) pago.FechaPago = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(pago.Concepto)) pago.Concepto = "Pago reserva";
            if (string.IsNullOrWhiteSpace(pago.MetodoPago)) pago.MetodoPago = "Efectivo";

            _db.StayPagos.Add(pago);

            reserva.MontoPagadoGs += pago.MontoGs;
            await _db.SaveChangesAsync();
            return pago;
        }

        public async Task<List<StayBloqueo>> GetBloqueosAsync(int empresaId, DateTime? desde, DateTime? hasta, int? habitacionId)
        {
            var q = _db.StayBloqueos.AsQueryable()
                .Where(b => b.EmpresaId == empresaId && b.Activo);
            if (habitacionId.HasValue) q = q.Where(b => b.HabitacionId == habitacionId.Value);
            if (desde.HasValue) q = q.Where(b => b.FechaHasta > desde.Value);
            if (hasta.HasValue) q = q.Where(b => b.FechaDesde < hasta.Value);
            return await q.OrderBy(b => b.FechaDesde).ToListAsync();
        }

        public async Task<StayBloqueo> CrearBloqueoAsync(StayBloqueo bloqueo)
        {
            if (bloqueo.FechaHasta <= bloqueo.FechaDesde)
                throw new Exception("FechaHasta debe ser posterior a FechaDesde");

            var hab = await _db.Habitaciones.AnyAsync(h =>
                h.HabitacionId == bloqueo.HabitacionId && h.EmpresaId == bloqueo.EmpresaId);
            if (!hab) throw new Exception("Habitación no encontrada en la empresa");

            bloqueo.StayBloqueoId = 0;
            bloqueo.Activo = true;
            bloqueo.FechaCreacion = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(bloqueo.Motivo)) bloqueo.Motivo = "BloqueoManual";

            _db.StayBloqueos.Add(bloqueo);
            await _db.SaveChangesAsync();
            return bloqueo;
        }

        public async Task<bool> DesactivarBloqueoAsync(int bloqueoId, int empresaId)
        {
            var db = await _db.StayBloqueos.FirstOrDefaultAsync(b =>
                b.StayBloqueoId == bloqueoId && b.EmpresaId == empresaId);
            if (db == null) return false;
            db.Activo = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<StayHuesped>> GetHuespedesAsync(int empresaId)
        {
            return await _db.StayHuespedes
                .Where(h => h.EmpresaId == empresaId && h.Activo)
                .OrderByDescending(h => h.FechaActualizacion)
                .ToListAsync();
        }

        public async Task<StayHuesped> UpsertHuespedAsync(StayHuesped huesped)
        {
            var personaOk = await _db.Personas.AnyAsync(p =>
                p.PersonaId == huesped.PersonaId && p.EmpresaId == huesped.EmpresaId);
            if (!personaOk) throw new Exception("Persona/huésped no pertenece a la empresa");

            var existing = await _db.StayHuespedes.FirstOrDefaultAsync(h =>
                h.EmpresaId == huesped.EmpresaId && h.PersonaId == huesped.PersonaId);

            if (existing != null)
            {
                existing.Nacionalidad = huesped.Nacionalidad;
                existing.FechaNacimiento = huesped.FechaNacimiento;
                existing.TipoDocumento = huesped.TipoDocumento;
                existing.PaisResidencia = huesped.PaisResidencia;
                existing.TelefonoEmergencia = huesped.TelefonoEmergencia;
                existing.Preferencias = huesped.Preferencias;
                existing.Notas = huesped.Notas;
                existing.Activo = huesped.Activo;
                existing.FechaActualizacion = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return existing;
            }

            huesped.StayHuespedId = 0;
            huesped.FechaCreacion = DateTime.UtcNow;
            huesped.FechaActualizacion = DateTime.UtcNow;
            huesped.Activo = true;
            _db.StayHuespedes.Add(huesped);
            await _db.SaveChangesAsync();
            return huesped;
        }

        public async Task<object> GetDisponibilidadAsync(int empresaId, DateTime desde, DateTime hasta)
        {
            if (hasta <= desde) throw new Exception("Rango de fechas inválido");

            var habitaciones = await _db.Habitaciones
                .Where(h => h.EmpresaId == empresaId)
                .OrderBy(h => h.Nombre)
                .ToListAsync();

            var reservas = await _db.Reservas
                .Where(r => r.EmpresaId == empresaId
                    && r.Estado != "Cancelada"
                    && r.FechaCheckOut > desde
                    && r.FechaCheckIn < hasta)
                .ToListAsync();

            var bloqueos = await _db.StayBloqueos
                .Where(b => b.EmpresaId == empresaId
                    && b.Activo
                    && b.FechaHasta > desde
                    && b.FechaDesde < hasta)
                .ToListAsync();

            var items = habitaciones.Select(h =>
            {
                var occ = reservas
                    .Where(r => r.HabitacionId == h.HabitacionId)
                    .Select(r => new
                    {
                        tipo = "reserva",
                        r.ReservaId,
                        r.Estado,
                        desde = r.FechaCheckIn,
                        hasta = r.FechaCheckOut,
                        r.HuespedId,
                        r.MontoTotalGs,
                        r.MontoPagadoGs
                    })
                    .Cast<object>()
                    .ToList();

                var blq = bloqueos
                    .Where(b => b.HabitacionId == h.HabitacionId)
                    .Select(b => new
                    {
                        tipo = "bloqueo",
                        b.StayBloqueoId,
                        b.Motivo,
                        desde = b.FechaDesde,
                        hasta = b.FechaHasta,
                        b.Notas
                    })
                    .Cast<object>();

                occ.AddRange(blq);

                var libre = !reservas.Any(r => r.HabitacionId == h.HabitacionId)
                    && !bloqueos.Any(b => b.HabitacionId == h.HabitacionId);

                return new
                {
                    h.HabitacionId,
                    h.Nombre,
                    h.Tipo,
                    h.Estado,
                    h.CapacidadCamas,
                    h.PrecioBasePorNocheGs,
                    disponibleEnRango = libre,
                    ocupaciones = occ
                };
            }).ToList();

            return new
            {
                empresaId,
                desde,
                hasta,
                totalHabitaciones = habitaciones.Count,
                disponibles = items.Count(i => i.disponibleEnRango),
                habitaciones = items
            };
        }

        public async Task<Reserva?> CheckInAsync(int empresaId, int reservaId)
        {
            var r = await _db.Reservas.FirstOrDefaultAsync(x =>
                x.ReservaId == reservaId && x.EmpresaId == empresaId);
            if (r == null) return null;
            if (r.Estado == "Cancelada" || r.Estado == "CheckOut")
                throw new Exception($"No se puede hacer check-in desde estado {r.Estado}");

            r.Estado = "CheckIn";
            var hab = await _db.Habitaciones.FirstOrDefaultAsync(h => h.HabitacionId == r.HabitacionId);
            if (hab != null) hab.Estado = "Ocupada";
            await _db.SaveChangesAsync();
            return r;
        }

        public async Task<Reserva?> CheckOutAsync(int empresaId, int reservaId)
        {
            var r = await _db.Reservas.FirstOrDefaultAsync(x =>
                x.ReservaId == reservaId && x.EmpresaId == empresaId);
            if (r == null) return null;
            if (r.Estado != "CheckIn" && r.Estado != "Confirmada")
                throw new Exception($"No se puede hacer check-out desde estado {r.Estado}");

            r.Estado = "CheckOut";
            var hab = await _db.Habitaciones.FirstOrDefaultAsync(h => h.HabitacionId == r.HabitacionId);
            if (hab != null) hab.Estado = "Limpieza";
            await _db.SaveChangesAsync();
            return r;
        }
    }
}
