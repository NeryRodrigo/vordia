using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// Genera una URL de pago (Checkout) en la pasarela seleccionada.
        /// </summary>
        /// <param name="empresaId">El ID del inquilino/cliente que va a pagar.</param>
        /// <param name="monto">Monto a cobrar.</param>
        /// <param name="moneda">Moneda (ej. "USD" o "PYG").</param>
        /// <param name="descripcion">Descripción del servicio (ej. "Suscripción Mensual GESCO Data Intelligence").</param>
        /// <returns>URL a la cual redirigir al usuario para que ponga su tarjeta.</returns>
        Task<string> GenerarLinkDePagoAsync(int empresaId, decimal monto, string moneda, string descripcion);

        /// <summary>
        /// Nombre del proveedor (ej. "Stripe", "Bancard"). 
        /// Sirve para el patrón Strategy.
        /// </summary>
        string ProviderName { get; }
    }
}
