using System;
using System.Threading;
using System.Threading.Tasks;
using Gesco.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gesco.API.BackgroundServices
{
    /// <summary>
    /// Corre el motor de automatizaciones de Vordia HQ cada 6 horas. Solo evalúa las
    /// reglas que el usuario dejó encendidas, y la bitácora de ejecuciones impide que
    /// se avise dos veces por el mismo hecho aunque haya varias corridas por día.
    /// </summary>
    public class CrmAutomatizacionesBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(6);

        private readonly ILogger<CrmAutomatizacionesBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CrmAutomatizacionesBackgroundService(
            ILogger<CrmAutomatizacionesBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutomatizacionesHQ: motor de automatizaciones iniciado.");

            // Margen para que la base y las migraciones terminen de levantar
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var motor = scope.ServiceProvider.GetRequiredService<ICrmAutomatizacionesService>();

                    var corrida = await motor.EjecutarTodasAsync();

                    if (corrida.Disparos > 0 || corrida.Errores > 0)
                        _logger.LogInformation("AutomatizacionesHQ: {Resumen}", corrida.Resumen);
                    else
                        _logger.LogDebug("AutomatizacionesHQ: {Resumen}", corrida.Resumen);
                }
                catch (Exception ex)
                {
                    // Un error de una corrida no debe apagar el motor
                    _logger.LogError(ex, "AutomatizacionesHQ: error corriendo el motor de automatizaciones.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }
    }
}
