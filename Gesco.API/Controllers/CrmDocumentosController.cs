using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// Repositorio de documentos de Vordia HQ. Los archivos se guardan fuera de wwwroot
    /// a propósito: así no quedan accesibles por URL pública y solo se pueden descargar
    /// con sesión válida a través de este controlador.
    /// </summary>
    [Route("api/Crm")]
    [ApiController]
    public class CrmDocumentosController : ControllerBase
    {
        private const long LimiteBytes = 20 * 1024 * 1024;

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx",
            ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip"
        };

        private readonly ICrmDocumentosService _documentos;
        private readonly ILogger<CrmDocumentosController> _logger;
        private readonly string _almacen;

        public CrmDocumentosController(
            ICrmDocumentosService documentos,
            IConfiguration config,
            ILogger<CrmDocumentosController> logger)
        {
            _documentos = documentos;
            _logger = logger;
            _almacen = config["Storage:CrmDocumentos"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "almacen-crm", "documentos");

            if (!Directory.Exists(_almacen)) Directory.CreateDirectory(_almacen);
        }

        [HttpGet("documentos")]
        public async Task<IActionResult> Listar(
            [FromQuery] int empresaId,
            [FromQuery] int? clienteId = null,
            [FromQuery] string? categoria = null,
            [FromQuery] string? q = null,
            [FromQuery] bool porVencer = false)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _documentos.ListarAsync(empresaId, clienteId, categoria, q, porVencer));
        }

        [HttpGet("documentos/resumen")]
        public async Task<IActionResult> Resumen([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            return Ok(await _documentos.ResumenAsync(empresaId));
        }

        [HttpGet("documentos/categorias")]
        public IActionResult Categorias() => Ok(_documentos.Categorias);

        [HttpPost("documentos/upload")]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(LimiteBytes)]
        public async Task<IActionResult> Subir(
            [FromForm] int empresaId,
            [FromForm] string? titulo,
            [FromForm] string? categoria,
            [FromForm] int? clienteId,
            [FromForm] DateTime? fechaVencimiento,
            [FromForm] string? notas,
            [FromForm] string? subidoPor,
            IFormFile? archivo)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            if (archivo == null || archivo.Length == 0) return BadRequest(new { mensaje = "No llegó ningún archivo." });
            if (archivo.Length > LimiteBytes) return BadRequest(new { mensaje = "El archivo supera el límite de 20 MB." });

            var extension = Path.GetExtension(archivo.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !ExtensionesPermitidas.Contains(extension))
                return BadRequest(new { mensaje = $"Tipo de archivo no permitido ({extension}). Se aceptan PDF, imágenes, Office, texto y ZIP." });

            var nombreEnDisco = $"{empresaId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaFisica = Path.Combine(_almacen, nombreEnDisco);

            try
            {
                await using var stream = new FileStream(rutaFisica, FileMode.Create);
                await archivo.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar el documento del CRM en {Ruta}", rutaFisica);
                return StatusCode(500, new { mensaje = "No se pudo guardar el archivo en el servidor." });
            }

            try
            {
                var dto = await _documentos.CrearConArchivoAsync(new CrmDocumentoArchivoRequest
                {
                    EmpresaId = empresaId,
                    ClienteId = clienteId,
                    Titulo = string.IsNullOrWhiteSpace(titulo) ? Path.GetFileNameWithoutExtension(archivo.FileName) : titulo,
                    Categoria = categoria,
                    NombreArchivo = Path.GetFileName(archivo.FileName),
                    Url = $"/crm-documentos/{nombreEnDisco}",
                    Extension = extension.ToLowerInvariant(),
                    TamanoBytes = archivo.Length,
                    FechaVencimiento = fechaVencimiento,
                    Notas = notas,
                    SubidoPor = subidoPor
                });

                return Ok(dto);
            }
            catch (Exception ex)
            {
                // Si la ficha no se pudo guardar, el archivo huérfano no aporta nada
                TryEliminarArchivo(rutaFisica);
                _logger.LogError(ex, "No se pudo registrar el documento del CRM");
                return StatusCode(500, new { mensaje = "El archivo se subió pero no se pudo registrar. Intentá de nuevo." });
            }
        }

        [HttpPost("documentos/enlace")]
        public async Task<IActionResult> CrearEnlace([FromBody] CrmDocumentoEnlaceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { mensaje = "Revisá los datos del formulario." });

            try
            {
                return Ok(await _documentos.CrearEnlaceAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("documentos/{documentoId:int}")]
        public async Task<IActionResult> Actualizar(int documentoId, [FromBody] CrmDocumentoMetadatosRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { mensaje = "Revisá los datos del formulario." });

            var dto = await _documentos.ActualizarMetadatosAsync(documentoId, request);
            if (dto == null) return NotFound(new { mensaje = "El documento no existe." });
            return Ok(dto);
        }

        [HttpDelete("documentos/{documentoId:int}")]
        public async Task<IActionResult> Eliminar(int documentoId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var ok = await _documentos.EliminarAsync(documentoId, empresaId);
            if (!ok) return NotFound(new { mensaje = "El documento no existe." });
            return NoContent();
        }

        /// <summary>Devuelve el binario. Los enlaces externos no pasan por acá.</summary>
        [HttpGet("documentos/{documentoId:int}/archivo")]
        public async Task<IActionResult> Descargar(int documentoId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var doc = await _documentos.ObtenerAsync(documentoId, empresaId);
            if (doc == null) return NotFound(new { mensaje = "El documento no existe." });
            if (doc.EsEnlaceExterno) return BadRequest(new { mensaje = "Este documento es un enlace externo." });

            // Solo el nombre del archivo: nunca se arma la ruta con datos crudos de la URL
            var nombreEnDisco = Path.GetFileName(doc.Url);
            var rutaFisica = Path.Combine(_almacen, nombreEnDisco);

            if (!System.IO.File.Exists(rutaFisica))
                return NotFound(new { mensaje = "La ficha existe pero el archivo no está en el servidor." });

            new FileExtensionContentTypeProvider().TryGetContentType(nombreEnDisco, out var contentType);
            var descarga = string.IsNullOrWhiteSpace(doc.NombreArchivo) ? nombreEnDisco : doc.NombreArchivo;

            return PhysicalFile(rutaFisica, contentType ?? "application/octet-stream", descarga);
        }

        private void TryEliminarArchivo(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo borrar el archivo huérfano {Ruta}", ruta);
            }
        }
    }
}
