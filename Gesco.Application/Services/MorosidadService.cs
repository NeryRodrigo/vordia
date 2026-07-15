using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class MorosidadService : IMorosidadService
    {
        private readonly IApplicationDbContext _context;

        public MorosidadService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MorosidadInquilino> RegistrarMorosidadAsync(int inquilinoId, int unidadId, decimal montoTotal, int cantidadCuotas, string observaciones, decimal montoInteresDiario)
        {
            var morosidad = new MorosidadInquilino
            {
                InquilinoId = inquilinoId,
                UnidadId = unidadId,
                MontoTotal = montoTotal,
                SaldoPendiente = montoTotal,
                CantidadCuotas = cantidadCuotas,
                FechaRegistro = DateTime.Now,
                ObservacionesGenerales = observaciones,
                Estado = "Pendiente",
                MontoInteresDiario = montoInteresDiario,
                InteresAcumuladoTotal = 0,
                UltimaFechaCalculoInteres = DateTime.Now,
                Cuotas = new List<CuotaMorosidad>()
            };

            decimal montoCuota = Math.Round(montoTotal / cantidadCuotas, 2);
            decimal resto = montoTotal - (montoCuota * cantidadCuotas);

            for (int i = 1; i <= cantidadCuotas; i++)
            {
                var cuota = new CuotaMorosidad
                {
                    NumeroCuota = i,
                    Monto = i == cantidadCuotas ? montoCuota + resto : montoCuota,
                    FechaVencimiento = DateTime.Now.AddMonths(i),
                    EstaPagado = false
                };
                morosidad.Cuotas.Add(cuota);
            }

            _context.MorosidadesInquilino.Add(morosidad);
            await _context.SaveChangesAsync();

            return morosidad;
        }

        public async Task<IEnumerable<MorosidadInquilino>> ObtenerMorosidadesAsync(int? unidadId = null)
        {
            var query = _context.MorosidadesInquilino
                .Include(m => m.Inquilino)
                .Include(m => m.Unidad)
                .Include(m => m.Cuotas)
                .AsQueryable();

            if (unidadId.HasValue)
            {
                query = query.Where(m => m.UnidadId == unidadId.Value);
            }

            var morosidades = await query.OrderByDescending(m => m.FechaRegistro).ToListAsync();

            // Consultar contratos activos para marcar si están desalojados o no
            var activeInquilinoIds = await _context.ContratosInquilinos
                .Where(c => c.EstaActivo)
                .Select(c => c.InquilinoId)
                .Distinct()
                .ToListAsync();

            // Aplicar cálculo de interés y determinar si está desalojado
            foreach(var m in morosidades)
            {
                m.EstaDesalojado = !activeInquilinoIds.Contains(m.InquilinoId);
                await AplicarInteresDiario(m);
            }
            if (morosidades.Any()) await _context.SaveChangesAsync();

            return morosidades;
        }

        public async Task<MorosidadInquilino?> ObtenerDetalleMorosidadAsync(int morosidadId)
        {
            var m = await _context.MorosidadesInquilino
                .Include(mo => mo.Inquilino)
                .Include(mo => mo.Unidad)
                .Include(mo => mo.Cuotas)
                    .ThenInclude(c => c.Seguimientos)
                .FirstOrDefaultAsync(mo => mo.MorosidadId == morosidadId);

            if (m != null)
            {
                var isActivo = await _context.ContratosInquilinos
                    .AnyAsync(c => c.InquilinoId == m.InquilinoId && c.EstaActivo);
                m.EstaDesalojado = !isActivo;

                await AplicarInteresDiario(m);
                await _context.SaveChangesAsync();
            }

            return m;
        }

        public async Task<CuotaMorosidad> CobrarCuotaAsync(int cuotaId, string metodoPago, int contratoActualId, string? notas)
        {
            var cuota = await _context.CuotasMorosidad
                .Include(c => c.Morosidad)
                .FirstOrDefaultAsync(c => c.CuotaId == cuotaId);

            if (cuota == null) throw new Exception("Cuota no encontrada");
            if (cuota.EstaPagado) throw new Exception("La cuota ya está pagada");

            cuota.EstaPagado = true;
            cuota.FechaPago = DateTime.Now;

            int finalContratoId = contratoActualId;
            if (finalContratoId <= 1)
            {
                // Buscar el último contrato del inquilino de la morosidad
                var ultimoContrato = await _context.ContratosInquilinos
                    .Where(c => c.InquilinoId == cuota.Morosidad.InquilinoId)
                    .OrderByDescending(c => c.ContratoId)
                    .FirstOrDefaultAsync();

                if (ultimoContrato != null)
                {
                    finalContratoId = ultimoContrato.ContratoId;
                }
            }

            // Impactar Facturación (Generar PagoRecibo)
            var pagoRecibo = new PagoRecibo
            {
                ContratoId = finalContratoId,
                CuotaMorosidadId = cuota.CuotaId,
                TipoPago = "Recupero Mora", // Concepto que se verá en Facturación
                MesPagado = DateTime.Now.Month,
                AnioPagado = DateTime.Now.Year,
                FechaPago = DateTime.Now,
                MontoPagado = cuota.Monto,
                MetodoPago = metodoPago,
                SaldoPendiente = 0,
                Mora = 0,
                FacturaEmitida = false,
                Notas = notas ?? $"Cobro de Cuota {cuota.NumeroCuota} de Morosidad"
            };

            _context.PagosRecibos.Add(pagoRecibo);

            // Actualizar Saldo Morosidad
            cuota.Morosidad.SaldoPendiente -= cuota.Monto;
            if (cuota.Morosidad.SaldoPendiente <= 0)
            {
                cuota.Morosidad.Estado = "Pagado";
            }

            await _context.SaveChangesAsync();

            return cuota;
        }

        public async Task<SeguimientoMorosidad> AgregarSeguimientoAsync(int cuotaId, string observacion)
        {
            var seguimiento = new SeguimientoMorosidad
            {
                CuotaId = cuotaId,
                FechaRegistro = DateTime.Now,
                Observacion = observacion
            };

            _context.SeguimientosMorosidad.Add(seguimiento);
            await _context.SaveChangesAsync();

            return seguimiento;
        }

        private async Task AplicarInteresDiario(MorosidadInquilino m)
        {
            if (m.Estado == "Pagado" || m.MontoInteresDiario <= 0 || !m.UltimaFechaCalculoInteres.HasValue) return;

            var ahora = DateTime.Now.Date;
            var ultimaFecha = m.UltimaFechaCalculoInteres.Value.Date;

            if (ahora > ultimaFecha)
            {
                int diasTranscurridos = (ahora - ultimaFecha).Days;
                
                decimal interesGenerado = diasTranscurridos * m.MontoInteresDiario;
                
                m.InteresAcumuladoTotal += interesGenerado;
                m.SaldoPendiente += interesGenerado;
                m.MontoTotal += interesGenerado; // El monto total histórico también sube
                
                // Distribuimos el nuevo interés en la última cuota pendiente, 
                // o creamos un registro de ajuste (por simplicidad, lo sumamos a la cuota no pagada más antigua)
                var cuotaPendiente = m.Cuotas.Where(c => !c.EstaPagado).OrderBy(c => c.FechaVencimiento).FirstOrDefault();
                if (cuotaPendiente != null)
                {
                    cuotaPendiente.Monto += interesGenerado;
                }

                m.UltimaFechaCalculoInteres = ahora;
            }
        }
    }
}
