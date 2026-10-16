using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// Link público de un pedido de Vordia (Necesidades del Mercado): el cliente lo abre desde
    /// WhatsApp o correo, ve el producto y el precio, y lo acepta sin crear cuenta.
    /// El token es aleatorio de 128 bits y no expone datos internos.
    /// </summary>
    [ApiController]
    [Route("api/public/pedidos")]
    [AllowAnonymous]
    [EnableRateLimiting("leads")]
    public class PedidoPublicoController : ControllerBase
    {
        private readonly IMercadoVentasService _ventas;

        public PedidoPublicoController(IMercadoVentasService ventas)
        {
            _ventas = ventas;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Obtener(string token)
        {
            var p = await _ventas.ObtenerPedidoPublicoAsync(token);
            return p == null ? NotFound(new { mensaje = "Pedido no encontrado" }) : Ok(p);
        }

        [HttpPost("{token}/aceptar")]
        public Task<IActionResult> Aceptar(string token, [FromBody] MercadoPedidoRespuestaRequest request) => Responder(token, true, request);

        [HttpPost("{token}/rechazar")]
        public Task<IActionResult> Rechazar(string token, [FromBody] MercadoPedidoRespuestaRequest request) => Responder(token, false, request);

        private async Task<IActionResult> Responder(string token, bool aceptar, MercadoPedidoRespuestaRequest request)
        {
            // Honeypot: solo un bot completa este campo
            if (!string.IsNullOrWhiteSpace(request.Sitio)) return Ok(new { mensaje = "Recibido" });
            try { return Ok(await _ventas.ResponderPedidoPublicoAsync(token, aceptar, request)); }
            catch (KeyNotFoundException) { return NotFound(new { mensaje = "Pedido no encontrado" }); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }
    }
}
