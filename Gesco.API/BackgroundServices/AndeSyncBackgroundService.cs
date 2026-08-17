using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Gesco.Application.Services;

namespace Gesco.API.BackgroundServices
{
    public class AndeSyncBackgroundService : BackgroundService
    {
        private readonly ILogger<AndeSyncBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AndeSyncBackgroundService(ILogger<AndeSyncBackgroundService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Desactivado a propósito: no abrir Chrome ni consultar ANDE al arrancar el servidor.
            // La consulta de saldo se hace solo bajo demanda (modal Consultar ANDE en Gesco).
            _logger.LogInformation(
                "AndeBot: sync automático desactivado. Chrome/ANDE solo al consultar saldo desde Gesco.");
            return Task.CompletedTask;
        }

        private async Task ProcessAndeSyncAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var gastoService = scope.ServiceProvider.GetRequiredService<IGastoMantenimientoService>();
            var andeScraper = scope.ServiceProvider.GetRequiredService<IAndeScrapingService>();

            var unidades = await context.UnidadesCondominio
                .Where(u => !string.IsNullOrEmpty(u.NisAnde) && u.NisAnde != "N/A" && u.NisAnde != "SIN NIS")
                .ToListAsync();

            var today = DateTime.Now;

            foreach (var unidad in unidades)
            {
                if (!int.TryParse(unidad.NisAnde.Length >= 2 ? unidad.NisAnde.Substring(unidad.NisAnde.Length - 2) : "0", out int nisNum))
                {
                    nisNum = 15;
                }

                int diaVencimiento = (nisNum % 15) + 10;
                int diaEmision = Math.Max(1, diaVencimiento - 5);
                var fechaVencimiento = new DateTime(today.Year, today.Month, diaVencimiento);
                bool isVencido = today > fechaVencimiento;

                // Si aún no se emitió la factura del mes por la ANDE, limpiar cualquier registro de prueba prematuro y no simular pago
                if (today.Day < diaEmision)
                {
                    var prematuros = await context.GastosMantenimiento
                        .Where(g => g.UnidadId == unidad.UnidadId 
                                 && g.Descripcion == "Luz" 
                                 && g.MesReferencia == today.Month 
                                 && g.AnioReferencia == today.Year 
                                 && g.MetodoPago == "Débito Automático/Web Scraping")
                        .ToListAsync();

                    if (prematuros.Any())
                    {
                        context.GastosMantenimiento.RemoveRange(prematuros);
                        await context.SaveChangesAsync();
                        _logger.LogInformation($"[AndeBot] Limpieza de pagos simulados prematuros en mes {today.Month} para NIS {unidad.NisAnde}");
                    }
                    continue;
                }
                
                var pagosLuzMes = await context.GastosMantenimiento
                    .Where(g => g.UnidadId == unidad.UnidadId 
                             && g.Descripcion == "Luz" 
                             && g.MesReferencia == today.Month 
                             && g.AnioReferencia == today.Year)
                    .AnyAsync();

                if (!pagosLuzMes)
                {
                    // LECTURA DE CONSUMO REAL DE LA ANDE VIA SCRAPING / API
                    var andeInfo = await andeScraper.ConsultarConsumoAsync(unidad.NisAnde);

                    // Si en la API o consulta de ANDE salta deuda 0, es que el medidor no adeuda este mes
                    if (andeInfo.Deuda <= 0)
                    {
                        _logger.LogInformation($"[AndeBot] Medidor NIS {unidad.NisAnde} verificado en línea: Al Día (Sin deuda).");
                    }
                    else if (andeInfo.Deuda > 0)
                    {
                        int montoTotal = (int)andeInfo.Deuda;
                        var request = new GastoServicioMantenimientoRequest
                        {
                            UnidadId = unidad.UnidadId,
                            TipoServicio = "Luz",
                            IdentificadorServicio = unidad.NisAnde,
                            NroComprobante = $"ANDE-AUT-{Math.Abs(unidad.NisAnde.GetHashCode()) % 90000 + 10000}",
                            MesReferencia = today.Month,
                            AnioReferencia = today.Year,
                            Fecha = today,
                            Monto = montoTotal,
                            MetodoPago = "Débito Automático/Web Scraping",
                            Proveedor = "ANDE S.A."
                        };

                        await gastoService.CreateServicioAsync(request);
                        _logger.LogInformation($"[AndeBot] Factura LUZ registrada en sistema para NIS {unidad.NisAnde} - Unidad {unidad.CodigoUnidad}. Monto: Gs. {montoTotal} - Consumo kW: {andeInfo.Kw}");
                    }
                }
            }
        }
    }
}
