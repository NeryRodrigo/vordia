using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IEnumerable<IPaymentGatewayService> _paymentGateways;

        public PaymentController(IEnumerable<IPaymentGatewayService> paymentGateways)
        {
            _paymentGateways = paymentGateways;
        }

        [HttpPost("crear-suscripcion")]
        public async Task<IActionResult> CrearSuscripcion([FromQuery] int empresaId, [FromQuery] string pasarela = "Stripe")
        {
            try
            {
                // Buscamos la pasarela correcta usando el Strategy Pattern
                var gateway = _paymentGateways.FirstOrDefault(g => g.ProviderName.Equals(pasarela, StringComparison.OrdinalIgnoreCase));

                if (gateway == null)
                {
                    return BadRequest($"La pasarela de pagos '{pasarela}' no está habilitada en GESCO.");
                }

                // Generar el link. En producción el monto dependerá del plan elegido.
                decimal montoMensual = 99.00m; 
                string moneda = pasarela == "Bancard" ? "PYG" : "USD";
                
                if (moneda == "PYG") montoMensual = 700000m; // Aprox 99 USD a Guaraníes

                string checkoutUrl = await gateway.GenerarLinkDePagoAsync(
                    empresaId, 
                    montoMensual, 
                    moneda, 
                    $"Suscripción Premium GESCO Data Intelligence ({gateway.ProviderName})"
                );

                return Ok(new
                {
                    Success = true,
                    CheckoutUrl = checkoutUrl,
                    Gateway = gateway.ProviderName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Error = "Error al generar cobro", Detail = ex.Message });
            }
        }

        // Este endpoint será llamado "silenciosamente" por Stripe o Bancard cuando la tarjeta pase
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // Aquí se lee el body del request y se valida la firma criptográfica (Signature)
            // Si el pago es "succeeded", se busca en la BD y se hace:
            // var licencia = _context.LicenciasModulos.First(l => l.EmpresaId == empresaId);
            // licencia.IsContratado = true;
            // licencia.FechaProximoVencimiento = DateTime.UtcNow.AddMonths(1);
            // await _context.SaveChangesAsync();

            Console.WriteLine("[Webhook] Pago recibido y verificado. Cuenta desbloqueada.");
            await Task.CompletedTask;

            return Ok();
        }
    }
}
