using Gesco.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    /// <summary>VORDIA HQ — contacto con el cliente y pedidos rápidos desde Necesidades del Mercado.</summary>
    public interface IMercadoVentasService
    {
        Task<MercadoNecesidadDto?> GuardarContactoAsync(int necesidadId, MercadoContactoInfoRequest request);
        Task<List<MercadoContactoDto>> ListarContactosAsync(int necesidadId, int empresaId);
        Task<MercadoContactoDto> RegistrarContactoAsync(int necesidadId, MercadoContactoRegistroRequest request);
        Task<MercadoContactoDto> EnviarCorreoAsync(int necesidadId, MercadoCorreoRequest request);

        Task<MercadoPedidoDto> CrearPedidoAsync(MercadoPedidoRequest request);
        Task<List<MercadoPedidoDto>> ListarPedidosAsync(int empresaId);
        Task<MercadoPedidoDto?> CambiarEstadoPedidoAsync(int pedidoId, MercadoPedidoEstadoRequest request);

        Task<MercadoPedidoPublicoDto?> ObtenerPedidoPublicoAsync(string token);
        Task<MercadoPedidoPublicoDto> ResponderPedidoPublicoAsync(string token, bool aceptar, MercadoPedidoRespuestaRequest request);
        Task<bool> RegistrarBajaAsync(string token);
    }
}
