using System;
using System.Threading.Tasks;
using Gesco.Application.Interfaces;

namespace Gesco.Application.Services
{
    public class StripePaymentService : IPaymentGatewayService
    {
        public string ProviderName => "Stripe";

        public async Task<string> GenerarLinkDePagoAsync(int empresaId, decimal monto, string moneda, string descripcion)
        {
            // Aquí iría la integración real con el SDK de Stripe.
            // StripeConfiguration.ApiKey = "sk_test_...";
            // var options = new SessionCreateOptions { ... };
            // var service = new SessionService();
            // Session session = await service.CreateAsync(options);

            // Simulación para arquitectura inicial:
            await Task.Delay(100); // Simulando red
            Console.WriteLine($"[Stripe] Generando sesión de pago para Empresa {empresaId} por {monto} {moneda}");
            
            return $"https://checkout.stripe.com/pay/cs_test_mock_session_{Guid.NewGuid()}";
        }
    }
}
