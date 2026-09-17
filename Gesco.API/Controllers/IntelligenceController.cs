using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gesco.Application.Services;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("uploads")]
    public class IntelligenceController : ControllerBase
    {
        private readonly DataIntelligenceService _intelligenceService;

        public IntelligenceController(DataIntelligenceService intelligenceService)
        {
            _intelligenceService = intelligenceService;
        }

        private static bool IsSafeFilePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            if (filePath.Contains("..", StringComparison.Ordinal)) return false;
            // Solo rutas locales absolutas o relativas sin traversal
            try
            {
                var full = Path.GetFullPath(filePath);
                return System.IO.File.Exists(full) || Directory.Exists(full) || System.IO.File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("scan-binary")]
        public async Task<IActionResult> ScanBinary(string filePath)
        {
            if (!IsSafeFilePath(filePath)) return BadRequest("Ruta de archivo no válida.");
            
            try 
            {
                var tablas = await _intelligenceService.EscanearEstructuraBinaria(filePath);
                return Ok(new { 
                    mensaje = "Escaneo completado exitosamente", 
                    conteo = tablas.Count,
                    tablasDetectadas = tablas 
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error al escanear el binario: {ex.Message}");
            }
        }

        [HttpGet("preview-table")]
        public async Task<IActionResult> PreviewTable(string filePath, string tableName)
        {
            if (!IsSafeFilePath(filePath) || string.IsNullOrEmpty(tableName)) 
                return BadRequest("Ruta de archivo y nombre de tabla son requeridos.");
            
            try 
            {
                var preview = await _intelligenceService.ObtenerVistaPreviaTabla(filePath, tableName);
                return Ok(preview);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error al obtener vista previa de la tabla: {ex.Message}");
            }
        }

        [HttpPost("ejecutar-migracion")]
        public async Task<IActionResult> EjecutarMigracion([FromBody] MigracionRequest request)
        {
            if (string.IsNullOrEmpty(request.NombreEmpresa)) return BadRequest("Nombre de empresa requerido.");
            if (string.IsNullOrEmpty(request.FilePath)) return BadRequest("Ruta de archivo requerida.");
            if (request.Tablas == null || request.Tablas.Count == 0) return BadRequest("Debe seleccionar al menos una tabla para migrar.");

            try 
            {
                // Ejecuta la migración dinámica de las tablas seleccionadas por el usuario, creando las tablas y migrando el contenido real
                int empresaId = await _intelligenceService.EjecutarMigracionSeleccionada(request.NombreEmpresa, request.FilePath, request.Tablas);
                
                return Ok(new { 
                    mensaje = "Migración finalizada con éxito", 
                    empresaId = empresaId, 
                    nombre = request.NombreEmpresa
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error en la migración: {ex.Message}");
            }
        }

        [HttpGet("scan-relations")]
        public async Task<IActionResult> ScanRelations(string filePath)
        {
            if (!IsSafeFilePath(filePath)) return BadRequest("Ruta de archivo no válida.");
            
            try 
            {
                var result = await _intelligenceService.EscanearRelacionesYTablas(filePath);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error al escanear relaciones: {ex.Message}");
            }
        }

        [HttpPost("ejecutar-migracion-mapeada")]
        public async Task<IActionResult> EjecutarMigracionMapeada([FromBody] MigracionRequest request)
        {
            if (string.IsNullOrEmpty(request.NombreEmpresa)) return BadRequest("Nombre de empresa requerido.");
            if (!IsSafeFilePath(request.FilePath)) return BadRequest("Ruta de archivo no válida.");
            if (request.Tablas == null || request.Tablas.Count == 0) return BadRequest("Debe seleccionar al menos una tabla para migrar.");

            try 
            {
                // Ejecuta la migración relacional mapeada directamente a las tablas Core de GESCO
                int empresaId = await _intelligenceService.EjecutarMigracionRelacionalMapeada(request.NombreEmpresa, request.FilePath, request.Tablas, request.TargetEmpresaId);
                
                return Ok(new { 
                    mensaje = "Migración Core completada y mapeada con éxito", 
                    empresaId = empresaId, 
                    nombre = request.NombreEmpresa
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error en la migración mapeada: {ex.Message}");
            }
        }
    }

    public class MigracionRequest
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public List<string> Tablas { get; set; } = new List<string>();
        public int? TargetEmpresaId { get; set; }
    }
}
