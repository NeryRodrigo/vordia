using Gesco.Application.Services.Conta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>VORDIA CONTA · Estudio contable: clientes (RUC), comprobantes, libros, IVA, vencimientos y RG90.</summary>
    [ApiController]
    [Route("api/Conta")]
    public class ContaController : ControllerBase
    {
        private const long MaxArchivo = 5 * 1024 * 1024;
        private readonly ContaService _svc;

        public ContaController(ContaService svc) => _svc = svc;

        public class QrRequest { public string Texto { get; set; } = ""; public int? ClienteId { get; set; } }

        private string? Usuario => User.FindFirstValue(ClaimTypes.Name);

        private IActionResult Error(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(new { mensaje = "No encontrado" }),
            InvalidOperationException => BadRequest(new { mensaje = ex.Message }),
            _ => throw ex
        };

        private static async Task<List<ArchivoSubido>> LeerArchivosAsync(IFormFileCollection archivos)
        {
            var lista = new List<ArchivoSubido>();
            foreach (var f in archivos.Take(100))
            {
                if (f.Length == 0 || f.Length > MaxArchivo) continue;
                if (!(f.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))) continue;
                using var ms = new MemoryStream();
                await f.CopyToAsync(ms);
                lista.Add(new ArchivoSubido(Path.GetFileName(f.FileName), ms.ToArray()));
            }
            return lista;
        }

        [HttpGet("clientes")]
        public async Task<IActionResult> Clientes([FromQuery] int empresaId) => Ok(await _svc.ClientesAsync(empresaId));

        [HttpPost("clientes")]
        public async Task<IActionResult> CrearCliente([FromQuery] int empresaId, [FromBody] ContaClienteRequest r)
        {
            try { return Ok(await _svc.GuardarClienteAsync(empresaId, r)); } catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Error(ex); }
        }

        [HttpPut("clientes/{clienteId:int}")]
        public async Task<IActionResult> EditarCliente(int clienteId, [FromQuery] int empresaId, [FromBody] ContaClienteRequest r)
        {
            try { return Ok(await _svc.GuardarClienteAsync(empresaId, r, clienteId)); } catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Error(ex); }
        }

        [HttpGet("panel")]
        public async Task<IActionResult> Panel([FromQuery] int empresaId, [FromQuery] int? periodo) =>
            Ok(new
            {
                periodo = periodo ?? ContaService.PeriodoActualDeclarar(DateTime.UtcNow.AddHours(-3)),
                clientes = await _svc.PanelAsync(empresaId, periodo ?? ContaService.PeriodoActualDeclarar(DateTime.UtcNow.AddHours(-3)))
            });

        [HttpGet("comprobantes")]
        public async Task<IActionResult> Comprobantes([FromQuery] int empresaId, [FromQuery] int? clienteId, [FromQuery] int? periodo, [FromQuery] string? estado, [FromQuery] string? tipo) =>
            Ok(await _svc.ComprobantesAsync(empresaId, clienteId, periodo, estado, tipo));

        [HttpPost("xml")]
        [RequestSizeLimit(60 * 1024 * 1024)]
        public async Task<IActionResult> Xml([FromQuery] int empresaId, [FromForm] IFormFileCollection archivos)
        {
            var lista = await LeerArchivosAsync(archivos);
            if (lista.Count == 0) return BadRequest(new { mensaje = "Subí archivos XML del SIFEN o un ZIP con XML (hasta 5 MB cada uno)" });
            return Ok(await _svc.ImportarXmlAsync(empresaId, lista, Usuario));
        }

        [HttpPost("qr")]
        public async Task<IActionResult> Qr([FromQuery] int empresaId, [FromBody] QrRequest r) => Ok(await _svc.ImportarQrAsync(empresaId, r.Texto, Usuario));

        [HttpPost("comprobantes")]
        public async Task<IActionResult> Crear([FromQuery] int empresaId, [FromBody] ContaComprobanteRequest r)
        {
            try { return Ok(await _svc.CrearManualAsync(empresaId, r, Usuario)); } catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Error(ex); }
        }

        [HttpPut("comprobantes/{comprobanteId:int}")]
        public async Task<IActionResult> Editar(int comprobanteId, [FromQuery] int empresaId, [FromBody] ContaComprobanteRequest r)
        {
            try { return await _svc.EditarAsync(empresaId, comprobanteId, r) is { } x ? Ok(x) : NotFound(); }
            catch (Exception ex) when (ex is InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("comprobantes/{comprobanteId:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int comprobanteId, [FromQuery] int empresaId) =>
            await _svc.AprobarAsync(empresaId, comprobanteId) is { } x ? Ok(x) : NotFound();

        [HttpDelete("comprobantes/{comprobanteId:int}")]
        public async Task<IActionResult> Eliminar(int comprobanteId, [FromQuery] int empresaId) =>
            await _svc.EliminarAsync(empresaId, comprobanteId) ? NoContent() : NotFound();

        [HttpGet("rg90")]
        public async Task<IActionResult> Rg90([FromQuery] int empresaId, [FromQuery] int clienteId, [FromQuery] int periodo)
        {
            try
            {
                var (archivo, zip, filas, electronicos, sinAprobar) = await _svc.ExportarRg90Async(empresaId, clienteId, periodo, Usuario);
                Response.Headers["X-Conta-Filas"] = filas.ToString();
                Response.Headers["X-Conta-Electronicos"] = electronicos.ToString();
                Response.Headers["X-Conta-SinAprobar"] = sinAprobar.ToString();
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition, X-Conta-Filas, X-Conta-Electronicos, X-Conta-SinAprobar";
                return File(zip, "application/zip", $"{archivo}.zip");
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Error(ex); }
        }

        [HttpGet("libro")]
        public async Task<IActionResult> Libro([FromQuery] int empresaId, [FromQuery] int clienteId, [FromQuery] int periodo, [FromQuery] string tipo = "Compra")
        {
            try
            {
                var (archivo, csv) = await _svc.LibroCsvAsync(empresaId, clienteId, periodo, tipo == "Venta" ? "Venta" : "Compra");
                Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
                return File(csv, "text/csv; charset=utf-8", archivo);
            }
            catch (Exception ex) when (ex is KeyNotFoundException) { return Error(ex); }
        }

        [HttpPost("demo")]
        public async Task<IActionResult> Demo([FromQuery] int empresaId)
        {
            try { return Ok(new { comprobantes = await _svc.CargarDemoAsync(empresaId) }); }
            catch (Exception ex) when (ex is InvalidOperationException) { return Error(ex); }
        }

        // ───────────── Portal del cliente: link privado, sin cuenta ─────────────

        [HttpGet("/api/public/conta/{token}")]
        [AllowAnonymous]
        [EnableRateLimiting("portal")]
        public async Task<IActionResult> Portal(string token)
        {
            if (await _svc.PortalAsync(token) is not { } p) return NotFound(new { mensaje = "El link no es válido. Pedile uno nuevo a tu contador." });
            return Ok(new { cliente = p.cliente.RazonSocial, ruc = $"{p.cliente.Ruc}-{p.cliente.Dv}", estudio = p.estudio, recibidosMes = await _svc.PortalRecibidosMesAsync(p.cliente.ClienteId) });
        }

        [HttpPost("/api/public/conta/{token}/qr")]
        [AllowAnonymous]
        [EnableRateLimiting("portal")]
        public async Task<IActionResult> PortalQr(string token, [FromBody] QrRequest r)
        {
            if (await _svc.PortalAsync(token) is not { } p) return NotFound(new { mensaje = "El link no es válido" });
            return Ok(await _svc.ImportarQrAsync(p.cliente.EmpresaId, r.Texto, $"Portal · {p.cliente.RazonSocial}", p.cliente, "Portal"));
        }

        [HttpPost("/api/public/conta/{token}/xml")]
        [AllowAnonymous]
        [EnableRateLimiting("portal")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> PortalXml(string token, [FromForm] IFormFileCollection archivos)
        {
            if (await _svc.PortalAsync(token) is not { } p) return NotFound(new { mensaje = "El link no es válido" });
            var lista = await LeerArchivosAsync(archivos);
            if (lista.Count == 0) return BadRequest(new { mensaje = "Subí el XML de la factura electrónica" });
            return Ok(await _svc.ImportarXmlAsync(p.cliente.EmpresaId, lista, $"Portal · {p.cliente.RazonSocial}", p.cliente, "Portal"));
        }
    }
}
