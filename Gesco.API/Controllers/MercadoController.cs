using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// VORDIA HQ — Necesidades del Mercado: pedidos públicos de sistemas y aplicaciones
    /// convertidos en prospectos del CRM y en productos rápidos del Roadmap.
    /// </summary>
    [ApiController]
    [Route("api/Mercado")]
    public class MercadoController : ControllerBase
    {
        private readonly IMercadoService _mercado;
        private readonly IMercadoVentasService _ventas;

        public MercadoController(IMercadoService mercado, IMercadoVentasService ventas)
        {
            _mercado = mercado;
            _ventas = ventas;
        }

        /* ───────────── Contacto con el cliente ───────────── */

        [HttpPut("necesidades/{necesidadId:int}/contacto")]
        public async Task<IActionResult> GuardarContacto(int necesidadId, [FromBody] MercadoContactoInfoRequest request)
        {
            var n = await _ventas.GuardarContactoAsync(necesidadId, request);
            return n == null ? NotFound() : Ok(n);
        }

        [HttpGet("necesidades/{necesidadId:int}/contactos")]
        public async Task<IActionResult> Contactos(int necesidadId, [FromQuery] int empresaId) =>
            Ok(await _ventas.ListarContactosAsync(necesidadId, empresaId));

        /// <summary>Registra un contacto hecho por WhatsApp, plataforma o llamada.</summary>
        [HttpPost("necesidades/{necesidadId:int}/contactos")]
        public async Task<IActionResult> RegistrarContacto(int necesidadId, [FromBody] MercadoContactoRegistroRequest request)
        {
            try { return Ok(await _ventas.RegistrarContactoAsync(necesidadId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("necesidades/{necesidadId:int}/correo")]
        public async Task<IActionResult> EnviarCorreo(int necesidadId, [FromBody] MercadoCorreoRequest request)
        {
            try
            {
                var c = await _ventas.EnviarCorreoAsync(necesidadId, request);
                return c.Resultado == "Enviado" ? Ok(c) : StatusCode(502, new { mensaje = c.Detalle ?? "No se pudo enviar el correo", contacto = c });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        /* ───────────── Pedidos rápidos ───────────── */

        [HttpGet("pedidos")]
        public async Task<IActionResult> Pedidos([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _ventas.ListarPedidosAsync(empresaId));
        }

        [HttpPost("pedidos")]
        public async Task<IActionResult> CrearPedido([FromBody] MercadoPedidoRequest request)
        {
            try { return Ok(await _ventas.CrearPedidoAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPatch("pedidos/{pedidoId:int}/estado")]
        public async Task<IActionResult> EstadoPedido(int pedidoId, [FromBody] MercadoPedidoEstadoRequest request)
        {
            try
            {
                var p = await _ventas.CambiarEstadoPedidoAsync(pedidoId, request);
                return p == null ? NotFound() : Ok(p);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("necesidades")]
        public async Task<IActionResult> Necesidades(
            [FromQuery] int empresaId, [FromQuery] string? estado = null, [FromQuery] string? vertical = null,
            [FromQuery] string? fuente = null, [FromQuery] int? minPuntaje = null, [FromQuery] string? q = null, [FromQuery] int? dias = null,
            [FromQuery] string? alcance = null, [FromQuery] int? minConfianza = null, [FromQuery] bool conContacto = false)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _mercado.ListarAsync(empresaId, estado, vertical, fuente, minPuntaje, q, dias, alcance, minConfianza, conContacto));
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> Resumen([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _mercado.ResumenAsync(empresaId));
        }

        /// <summary>Busca ahora en todas las fuentes activas.</summary>
        [HttpPost("sincronizar")]
        public async Task<IActionResult> Sincronizar([FromQuery] int empresaId, CancellationToken ct)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _mercado.SincronizarAsync(empresaId, ct));
        }

        [HttpPost("necesidades")]
        public async Task<IActionResult> CrearManual([FromBody] MercadoManualRequest request)
        {
            try { return Ok(await _mercado.CrearManualAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPatch("necesidades/{necesidadId:int}")]
        public async Task<IActionResult> Actualizar(int necesidadId, [FromBody] MercadoActualizarRequest request)
        {
            try
            {
                var n = await _mercado.ActualizarAsync(necesidadId, request);
                return n == null ? NotFound() : Ok(n);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("necesidades/{necesidadId:int}/crm")]
        public async Task<IActionResult> ACrm(int necesidadId, [FromBody] MercadoConversionRequest request)
        {
            try { return Ok(await _mercado.ConvertirACrmAsync(necesidadId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("necesidades/{necesidadId:int}/producto")]
        public async Task<IActionResult> AProducto(int necesidadId, [FromBody] MercadoConversionRequest request)
        {
            try { return Ok(await _mercado.ConvertirAProductoAsync(necesidadId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("oportunidades/producto")]
        public async Task<IActionResult> OportunidadAProducto([FromBody] MercadoOportunidadProductoRequest request)
        {
            try { return Ok(await _mercado.OportunidadAProductoAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("fuentes")]
        public async Task<IActionResult> Fuentes([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _mercado.ListarFuentesAsync(empresaId));
        }

        [HttpPost("fuentes")]
        public async Task<IActionResult> CrearFuente([FromBody] MercadoFuenteRequest request)
        {
            try { return Ok(await _mercado.GuardarFuenteAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPut("fuentes/{fuenteId:int}")]
        public async Task<IActionResult> ActualizarFuente(int fuenteId, [FromBody] MercadoFuenteRequest request)
        {
            try { return Ok(await _mercado.GuardarFuenteAsync(request, fuenteId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpDelete("fuentes/{fuenteId:int}")]
        public async Task<IActionResult> EliminarFuente(int fuenteId, [FromQuery] int empresaId)
        {
            var ok = await _mercado.EliminarFuenteAsync(fuenteId, empresaId);
            return ok ? NoContent() : NotFound();
        }
    }
}
