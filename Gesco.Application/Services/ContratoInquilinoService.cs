using Microsoft.EntityFrameworkCore; 
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class ContratoInquilinoService : IContratoInquilinoService
    {
        private readonly IApplicationDbContext _context;

        public ContratoInquilinoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ContratoInquilino> CreateContratoAsync(ContratoInquilino contrato, Persona inquilino)
        {
            // 1. Buscar o crear la persona (inquilino)
            var personaExistente = await _context.Personas
                .FirstOrDefaultAsync(p => p.Documento == inquilino.Documento);

            if (personaExistente == null)
            {
                _context.Personas.Add(inquilino);
                await _context.SaveChangesAsync();
                contrato.InquilinoId = inquilino.PersonaId;
            }
            else
            {
                contrato.InquilinoId = personaExistente.PersonaId;
            }

            // 2. Desactivar contratos anteriores para esta unidad
            var contratosAnteriores = await _context.ContratosInquilinos
                .Where(c => c.UnidadId == contrato.UnidadId && c.EstaActivo)
                .ToListAsync();

            foreach (var c in contratosAnteriores)
            {
                c.EstaActivo = false;
                if (c.EstadoHistorico != "Definitivo")
                    c.EstadoHistorico = "Temporal";
            }

            // 3. Crear el nuevo contrato
            contrato.EstaActivo = true;
            contrato.EstadoHistorico = "Ocupado";
            _context.ContratosInquilinos.Add(contrato);

            // 4. Actualizar el estado de la unidad
            var unidad = await _context.UnidadesCondominio.FindAsync(contrato.UnidadId);
            if (unidad != null)
            {
                unidad.Estado = "Ocupado";
            }

            await _context.SaveChangesAsync();
            return contrato;
        }

        public async Task<PagoRecibo> RegisterPagoAsync(PagoRecibo pago)
        {
            if (pago.FechaPago == default)
            {
                pago.FechaPago = DateTime.Now;
            }
            _context.PagosRecibos.Add(pago);
            await _context.SaveChangesAsync();
            return pago;
        }

        public async Task<IEnumerable<PagoRecibo>> GetAllPagosAsync()
        {
            return await _context.PagosRecibos
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Inquilino)
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Unidad)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();
        }

        public async Task<bool> DeletePagoAsync(int pagoId)
        {
            var pago = await _context.PagosRecibos.FindAsync(pagoId);
            if (pago == null) return false;

            _context.PagosRecibos.Remove(pago);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePagoAsync(PagoRecibo pago)
        {
            var db = await _context.PagosRecibos.FindAsync(pago.PagoId);
            if (db == null) return false;

            db.ContratoId = pago.ContratoId;
            db.TipoPago = pago.TipoPago ?? db.TipoPago;
            db.MesPagado = pago.MesPagado;
            db.AnioPagado = pago.AnioPagado;
            db.FechaPago = pago.FechaPago == default ? db.FechaPago : pago.FechaPago;
            db.MontoPagado = pago.MontoPagado;
            db.MetodoPago = pago.MetodoPago ?? db.MetodoPago;
            db.SaldoPendiente = pago.SaldoPendiente;
            db.Mora = pago.Mora;
            db.FacturaEmitida = pago.FacturaEmitida;
            db.NumeroFactura = pago.NumeroFactura;
            db.ComprobanteUrl = pago.ComprobanteUrl;
            db.NroComprobante = pago.NroComprobante;
            db.Banco = pago.Banco;
            db.Notas = pago.Notas;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePagoComprobanteAsync(int pagoId, bool facturaEmitida, string? numeroFactura, string? comprobanteUrl)
        {
            var db = await _context.PagosRecibos.FindAsync(pagoId);
            if (db == null) return false;
            db.FacturaEmitida = facturaEmitida;
            db.NumeroFactura = numeroFactura;
            db.ComprobanteUrl = comprobanteUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ContratoInquilino?> GetContratoByUnidadAsync(int unidadId)
        {
            return await _context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .OrderByDescending(c => c.ContratoId)
                .FirstOrDefaultAsync(c => c.UnidadId == unidadId && c.EstaActivo);
        }

        public async Task<IEnumerable<ContratoInquilino>> GetContratosHistoricosByUnidadAsync(int unidadId)
        {
            return await _context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .Where(c => c.UnidadId == unidadId && !c.EstaActivo)
                .OrderByDescending(c => c.FechaFinContrato)
                .ToListAsync();
        }

        public async Task<IEnumerable<ContratoInquilino>> GetAllContratosHistoricosAsync()
        {
            return await _context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .Where(c => !c.EstaActivo)
                .OrderByDescending(c => c.FechaFinContrato)
                .ToListAsync();
        }

        public async Task<bool> UpdateContratoAsync(ContratoInquilino contrato, Persona inquilino)
        {
            var dbContrato = await _context.ContratosInquilinos.FindAsync(contrato.ContratoId);
            if (dbContrato == null) return false;

            var dbInquilino = await _context.Personas.FindAsync(inquilino.PersonaId);
            if (dbInquilino == null) return false;

            // Update Inquilino
            dbInquilino.RazonSocial = inquilino.RazonSocial;
            dbInquilino.Documento = inquilino.Documento;
            dbInquilino.Telefono = inquilino.Telefono;
            dbInquilino.Email = inquilino.Email;

            // Update Contrato
            dbContrato.FechaIngreso = contrato.FechaIngreso;
            dbContrato.FechaInicioContrato = contrato.FechaInicioContrato;
            dbContrato.FechaFinContrato = contrato.FechaFinContrato;
            dbContrato.MontoMensualAlquiler = contrato.MontoMensualAlquiler;
            dbContrato.DepositoGarantia = contrato.DepositoGarantia;
            dbContrato.CantidadOcupantes = contrato.CantidadOcupantes;
            dbContrato.VehiculoInfo = contrato.VehiculoInfo;
            dbContrato.Observaciones = contrato.Observaciones;
            dbContrato.ServiciosIncluidos = contrato.ServiciosIncluidos;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DesalojarUnidadAsync(int contratoId)
        {
            var dbContrato = await _context.ContratosInquilinos
                .Include(c => c.Unidad)
                .FirstOrDefaultAsync(c => c.ContratoId == contratoId);

            if (dbContrato == null) return false;

            // No permitir cambios si ya es definitivo
            if (dbContrato.EstadoHistorico == "Definitivo") return false;

            // Finalizar contrato como temporal (con intención de renovar)
            dbContrato.EstaActivo = false;
            dbContrato.FechaFinContrato = DateTime.Now;
            dbContrato.EstadoHistorico = "Temporal";

            // Liberar unidad
            if (dbContrato.Unidad != null)
            {
                dbContrato.Unidad.Estado = "Disponible";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AnularDesalojoAsync(int contratoId)
        {
            var dbContrato = await _context.ContratosInquilinos
                .Include(c => c.Unidad)
                .FirstOrDefaultAsync(c => c.ContratoId == contratoId);

            if (dbContrato == null) return false;

            // No permitir anular si el estado es Definitivo (irreversible)
            if (dbContrato.EstadoHistorico == "Definitivo") return false;

            // Si hay otros contratos posteriores o activos para esta unidad (ej: por una renovación que se está anulando), reasignamos sus pagos y los eliminamos
            var contratosPosteriores = await _context.ContratosInquilinos
                .Where(c => c.UnidadId == dbContrato.UnidadId && c.ContratoId != contratoId && (c.EstaActivo || c.ContratoId > contratoId))
                .ToListAsync();

            if (contratosPosteriores.Any())
            {
                foreach (var post in contratosPosteriores)
                {
                    var pagosPost = await _context.PagosRecibos.Where(p => p.ContratoId == post.ContratoId).ToListAsync();
                    foreach (var pago in pagosPost)
                    {
                        pago.ContratoId = dbContrato.ContratoId;
                    }
                }
                _context.ContratosInquilinos.RemoveRange(contratosPosteriores);
            }

            // Restablecer contrato como activo
            dbContrato.EstaActivo = true;
            dbContrato.EstadoHistorico = "Ocupado";

            // Si la fecha de inicio es posterior al 1 de enero del año actual (ej. creado por renovación en el mismo año), restablecer al 1 de enero
            if (dbContrato.FechaInicioContrato.Year == DateTime.Now.Year && dbContrato.FechaInicioContrato.Month > 1)
            {
                dbContrato.FechaInicioContrato = new DateTime(dbContrato.FechaInicioContrato.Year, 1, 1);
                if (dbContrato.FechaIngreso.Year == DateTime.Now.Year && dbContrato.FechaIngreso.Month > 1)
                {
                    dbContrato.FechaIngreso = new DateTime(dbContrato.FechaInicioContrato.Year, 1, 1);
                }
            }

            if (dbContrato.FechaFinContrato <= DateTime.Now.AddDays(1))
            {
                dbContrato.FechaFinContrato = new DateTime(dbContrato.FechaInicioContrato.Year + 1, 12, 31);
            }

            // Restablecer estado de la unidad como ocupada
            if (dbContrato.Unidad != null)
            {
                dbContrato.Unidad.Estado = "Ocupado";
            }

            // Restablecer estado de morosidad si correspondiese o actualizar EstaDesalojado
            var morosidades = await _context.MorosidadesInquilino
                .Where(m => m.InquilinoId == dbContrato.InquilinoId || m.UnidadId == dbContrato.UnidadId)
                .ToListAsync();

            foreach (var mor in morosidades)
            {
                mor.EstaDesalojado = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DesalojoDefinitivoAsync(int contratoId)
        {
            var dbContrato = await _context.ContratosInquilinos
                .Include(c => c.Unidad)
                .FirstOrDefaultAsync(c => c.ContratoId == contratoId);

            if (dbContrato == null) return false;

            // Si ya es definitivo, no hacemos nada
            if (dbContrato.EstadoHistorico == "Definitivo") return true;

            // Marcar como definitivo e irreversible
            dbContrato.EstaActivo = false;
            dbContrato.EstadoHistorico = "Definitivo";
            if (dbContrato.FechaFinContrato > DateTime.Now)
                dbContrato.FechaFinContrato = DateTime.Now;

            // Liberar unidad si aún la tenía
            if (dbContrato.Unidad != null && dbContrato.Unidad.Estado == "Ocupado")
            {
                // Verificar que no haya otro contrato activo para la misma unidad
                var otroActivo = await _context.ContratosInquilinos
                    .AnyAsync(c => c.UnidadId == dbContrato.UnidadId && c.ContratoId != contratoId && c.EstaActivo);
                if (!otroActivo)
                {
                    dbContrato.Unidad.Estado = "Disponible";
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ContratoInquilino>> GetAllContratosConEstadoAsync()
        {
            return await _context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .OrderByDescending(c => c.ContratoId)
                .ToListAsync();
        }
    }
}
