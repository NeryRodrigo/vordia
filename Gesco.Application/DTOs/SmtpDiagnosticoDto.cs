namespace Gesco.Application.DTOs
{
    /// <summary>
    /// Resultado del chequeo de correo saliente: sirve para distinguir un problema
    /// de configuración, de credenciales de Gmail o de firewall del servidor.
    /// </summary>
    public class SmtpDiagnosticoDto
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = "(no configurado)";
        public bool TienePassword { get; set; }
        public int LargoPassword { get; set; }

        /// <summary>El servidor puede abrir el puerto SMTP (descarta firewall).</summary>
        public bool ConexionOk { get; set; }
        public string? ConexionError { get; set; }

        /// <summary>Se envió un correo de prueba al propio remitente.</summary>
        public bool EnvioOk { get; set; }
        public string? EnvioError { get; set; }

        public string Resumen { get; set; } = string.Empty;
    }
}
