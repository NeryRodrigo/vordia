using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Gesco.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Gesco.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> EnviarComprobanteAsync(
            string emailDestino,
            string nombreCliente,
            string numeroComprobante,
            string tipoEmision,
            decimal monto,
            string concepto,
            byte[]? pdfAdjunto = null)
        {
            concepto = FormatearNombreUnidad(concepto);
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var senderName = _config["EmailSettings:SenderName"] ?? "Gesco Real Estate";
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                {
                    throw new Exception("No están configuradas las credenciales SMTP en appsettings.json.");
                }

                var fromAddress = new MailAddress(senderEmail, senderName);
                var toAddress = new MailAddress(emailDestino, nombreCliente);

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = $"Comprobante de Pago N° {numeroComprobante} - {senderName}",
                    IsBodyHtml = true,
                    Body = GetHtmlBody(nombreCliente, numeroComprobante, tipoEmision, monto, concepto)
                };

                if (pdfAdjunto != null && pdfAdjunto.Length > 0)
                {
                    var stream = new MemoryStream(pdfAdjunto);
                    var attachment = new Attachment(stream, $"Comprobante_{numeroComprobante}.pdf", MediaTypeNames.Application.Pdf);
                    message.Attachments.Add(attachment);
                }

                using var smtp = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo SMTP: {ex.Message}");
                throw;
            }
        }

        private string GetHtmlBody(string nombreCliente, string numeroComprobante, string tipoEmision, decimal monto, string concepto)
        {
            var montoStr = monto.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; color: #1e293b;'>
                <div style='text-align: center; border-bottom: 2px solid #0f172a; padding-bottom: 16px; margin-bottom: 24px;'>
                    <h1 style='color: #0f172a; margin: 0; font-size: 24px; font-weight: 900;'>GESCO REAL ESTATE</h1>
                    <p style='color: #64748b; margin: 4px 0 0 0; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Gestión de Alquileres y Administración</p>
                </div>
                <h2 style='color: #0f172a; font-size: 18px; margin-top: 0;'>¡Hola, {nombreCliente}!</h2>
                <p style='font-size: 14px; line-height: 1.6; color: #475569;'>
                    Adjunto a este correo te enviamos el <strong>{tipoEmision}</strong> correspondiente a tu pago registrado en nuestro sistema.
                </p>
                <div style='background-color: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 16px; margin: 20px 0;'>
                    <table style='width: 100%; font-size: 13px; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b;'>Nº de Comprobante:</td>
                            <td style='padding: 6px 0; font-weight: bold; color: #0f172a; text-align: right;'>{numeroComprobante}</td>
                        </tr>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b;'>Concepto:</td>
                            <td style='padding: 6px 0; font-weight: bold; color: #0f172a; text-align: right;'>{concepto}</td>
                        </tr>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 10px;'>Total Pagado:</td>
                            <td style='padding: 6px 0; font-weight: 900; color: #16a34a; font-size: 16px; text-align: right; border-top: 1px solid #e2e8f0; padding-top: 10px;'>Gs. {montoStr}</td>
                        </tr>
                    </table>
                </div>
                <p style='font-size: 13px; color: #64748b; margin-top: 24px;'>
                    Si tienes alguna duda o consulta con respecto a este comprobante, puedes responder directamente a este correo o contactar con administración.
                </p>
                <div style='border-top: 1px solid #e2e8f0; margin-top: 32px; padding-top: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p style='margin: 0;'>Gesco Real Estate — Administración Inmobiliaria</p>
                    <p style='margin: 4px 0 0 0;'>Luque, Paraguay</p>
                </div>
            </div>";
        }

        public async Task<bool> EnviarAlertaVencimientoAsync(
            string emailInquilino,
            string nombreInquilino,
            string nombreUnidad,
            decimal montoMensual,
            DateTime fechaVencimiento,
            int diasRestantes)
        {
            nombreUnidad = FormatearNombreUnidad(nombreUnidad);
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var senderName = _config["EmailSettings:SenderName"] ?? "Gesco Real Estate";
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                {
                    throw new Exception("No están configuradas las credenciales SMTP en appsettings.json.");
                }

                var message = new MailMessage();
                message.From = new MailAddress(senderEmail, senderName);

                // Destinatario principal: Inquilino (si tiene email válido)
                if (!string.IsNullOrWhiteSpace(emailInquilino) && emailInquilino.Contains("@"))
                {
                    message.To.Add(new MailAddress(emailInquilino.Trim()));
                }

                // Copia/Alerta para el Administrador del Sistema
                message.Bcc.Add(new MailAddress(senderEmail, "Gesco Administrador"));

                var diasTexto = diasRestantes == 0 ? "HOY" : $"en {diasRestantes} días";
                message.Subject = diasRestantes == 0
                    ? $"⚠️ ALERTA DE VENCIMIENTO HOY: Cuota de Alquiler — {nombreUnidad}"
                    : $"⚠️ ALERTA: Cuota de Alquiler vence {diasTexto} ({fechaVencimiento:dd/MM/yyyy}) — {nombreUnidad}";

                message.IsBodyHtml = true;
                message.Body = GenerateAlertaVencimientoHtml(nombreInquilino, nombreUnidad, montoMensual, fechaVencimiento, diasRestantes);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando alerta de vencimiento por correo: {ex.Message}");
                return false;
            }
        }

        private string GenerateAlertaVencimientoHtml(string nombreInquilino, string nombreUnidad, decimal montoMensual, DateTime fechaVencimiento, int diasRestantes)
        {
            var montoStr = montoMensual.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            var badgeColor = diasRestantes == 0 ? "#dc2626" : "#f59e0b";
            var badgeText = diasRestantes == 0 ? "VENCE HOY" : $"VENCE EN {diasRestantes} DÍAS";

            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                <div style='text-align: center; border-bottom: 2px solid #0f172a; padding-bottom: 16px; margin-bottom: 24px;'>
                    <h2 style='color: #0f172a; margin: 0; font-size: 22px; font-weight: 900; letter-spacing: -0.5px;'>GESCO REAL ESTATE</h2>
                    <p style='color: #64748b; margin: 4px 0 0 0; font-size: 13px; text-transform: uppercase; font-weight: 600;'>Alerta Automática de Vencimiento de Alquiler</p>
                </div>
                
                <div style='margin-bottom: 24px;'>
                    <span style='display: inline-block; background-color: {badgeColor}; color: #ffffff; font-size: 11px; font-weight: bold; padding: 4px 12px; border-radius: 999px; text-transform: uppercase; letter-spacing: 0.5px;'>
                        {badgeText}
                    </span>
                    <h3 style='color: #0f172a; margin: 12px 0 8px 0; font-size: 18px;'>Estimado/a {nombreInquilino},</h3>
                    <p style='color: #334155; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                        Le recordamos amablemente que la cuota mensual correspondiente al alquiler de su inmueble se encuentra próxima a vencer. A continuación detallamos la información del pago:
                    </p>
                    <p style='color: #dc2626; font-size: 13px; line-height: 1.5; margin: 0; font-weight: 600;'>
                        ⚠️ Si no paga en la fecha establecida, al monto del alquiler se le acumularía Gs. 30.000 por día en concepto de multa por falta de pago.
                    </p>
                </div>

                <div style='background-color: #f8fafc; border-left: 4px solid {badgeColor}; border-radius: 8px; padding: 18px; margin-bottom: 24px;'>
                    <table style='width: 100%; font-size: 14px; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b;'>Unidad / Inmueble:</td>
                            <td style='padding: 6px 0; font-weight: bold; color: #0f172a; text-align: right;'>{nombreUnidad}</td>
                        </tr>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b;'>Fecha Límite de Pago:</td>
                            <td style='padding: 6px 0; font-weight: bold; color: #0f172a; text-align: right;'>{fechaVencimiento:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding: 6px 0; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 10px;'>Importe Mensual:</td>
                            <td style='padding: 6px 0; font-weight: 900; color: #0f172a; font-size: 17px; text-align: right; border-top: 1px solid #e2e8f0; padding-top: 10px;'>Gs. {montoStr}</td>
                        </tr>
                    </table>
                </div>

                <div style='background-color: #f1f5f9; border-radius: 8px; padding: 14px; margin-bottom: 24px; font-size: 13px; color: #334155;'>
                    <p style='margin: 0 0 6px 0; font-weight: bold; color: #0f172a;'>Opciones de Pago:</p>
                    <p style='margin: 0; line-height: 1.6;'>• Transferencia Bancaria: <strong>Alias 3389403 - Bancop</strong><br/>• Por favor, envíe su comprobante por WhatsApp a este número <strong>0994254655</strong>, una vez realizado el pago para procesar su recibo.</p>
                </div>

                <p style='font-size: 13px; color: #64748b; margin-top: 24px;'>
                    Si ya realizó el pago en las últimas horas, por favor ignore este mensaje automatizado.
                </p>
                <div style='border-top: 1px solid #e2e8f0; margin-top: 32px; padding-top: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p style='margin: 0;'>Gesco Real Estate — Administración Inmobiliaria</p>
                    <p style='margin: 4px 0 0 0;'>Luque, Paraguay</p>
                </div>
            </div>";
        }

        private static string FormatearNombreUnidad(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;
            return Regex.Replace(
                texto,
                @"\bD\s*-?\s*(\d+)\b",
                m => $"Dúplex {int.Parse(m.Groups[1].Value):D2}",
                RegexOptions.IgnoreCase);
        }
    }
}
