using Gesco.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>VORDIA HQ · Secuencias de correo comercial con redacción por IA y aprobación humana.</summary>
    [ApiController]
    [Route("api/Secuencias")]
    public class SecuenciasController : ControllerBase
    {
        private readonly ISecuenciasService _svc;

        public SecuenciasController(ISecuenciasService svc) => _svc = svc;

        public class ImportarRequest { public int EmpresaId { get; set; } public string Origen { get; set; } = "crm"; public int Max { get; set; } = 50; }
        public class RedactarRequest { public int EmpresaId { get; set; } public int Max { get; set; } = 10; }
        public class BorradorRequest { public int EmpresaId { get; set; } public string Asunto { get; set; } = ""; public string Cuerpo { get; set; } = ""; }
        public class EstadoRequest { public int EmpresaId { get; set; } public string Estado { get; set; } = ""; }

        private IActionResult Error(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(new { mensaje = "No encontrado" }),
            InvalidOperationException => BadRequest(new { mensaje = ex.Message }),
            _ => throw ex
        };

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int empresaId) =>
            Ok(new { iaConfigurada = _svc.IaConfigurada, secuencias = await _svc.ListarAsync(empresaId) });

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] SecuenciaRequest r)
        {
            try { return Ok(await _svc.GuardarAsync(r)); } catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
        }

        [HttpPut("{secuenciaId:int}")]
        public async Task<IActionResult> Editar(int secuenciaId, [FromBody] SecuenciaRequest r)
        {
            try { return Ok(await _svc.GuardarAsync(r, secuenciaId)); } catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("{secuenciaId:int}/activar")]
        public async Task<IActionResult> Activar(int secuenciaId, [FromQuery] int empresaId, [FromQuery] bool activa) =>
            await _svc.ActivarAsync(secuenciaId, empresaId, activa) ? Ok(new { activa }) : NotFound();

        [HttpGet("{secuenciaId:int}/contactos")]
        public async Task<IActionResult> Contactos(int secuenciaId, [FromQuery] int empresaId) => Ok(await _svc.ContactosAsync(secuenciaId, empresaId));

        [HttpPost("{secuenciaId:int}/importar")]
        public async Task<IActionResult> Importar(int secuenciaId, [FromBody] ImportarRequest r)
        {
            try { return Ok(new { agregados = await _svc.ImportarAsync(secuenciaId, r.EmpresaId, r.Origen, r.Max) }); }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("{secuenciaId:int}/contactos")]
        public async Task<IActionResult> AgregarManual(int secuenciaId, [FromBody] ContactoManualRequest r)
        {
            try { return Ok(await _svc.AgregarManualAsync(secuenciaId, r)); }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("{secuenciaId:int}/redactar")]
        public async Task<IActionResult> Redactar(int secuenciaId, [FromBody] RedactarRequest r, CancellationToken ct)
        {
            try { return Ok(new { redactados = await _svc.RedactarAsync(secuenciaId, r.EmpresaId, r.Max, ct) }); }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("{secuenciaId:int}/aprobar-todos")]
        public async Task<IActionResult> AprobarTodos(int secuenciaId, [FromQuery] int empresaId) =>
            Ok(new { aprobados = await _svc.AprobarTodosAsync(secuenciaId, empresaId) });

        [HttpPut("contactos/{contactoId:int}")]
        public async Task<IActionResult> EditarBorrador(int contactoId, [FromBody] BorradorRequest r)
        {
            try { return await _svc.EditarBorradorAsync(contactoId, r.EmpresaId, r.Asunto, r.Cuerpo) is { } c ? Ok(c) : NotFound(); }
            catch (Exception ex) when (ex is InvalidOperationException) { return Error(ex); }
        }

        [HttpPost("contactos/{contactoId:int}/estado")]
        public async Task<IActionResult> Estado(int contactoId, [FromBody] EstadoRequest r)
        {
            try { return await _svc.CambiarEstadoAsync(contactoId, r.EmpresaId, r.Estado) is { } c ? Ok(c) : NotFound(); }
            catch (Exception ex) when (ex is InvalidOperationException) { return Error(ex); }
        }

        /// <summary>Envía ahora lo que esté pendiente (respetando horario hábil y límites).</summary>
        [HttpPost("procesar")]
        public async Task<IActionResult> Procesar([FromQuery] int empresaId, CancellationToken ct) => Ok(await _svc.ProcesarAsync(empresaId, ct));
    }
}
