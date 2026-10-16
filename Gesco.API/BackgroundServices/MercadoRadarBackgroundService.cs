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
    /// Radar de Necesidades del Mercado (Vordia HQ): cada 6 horas busca pedidos nuevos de
    /// sistemas y aplicaciones en las fuentes activas. La deduplicación por fuente + id externo
    /// evita repetir pedidos aunque haya varias corridas por día.
    /// </summary>
    public class MercadoRadarBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(6);

        private readonly ILogger<MercadoRadarBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public MercadoRadarBackgroundService(ILogger<MercadoRadarBackgroundService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Margen para que la base termine de levantar
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var radar = scope.ServiceProvider.GetRequiredService<IMercadoService>();
                    var nuevas = await radar.SincronizarTodasAsync(stoppingToken);
                    _logger.LogInformation("MercadoHQ: radar ejecutado, {Nuevas} necesidades nuevas.", nuevas);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Una corrida fallida no debe apagar el radar
                    _logger.LogError(ex, "MercadoHQ: error ejecutando el radar de necesidades.");
                }

                await Task.Delay(Intervalo, stoppingToken);
            }
        }
    }
}
