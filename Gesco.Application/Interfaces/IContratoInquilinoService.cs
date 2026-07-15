using System.Collections.Generic;
using System.Threading.Tasks; 
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IContratoInquilinoService
    {
        Task<ContratoInquilino> CreateContratoAsync(ContratoInquilino contrato, Persona inquilino);
        Task<PagoRecibo> RegisterPagoAsync(PagoRecibo pago);
        Task<ContratoInquilino?> GetContratoByUnidadAsync(int unidadId);
        Task<IEnumerable<ContratoInquilino>> GetContratosHistoricosByUnidadAsync(int unidadId);
        Task<IEnumerable<ContratoInquilino>> GetAllContratosHistoricosAsync();
        Task<bool> UpdateContratoAsync(ContratoInquilino contrato, Persona inquilino);
        Task<IEnumerable<PagoRecibo>> GetAllPagosAsync();
        Task<bool> DeletePagoAsync(int pagoId);
        Task<bool> UpdatePagoAsync(PagoRecibo pago);
        Task<bool> UpdatePagoComprobanteAsync(int pagoId, bool facturaEmitida, string? numeroFactura, string? comprobanteUrl);
        Task<bool> DesalojarUnidadAsync(int contratoId);
        Task<bool> AnularDesalojoAsync(int contratoId);
    }
}
