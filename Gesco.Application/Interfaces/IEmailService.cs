using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> EnviarComprobanteAsync(
            string emailDestino,
            string nombreCliente,
            string numeroComprobante,
            string tipoEmision,
            decimal monto,
            string concepto,
            byte[]? pdfAdjunto = null
        );

        Task<bool> EnviarAlertaVencimientoAsync(
            string emailInquilino,
            string nombreInquilino,
            string nombreUnidad,
            decimal montoMensual,
            DateTime fechaVencimiento,
            int diasRestantes
        );
    }
}
