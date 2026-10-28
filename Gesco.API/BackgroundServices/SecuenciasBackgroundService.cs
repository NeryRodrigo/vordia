using Gesco.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.API.BackgroundServices
{
    /// <summary>Cada 15 minutos envía los correos pendientes de las secuencias activas (solo en horario hábil).</summary>
    public class SecuenciasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<SecuenciasBackgroundService> _log;

        public SecuenciasBackgroundService(IServiceProvider sp, ILogger<SecuenciasBackgroundService> log)
        {
            _sp = sp;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
            do
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var r = await scope.ServiceProvider.GetRequiredService<ISecuenciasService>().ProcesarAsync(null, stoppingToken);
                    if (r.Enviados + r.Simulados + r.Errores + r.Bajas > 0)
                        _log.LogInformation("Secuencias: {Enviados} enviados, {Simulados} simulados, {Errores} errores, {Bajas} bajas", r.Enviados, r.Simulados, r.Errores, r.Bajas);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "Error procesando secuencias de correo");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
