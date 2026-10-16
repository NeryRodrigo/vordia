using System.Threading.Tasks;
using Gesco.Application.DTOs;

namespace Gesco.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>Verifica configuración, conectividad y credenciales del correo saliente.</summary>
        Task<SmtpDiagnosticoDto> DiagnosticarSmtpAsync();

        /// <summary>
        /// Aviso interno de Vordia HQ (motor de automatizaciones). Si no se indica
        /// destinatario, se envía a la propia cuenta remitente configurada.
        /// </summary>
        Task<(bool Enviado, string? Error)> EnviarNotificacionHqAsync(
            string? emailDestino,
            string asunto,
            string mensaje
        );

        /// <summary>
        /// Correo comercial a un prospecto (Necesidades del Mercado). Las respuestas van a
        /// <paramref name="responderA"/> o, si no se indica, a la casilla remitente.
        /// </summary>
        Task<(bool Enviado, string? Error)> EnviarCorreoComercialAsync(
            string emailDestino,
            string asunto,
            string mensaje,
            string? responderA = null
        );

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
            int diasRestantes,
            string? emailCopia = null
        );

        /// <summary>
        /// Igual que EnviarAlertaVencimientoAsync pero devuelve el motivo de la falla,
        /// para poder mostrarlo en pantalla en lugar de un error genérico.
        /// </summary>
        Task<(bool Enviado, string? Error)> EnviarAlertaVencimientoDetalladoAsync(
            string emailInquilino,
            string nombreInquilino,
            string nombreUnidad,
            decimal montoMensual,
            DateTime fechaVencimiento,
            int diasRestantes,
            string? emailCopia = null
        );
    }
}
