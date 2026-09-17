using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using Gesco.Application.DTOs;
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
                var senderName = _config["EmailSettings:SenderName"] ?? "Fontana - Administración";
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = LimpiarAppPassword(_config["EmailSettings:AppPassword"]);

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                {
                    throw new Exception("No están configuradas las credenciales SMTP en appsettings.json.");
                }

                var fromAddress = new MailAddress(senderEmail, senderName);
                var toAddress = new MailAddress(emailDestino, nombreCliente);

                var tipoLabel = NormalizarTipoEmision(tipoEmision);
                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = $"{tipoLabel} N° {numeroComprobante} - {senderName}",
                    IsBodyHtml = true
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(
                    GetHtmlBody(nombreCliente, numeroComprobante, tipoLabel, monto, concepto),
                    Encoding.UTF8,
                    MediaTypeNames.Text.Html);
                AdjuntarLogoEmail(htmlView, "fontanas.png", "fontanaLogo");
                AdjuntarLogoEmail(htmlView, "VORDIA H NEGRO.png", "vordiaLogo");
                message.AlternateViews.Add(htmlView);

                if (pdfAdjunto != null && pdfAdjunto.Length > 0)
                {
                    var stream = new MemoryStream(pdfAdjunto);
                    var attachment = new Attachment(stream, $"{tipoLabel}_{numeroComprobante}.pdf", MediaTypeNames.Application.Pdf);
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

        private static string NormalizarTipoEmision(string? tipoEmision)
        {
            if (string.IsNullOrWhiteSpace(tipoEmision)) return "FACTURA";
            return tipoEmision.ToUpperInvariant().Contains("FACTURA") ? "FACTURA" : "RECIBO";
        }

        private string GetHtmlBody(string nombreCliente, string numeroComprobante, string tipoEmision, decimal monto, string concepto)
        {
            var montoStr = monto.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            var tipoLabel = NormalizarTipoEmision(tipoEmision);
            const string colorFondoApp = "#080E1E";
            return $@"
            <div style='font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; color: #1e293b; background:#ffffff;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='border-collapse: collapse; border-bottom: 2px solid #0f172a; margin-bottom: 24px;'>
                    <tr>
                        <td width='72' valign='middle' style='padding: 0 12px 16px 0;'>
                            <table role='presentation' cellpadding='0' cellspacing='0' style='border-collapse: collapse;'>
                                <tr>
                                    <td bgcolor='{colorFondoApp}' style='background-color: {colorFondoApp}; border-radius: 8px; padding: 8px; width: 64px; height: 64px; text-align: center;'>
                                        <img src='cid:fontanaLogo' alt='Fontana' width='48' height='48' style='display: block; margin: 0 auto; border: 0; outline: none; max-width: 48px; height: auto;' />
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td align='left' valign='middle' style='padding: 0 0 16px 0;'>
                            <p style='color: #1E7BFF; margin: 0; font-size: 12px; text-transform: uppercase; font-weight: 700; letter-spacing: 0.5px;'>Fontana Di Luque</p>
                            <p style='color: #64748b; margin: 4px 0 0 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;'>{tipoLabel} electrónico</p>
                        </td>
                    </tr>
                </table>
                <h2 style='color: #0f172a; font-size: 18px; margin-top: 0;'>¡Hola, {nombreCliente}!</h2>
                <p style='font-size: 14px; line-height: 1.6; color: #475569;'>
                    Adjunto a este correo te enviamos el <strong>{tipoLabel}</strong> correspondiente a tu pago registrado en nuestro sistema.
                </p>
                <div style='background-color: #f8fafc; border-radius: 8px; padding: 20px; margin: 24px 0; border-left: 4px solid #3b82f6;'>
                    <table style='width: 100%; font-size: 14px; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b;'>N° Comprobante:</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #0f172a;'>{numeroComprobante}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b;'>Concepto:</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #0f172a;'>{concepto}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; border-top: 1px solid #e2e8f0;'>Monto:</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: 900; font-size: 18px; color: #059669; border-top: 1px solid #e2e8f0;'>Gs. {montoStr}</td>
                        </tr>
                    </table>
                </div>
                <p style='font-size: 12px; color: #94a3b8; text-align: center; margin-top: 32px;'>
                    Este es un correo automático. Por favor no responda a este mensaje.<br/>
                    Si tiene dudas, comuníquese con la administración.
                </p>
                <div style='text-align: right; margin-top: 20px; padding-top: 16px; border-top: 1px solid #e2e8f0;'>
                    <img src='cid:vordiaLogo' alt='Vordia' width='56' height='56' style='display: inline-block; border: 0; outline: none;' />
                </div>
            </div>";
        }

        public async Task<bool> EnviarAlertaVencimientoAsync(
            string emailInquilino,
            string nombreInquilino,
            string nombreUnidad,
            decimal montoMensual,
            DateTime fechaVencimiento,
            int diasRestantes,
            string? emailCopia = null)
        {
            var (enviado, _) = await EnviarAlertaVencimientoDetalladoAsync(
                emailInquilino, nombreInquilino, nombreUnidad, montoMensual, fechaVencimiento, diasRestantes, emailCopia);
            return enviado;
        }

        public async Task<(bool Enviado, string? Error)> EnviarAlertaVencimientoDetalladoAsync(
            string emailInquilino,
            string nombreInquilino,
            string nombreUnidad,
            decimal montoMensual,
            DateTime fechaVencimiento,
            int diasRestantes,
            string? emailCopia = null)
        {
            nombreUnidad = FormatearNombreUnidad(nombreUnidad);
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var senderName = _config["EmailSettings:SenderName"] ?? "Fontana - Administración";
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = LimpiarAppPassword(_config["EmailSettings:AppPassword"]);

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                {
                    throw new Exception("No están configuradas las credenciales SMTP en appsettings.json.");
                }

                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail, senderName);

                // Destinatario principal: Inquilino (si tiene email válido)
                if (!string.IsNullOrWhiteSpace(emailInquilino) && emailInquilino.Contains("@"))
                {
                    message.To.Add(new MailAddress(emailInquilino.Trim()));
                }

                // Copia al correo de administración indicado en el panel de alertas
                if (!string.IsNullOrWhiteSpace(emailCopia) && emailCopia.Contains("@")
                    && !string.Equals(emailCopia.Trim(), emailInquilino?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    message.CC.Add(new MailAddress(emailCopia.Trim()));
                }

                // Copia/Alerta para el Administrador del Sistema
                message.Bcc.Add(new MailAddress(senderEmail, "Vordia Administrador"));

                if (message.To.Count == 0 && message.CC.Count == 0)
                    return (false, "No hay destinatario válido: el inquilino no tiene correo y no se indicó copia.");

                var diasTexto = diasRestantes == 0 ? "HOY" : $"en {diasRestantes} días";
                message.Subject = diasRestantes == 0
                    ? $"⚠️ ALERTA DE VENCIMIENTO HOY: Cuota de Alquiler — {nombreUnidad}"
                    : $"⚠️ ALERTA: Cuota de Alquiler vence {diasTexto} ({fechaVencimiento:dd/MM/yyyy}) — {nombreUnidad}";

                message.IsBodyHtml = true;
                var html = GenerateAlertaVencimientoHtml(nombreInquilino, nombreUnidad, montoMensual, fechaVencimiento, diasRestantes);
                var htmlView = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);
                AdjuntarLogoEmail(htmlView, "fontanas.png", "fontanaLogo");
                AdjuntarLogoEmail(htmlView, "VORDIA H NEGRO.png", "vordiaLogo");
                message.AlternateViews.Add(htmlView);

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true,
                    Timeout = 25000
                };

                await smtpClient.SendMailAsync(message);
                return (true, null);
            }
            catch (SmtpException ex)
            {
                var detalle = $"SMTP {ex.StatusCode}: {ex.Message}";
                Console.WriteLine($"Error enviando alerta de vencimiento por correo: {ex}");
                return (false, detalle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando alerta de vencimiento por correo: {ex}");
                return (false, ex.Message);
            }
        }

        public async Task<SmtpDiagnosticoDto> DiagnosticarSmtpAsync()
        {
            var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
            var senderName = _config["EmailSettings:SenderName"] ?? "Fontana - Administración";
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var appPassword = LimpiarAppPassword(_config["EmailSettings:AppPassword"]);

            var dto = new SmtpDiagnosticoDto
            {
                SmtpServer = smtpServer,
                SmtpPort = smtpPort,
                SenderEmail = string.IsNullOrWhiteSpace(senderEmail) ? "(no configurado)" : senderEmail,
                TienePassword = !string.IsNullOrWhiteSpace(appPassword),
                LargoPassword = (appPassword ?? string.Empty).Replace(" ", "").Length
            };

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
            {
                dto.Resumen = "Faltan las credenciales SMTP (EmailSettings) en la configuración del servidor.";
                return dto;
            }

            // 1) ¿El servidor puede abrir el puerto? Si falla acá, es firewall y no credenciales.
            try
            {
                using var tcp = new TcpClient();
                var conexion = tcp.ConnectAsync(smtpServer, smtpPort);
                var completada = await Task.WhenAny(conexion, Task.Delay(TimeSpan.FromSeconds(8)));
                if (completada != conexion || !tcp.Connected)
                    throw new TimeoutException($"Sin respuesta de {smtpServer}:{smtpPort} en 8 segundos.");
                dto.ConexionOk = true;
            }
            catch (Exception ex)
            {
                dto.ConexionError = ex.Message;
                dto.Resumen = $"El servidor no logra salir al puerto {smtpPort}. Es un bloqueo de red o firewall, no un problema de credenciales.";
                return dto;
            }

            // 2) Envío real de prueba al propio remitente: valida credenciales de Gmail.
            try
            {
                using var message = new MailMessage(
                    new MailAddress(senderEmail, senderName),
                    new MailAddress(senderEmail))
                {
                    Subject = "Prueba de configuración de correo — Vordia",
                    Body = $"Correo de prueba enviado el {DateTime.Now:dd/MM/yyyy HH:mm}. Si lo estás leyendo, el envío desde el servidor funciona.",
                    IsBodyHtml = false
                };

                using var smtp = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true,
                    Timeout = 20000
                };

                await smtp.SendMailAsync(message);
                dto.EnvioOk = true;
                dto.Resumen = $"Correo saliente funcionando. Se envió una prueba a {senderEmail}.";
            }
            catch (SmtpException ex)
            {
                dto.EnvioError = $"SMTP {ex.StatusCode}: {ex.Message}";
                dto.Resumen = "Conecta al servidor de Gmail pero rechaza el envío. Revisá la contraseña de aplicación.";
            }
            catch (Exception ex)
            {
                dto.EnvioError = ex.Message;
                dto.Resumen = "Falló el envío de prueba.";
            }

            return dto;
        }

        /// <summary>
        /// Gmail muestra las contraseñas de aplicación en grupos de 4 ("abcd efgh ijkl mnop"),
        /// pero al autenticar hay que enviarlas sin espacios.
        /// </summary>
        public async Task<(bool Enviado, string? Error)> EnviarNotificacionHqAsync(
            string? emailDestino,
            string asunto,
            string mensaje)
        {
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var appPassword = LimpiarAppPassword(_config["EmailSettings:AppPassword"]);

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                    return (false, "No están configuradas las credenciales SMTP (EmailSettings) en el servidor.");

                // Sin destinatario explícito el aviso va a la casilla de Vordia
                var destino = string.IsNullOrWhiteSpace(emailDestino) || !emailDestino.Contains("@")
                    ? senderEmail
                    : emailDestino.Trim();

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, "Vordia HQ"),
                    Subject = asunto,
                    IsBodyHtml = true,
                    Body = GenerarNotificacionHqHtml(asunto, mensaje)
                };
                message.To.Add(new MailAddress(destino));

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true,
                    Timeout = 25000
                };

                await smtpClient.SendMailAsync(message);
                return (true, null);
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Error enviando notificación de Vordia HQ: {ex}");
                return (false, $"SMTP {ex.StatusCode}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando notificación de Vordia HQ: {ex}");
                return (false, ex.Message);
            }
        }

        private static string GenerarNotificacionHqHtml(string asunto, string mensaje)
        {
            var cuerpo = System.Net.WebUtility.HtmlEncode(mensaje).Replace("\n", "<br/>");
            var titulo = System.Net.WebUtility.HtmlEncode(asunto);

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'></head>
<body style='margin:0;padding:24px;background:#050B18;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='max-width:560px;margin:0 auto;background:#0C1327;border:1px solid rgba(255,255,255,0.08);border-radius:16px;overflow:hidden;'>
    <tr>
      <td style='padding:20px 24px;background:#007bff;'>
        <div style='color:#ffffff;font-size:12px;letter-spacing:2px;text-transform:uppercase;'>Vordia HQ</div>
        <div style='color:#ffffff;font-size:19px;font-weight:700;margin-top:4px;'>{titulo}</div>
      </td>
    </tr>
    <tr>
      <td style='padding:24px;color:#dbe4f5;font-size:15px;line-height:1.6;'>{cuerpo}</td>
    </tr>
    <tr>
      <td style='padding:16px 24px;border-top:1px solid rgba(255,255,255,0.08);color:#7c8db5;font-size:12px;'>
        Aviso generado automáticamente por el motor de automatizaciones. Podés pausar o ajustar esta regla en Vordia HQ · Automatizaciones.
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        private static string LimpiarAppPassword(string? appPassword) =>
            (appPassword ?? string.Empty).Replace(" ", "").Trim();

        /// <summary>
        /// Resuelve logos en wwwroot/email (misma convención que plantillas del API).
        /// </summary>
        private static string? ResolverRutaLogoEmail(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email", fileName),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "email", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "email", fileName),
            };

            return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
        }

        private static void AdjuntarLogoEmail(AlternateView view, string fileName, string contentId)
        {
            var path = ResolverRutaLogoEmail(fileName);
            if (path == null) return;

            var resource = new LinkedResource(path, "image/png")
            {
                ContentId = contentId,
                TransferEncoding = TransferEncoding.Base64,
                ContentType = { Name = fileName }
            };
            view.LinkedResources.Add(resource);
        }

        private string GenerateAlertaVencimientoHtml(string nombreInquilino, string nombreUnidad, decimal montoMensual, DateTime fechaVencimiento, int diasRestantes)
        {
            var montoStr = montoMensual.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            var badgeColor = diasRestantes == 0 ? "#dc2626" : "#f59e0b";
            var badgeText = diasRestantes == 0
                ? "VENCE HOY"
                : diasRestantes == 1
                    ? "VENCE EN 1 DÍA"
                    : $"VENCE EN {diasRestantes} DÍAS";

            // Color del sidebar del sistema (#080E1E) para que fontanas.png (sin fondo) se vea igual que en la app
            const string colorFondoApp = "#080E1E";

            return $@"
            <div style='font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='border-collapse: collapse; border-bottom: 1px solid #e2e8f0; padding-bottom: 16px; margin-bottom: 24px;'>
                    <tr>
                        <td width='72' valign='middle' style='padding: 0 12px 16px 0;'>
                            <table role='presentation' cellpadding='0' cellspacing='0' style='border-collapse: collapse;'>
                                <tr>
                                    <td bgcolor='{colorFondoApp}' style='background-color: {colorFondoApp}; border-radius: 8px; padding: 8px; width: 64px; height: 64px; text-align: center;'>
                                        <img src='cid:fontanaLogo' alt='Fontana' width='48' height='48' style='display: block; margin: 0 auto; border: 0; outline: none; max-width: 48px; height: auto;' />
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td align='left' valign='middle' style='padding: 0 0 16px 0;'>
                            <p style='color: #3b82f6; margin: 0; font-size: 12px; text-transform: uppercase; font-weight: 700; letter-spacing: 0.5px; line-height: 1.4;'>Alerta Automática de Vencimiento de Alquiler</p>
                        </td>
                    </tr>
                </table>

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
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='border-collapse: collapse; border-top: 1px solid #e2e8f0; margin-top: 32px; padding-top: 16px;'>
                    <tr>
                        <td align='left' valign='middle' style='padding-top: 16px; font-size: 11px; color: #94a3b8;'>
                            <p style='margin: 0;'>Vordia — Administración Inmobiliaria</p>
                        </td>
                        <td align='right' valign='middle' style='padding-top: 16px;'>
                            <img src='cid:vordiaLogo' alt='Vordia' width='140' style='display: inline-block; border: 0; outline: none; max-width: 140px; height: auto;' />
                        </td>
                    </tr>
                </table>
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
