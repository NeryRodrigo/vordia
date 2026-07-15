using System;
using System.Threading.Tasks;
using Gesco.Application.Interfaces;

namespace Gesco.Application.Services
{
    public class BancardPaymentService : IPaymentGatewayService
    {
        public string ProviderName => "Bancard";

        public async Task<string> GenerarLinkDePagoAsync(int empresaId, decimal monto, string moneda, string descripcion)
        {
            // Aquí iría la integración con la API REST de vPOS Bancard.
            // Requiere: Private Key, Public Key, y un "shop_process_id" único.
            // Una vez que el dominio de GESCO esté en vivo, se envía un POST a https://vpos.infonet.com.py/vpos/api/0.3/single_buy
            
            // Simulación para arquitectura inicial (hasta tener el dominio en vivo):
            await Task.Delay(100); // Simulando red
            Console.WriteLine($"[Bancard vPOS] Generando proceso de compra para Empresa {empresaId} por {monto} {moneda} (Requiere Dominio Activo)");
            
            return $"https://vpos.infonet.com.py/payment/mock_bancard_hash_{Guid.NewGuid()}";
        }
    }
}
