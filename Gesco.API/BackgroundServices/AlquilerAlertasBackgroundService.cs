using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;

namespace Gesco.API.BackgroundServices
{
    public class AlquilerAlertasBackgroundService : BackgroundService
    {
        private readonly ILogger<AlquilerAlertasBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AlquilerAlertasBackgroundService(ILogger<AlquilerAlertasBackgroundService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertaVencimientosBot: Iniciando servicio de notificaciones automáticas de alquiler.");

            // Esperar 30 segundos tras arrancar el servidor antes de verificar por primera vez
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarYEnviarAlertasAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AlertaVencimientosBot: Error procesando alertas de vencimiento de alquiler.");
                }

                // Revisar de nuevo cada 12 horas (o cada 24 horas)
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        private async Task VerificarYEnviarAlertasAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateTime.Now.Date;

            // Obtener contratos activos que tengan inquilino y unidad
            var contratosActivos = await context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .Where(c => c.EstaActivo)
                .ToListAsync();

            _logger.LogInformation($"AlertaVencimientosBot: Revisando {contratosActivos.Count} contratos activos para alertas de 5 días.");

            foreach (var contrato in contratosActivos)
            {
                var diaPago = contrato.DiaVencimiento > 0 ? contrato.DiaVencimiento : 10;
                var anio = today.Year;
                var mes = today.Month;

                // Calcular fecha de vencimiento del mes en curso
                int daysInMonth = DateTime.DaysInMonth(anio, mes);
                var diaReal = Math.Min(diaPago, daysInMonth);
                var fechaVencimiento = new DateTime(anio, mes, diaReal);

                // Si ya pasó la fecha de vencimiento del mes actual, evaluar si falta para el mes siguiente
                if (today > fechaVencimiento)
                {
                    var proxMesDate = today.AddMonths(1);
                    int daysInNextMonth = DateTime.DaysInMonth(proxMesDate.Year, proxMesDate.Month);
                    fechaVencimiento = new DateTime(proxMesDate.Year, proxMesDate.Month, Math.Min(diaPago, daysInNextMonth));
                }

                int diasRestantes = (int)(fechaVencimiento.Date - today).TotalDays;

                // Si faltan exactamente 5 días (o 1 día) para vencer
                if (diasRestantes == 5 || diasRestantes == 1)
                {
                    // Verificar si ya existe pago registrado para este mes/periodo
                    bool yaPAGO = contrato.Pagos.Any(p =>
                        p.AnioPagado == fechaVencimiento.Year &&
                        p.MesPagado == fechaVencimiento.Month);

                    if (!yaPAGO)
                    {
                        string emailInquilino = contrato.Inquilino?.Email ?? "";
                        string nombreInquilino = contrato.Inquilino?.RazonSocial ?? "Cliente";
                        string nombreUnidadRaw = contrato.Unidad != null ? $"{contrato.Unidad.NombreCondominio} - {contrato.Unidad.CodigoUnidad}" : $"Dúplex #{contrato.UnidadId}";
                        string nombreUnidad = FormatearNombreUnidad(nombreUnidadRaw);

                        _logger.LogInformation($"AlertaVencimientosBot: Enviando alerta ({diasRestantes} días) a {nombreInquilino} ({emailInquilino}) por unidad {nombreUnidad}");

                        await emailService.EnviarAlertaVencimientoAsync(
                            emailInquilino,
                            nombreInquilino,
                            nombreUnidad,
                            contrato.MontoMensualAlquiler > 0 ? contrato.MontoMensualAlquiler : contrato.MontoMensualGs,
                            fechaVencimiento,
                            diasRestantes
                        );
                    }
                }
            }
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
