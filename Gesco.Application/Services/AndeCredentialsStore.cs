using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gesco.Application.Services
{
    public interface IAndeCredentialsStore
    {
        /// <summary>True solo si hay CI/clave en memoria de esta sesión (no appsettings).</summary>
        bool EstaConfigurado();
        string? DocumentoEnmascarado();
        string GetDocumento();
        string GetPassword();
        string GetTipoDocumento();
        string GetCedulaRepresentante();
        string GetTipoSolicitante();
        /// <summary>Guarda en memoria de proceso, valida contra ANDE. No escribe appsettings.</summary>
        Task<(bool Ok, string Mensaje)> GuardarYValidarAsync(
            string documento,
            string password,
            string? tipoDocumento = null,
            CancellationToken ct = default);
        /// <summary>Borra CI/clave de sesión e invalida JWT ANDE (p. ej. al cerrar sesión Gesco).</summary>
        void LimpiarSesion();
    }

    /// <summary>
    /// Credenciales ANDE por sesión de uso: solo memoria.
    /// Al cerrar sesión Gesco se limpian; el próximo login vuelve a pedirlas en el modal.
    /// </summary>
    public class AndeCredentialsStore : IAndeCredentialsStore
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AndeCredentialsStore> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private string? _documento;
        private string? _password;
        private string? _tipoDocumento;

        public AndeCredentialsStore(
            IConfiguration config,
            ILogger<AndeCredentialsStore> logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool EstaConfigurado() =>
            !string.IsNullOrWhiteSpace(_documento) && !string.IsNullOrWhiteSpace(_password);

        public string? DocumentoEnmascarado()
        {
            var d = GetDocumento();
            if (string.IsNullOrWhiteSpace(d)) return null;
            if (d.Length <= 3) return new string('*', d.Length);
            return new string('*', Math.Max(0, d.Length - 3)) + d[^3..];
        }

        public string GetDocumento() => (_documento ?? "").Trim();

        public string GetPassword() => _password ?? "";

        public string GetTipoDocumento() =>
            FirstNonEmpty(_tipoDocumento, _config["AndeConfig:TipoDocumento"], "TD001");

        public string GetCedulaRepresentante() =>
            FirstNonEmpty(_config["AndeConfig:CedulaRepresentante"], "lteor");

        public string GetTipoSolicitante() =>
            FirstNonEmpty(_config["AndeConfig:TipoSolicitante"], "Sin registros");

        public async Task<(bool Ok, string Mensaje)> GuardarYValidarAsync(
            string documento,
            string password,
            string? tipoDocumento = null,
            CancellationToken ct = default)
        {
            documento = (documento ?? "").Trim();
            password = password ?? "";
            tipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? null : tipoDocumento.Trim();

            if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(password))
                return (false, "Ingresá documento y contraseña de Mi Cuenta ANDE.");

            await _gate.WaitAsync(ct);
            try
            {
                _documento = documento;
                _password = password;
                if (!string.IsNullOrWhiteSpace(tipoDocumento))
                    _tipoDocumento = tipoDocumento;

                AndeJwtConsultaService.InvalidarSesionEstatica();

                var (ok, mensaje, _) = await AndeJwtConsultaService.ProbarLoginConCredencialesAsync(
                    _config,
                    GetTipoDocumento(),
                    documento,
                    password,
                    GetCedulaRepresentante(),
                    GetTipoSolicitante(),
                    ct);

                if (!ok)
                {
                    _documento = null;
                    _password = null;
                    _logger.LogWarning("ANDE login de sesión rechazado: {Msg}", mensaje);
                    return (false, string.IsNullOrWhiteSpace(mensaje)
                        ? "ANDE rechazó el usuario/contraseña. Revisá los datos."
                        : mensaje);
                }

                _logger.LogInformation("ANDE sesión lista (doc={Doc})", DocumentoEnmascarado());
                return (true, "Sesión ANDE lista. Podés consultar saldos y PDF hasta cerrar sesión en Gesco.");
            }
            finally
            {
                _gate.Release();
            }
        }

        public void LimpiarSesion()
        {
            _documento = null;
            _password = null;
            _tipoDocumento = null;
            AndeJwtConsultaService.InvalidarSesionEstatica();
            _logger.LogInformation("ANDE sesión limpiada (logout Gesco).");
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return "";
        }
    }
}
