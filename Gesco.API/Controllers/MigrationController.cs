using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gesco.Application.Interfaces;
using Gesco.Application.DTOs;
using Gesco.Domain.Entities;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly IMigrationService _migrationService;
        private readonly ICloudExtractionService _cloudExtractionService;
        private readonly IAiMappingService _aiMappingService;

        public MigrationController(
            IMigrationService migrationService, 
            ICloudExtractionService cloudExtractionService,
            IAiMappingService aiMappingService)
        {
            _migrationService = migrationService;
            _cloudExtractionService = cloudExtractionService;
            _aiMappingService = aiMappingService;
        }

        [HttpGet("personas/{empresaId}")]
        public async Task<IActionResult> GetPersonas(int empresaId)
        {
            var personas = await _migrationService.ObtenerPersonas(empresaId);
            return Ok(personas);
        }

        [HttpGet("agentes/{empresaId}")]
        public async Task<IActionResult> GetAgentes(int empresaId)
        {
            var agentes = await _migrationService.ObtenerAgentes(empresaId);
            return Ok(agentes);
        }

        [HttpPost("upload-excel")]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> UploadExcel(IFormFile file, [FromQuery] string tablaDestino, int empresaId)
        {
            // 1. Validaciones básicas
            if (file == null || file.Length == 0)
                return BadRequest("Por favor, subí un archivo Excel válido.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
                return BadRequest("Solo se permiten archivos .xlsx (Excel moderno).");

            try
            {
                // 2. Usamos el Stream del archivo sin guardarlo en el disco (más rápido y seguro)
                using var stream = file.OpenReadStream();

                // 3. Llamamos a tu servicio estrella
                int registrosProcesados = await _migrationService.MigrarDatosDesdeExcel(stream, tablaDestino, empresaId);

                return Ok(new
                {
                    Success = true,
                    Message = $"¡Gesco! Se procesaron {registrosProcesados} registros en la tabla {tablaDestino}."
                });
            }
            catch (Exception ex)
            {
                //return StatusCode(500, $"Error interno: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Error interno al procesar el Excel",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("upload-condominio")]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> UploadCondominio(IFormFile file, [FromQuery] int empresaId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Por favor, subí un archivo Excel válido.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
                return BadRequest("Solo se permiten archivos .xlsx.");

            try
            {
                using var stream = file.OpenReadStream();
                int registrosProcesados = await _migrationService.MigrarCondominioExcel(stream, empresaId);

                return Ok(new
                {
                    Success = true,
                    Message = $"¡Gesco! Se migraron {registrosProcesados} registros del condominio con éxito."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error interno al procesar el Excel de Condominio",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("upload-contratos")]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(ValueLengthLimit = 50 * 1024 * 1024, MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        public async Task<IActionResult> UploadContratos(List<IFormFile> files, [FromQuery] int empresaId)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Por favor, subí al menos un archivo PDF válido.");

            var pdfs = files.Where(f => Path.GetExtension(f.FileName).ToLower() == ".pdf").ToList();
            if (pdfs.Count == 0)
                return BadRequest("Solo se permiten archivos .pdf.");

            try
            {
                int registrosProcesados = await _migrationService.MigrarContratosPdf(pdfs, empresaId);

                return Ok(new
                {
                    Success = true,
                    Message = $"¡Gesco! Se extrajeron datos de {registrosProcesados} contratos (PDFs) con éxito y se autocompletaron las fichas."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error interno al procesar los PDFs de Contratos",
                    detalle = ex.Message
                });
            }
        }

        [HttpGet("siguiente-nro/{empresaId}")]
        public async Task<IActionResult> GetSiguienteNro(int empresaId)
        {
            var nro = await _migrationService.ObtenerSiguienteNroFactura(empresaId);
            return Ok(new { siguienteNro = nro });
        }

        [HttpGet("ventas/{empresaId}")]
        public async Task<IActionResult> GetVentas(int empresaId)
        {
            var ventas = await _migrationService.ObtenerVentas(empresaId);
            return Ok(ventas);
        }

        [HttpGet("factura-completa/{id}")]
        public async Task<IActionResult> GetFacturaCompleta(int id)
        {
            var factura = await _migrationService.ObtenerFacturaCompleta(id);

            if (factura == null)
                return NotFound(new { Message = "La factura no existe." });

            return Ok(factura);
        }

        [HttpGet("catalogos/{empresaId}")]
        public async Task<IActionResult> GetCatalogos(int empresaId)
        {
            var catalogos = await _migrationService.ObtenerCatalogos(empresaId);
            return Ok(catalogos);
        }

        [HttpGet("analisis/{empresaId}")]
        public async Task<IActionResult> GetAnalisis(int empresaId)
        {
            var lista = await _migrationService.ObtenerAnalisisMercado(empresaId);
            return Ok(lista);
        }

        [HttpPost("guardar-propiedad")]
        public async Task<IActionResult> PostPropiedad([FromBody] Propiedad prop)
        {
            var id = await _migrationService.GuardarPropiedad(prop);
            return Ok(new { Success = true, Id = id });
        }

        [HttpGet("propiedades/{empresaId}")]
        public async Task<IActionResult> GetPropiedades(int empresaId)
        {
            var lista = await _migrationService.ObtenerPropiedades(empresaId);
            return Ok(lista);
        }

        [HttpGet("referencias/{analisisId}")]
        public async Task<IActionResult> GetReferencias(int analisisId)
        {
            var referencias = await _migrationService.ObtenerReferenciasPorAnalisis(analisisId);
            return Ok(referencias);
        }

        [HttpPost("upload-inteligente")]
        public async Task<IActionResult> UploadInteligente(IFormFile file, [FromQuery] int empresaId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Archivo no válido" });

            try
            {
                using var stream = file.OpenReadStream();

                int procesados = await _migrationService.MigrarInteligente(
                    stream,
                    file.FileName,
                    empresaId
                );

                return Ok(new
                {
                    message = $"¡Gesco! Archivo '{file.FileName}' procesado.",
                    registros = procesados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error en la migración inteligente",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("guardar-factura")]
        public async Task<IActionResult> GuardarFactura([FromBody] VentaCabecera factura)
        {
            var id = await _migrationService.GuardarFacturaManual(factura);
            return Ok(new { Success = true, Id = id });
        }

        [HttpPost("pause-sync")]
        public async Task<IActionResult> Pause(int empresaId)
        {
            await _migrationService.PausarSync(empresaId);
            return Ok();
        }

        [HttpGet("sync-status")]
        public async Task<IActionResult> GetSyncStatus(int empresaId)
        {
            var status = await _migrationService.ObtenerSyncStatus(empresaId);
            return Ok(status);
        }

        [HttpPost("scrapping-masivo")]
        public async Task<IActionResult> ScrappingMasivo(int empresaId, [FromQuery] string? urlManual = null)
        {
            /*try
            {
                // Llamamos al proceso de inteligencia
                //int total = await _migrationService.ScrappingMasivoRemax(empresaId);
                int total = await _migrationService.ScrappingMasivoRemax(empresaId);

                return Ok(new { registros = total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en el motor de búsqueda: " + ex.Message });
            }*/
            try
            {
                var totalInsertados = await _migrationService.ScrappingMasivoRemax(empresaId, urlManual);

                return Ok(new
                {
                    empresaId,
                    registros = totalInsertados.Total,
                    estado = totalInsertados.Estado
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new
                {
                    mensaje = "Scrapping cancelado por el usuario"
                });
            }
            catch (Exception ex)
            {
                // ⚠️ nunca devuelvas ex completo en prod
                return StatusCode(500, new
                {
                    mensaje = "Error en el motor de búsqueda",
                    detalle = ex.Message
                });
            }
        }

        [HttpPost("cloud-scraping")]
        public async Task<IActionResult> CloudScraping([FromBody] CloudExtractionRequest request)
        {
            if (string.IsNullOrEmpty(request.LoginUrl) || string.IsNullOrEmpty(request.DataUrl))
                return BadRequest("LoginUrl y DataUrl son obligatorios.");

            try
            {
                string jsonResult = await _cloudExtractionService.ExtraerDatosJsonNubeAsync(
                    request.LoginUrl,
                    request.DataUrl,
                    request.Username,
                    request.Password
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Datos extraídos de la nube exitosamente.",
                    Data = jsonResult
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error en la extracción RPA de la nube.",
                    Detail = ex.Message
                });
            }
        }

        [HttpPost("migrar-universal")]
        public async Task<IActionResult> MigrarUniversal([FromBody] CloudExtractionRequest request, [FromQuery] string tablaDestino = "Personas")
        {
            try
            {
                // Paso 1: Usar RPA para extraer los datos crudos del sistema origen
                string rawJson = await _cloudExtractionService.ExtraerDatosJsonNubeAsync(
                    request.LoginUrl,
                    request.DataUrl,
                    request.Username,
                    request.Password
                );

                if (rawJson.Contains("\"error\""))
                {
                    return BadRequest("No se pudieron extraer datos del origen.");
                }

                // Paso 2: Usar Inteligencia Artificial para entender el esquema origen y mapearlo a GESCO
                var diccionarioMapeo = await _aiMappingService.GenerarMapeoSemanticoAsync(rawJson, tablaDestino);

                // Paso 3: (Simulación) Aquí el backend usaría 'diccionarioMapeo' para hacer el Bulk Insert dinámico.
                // Retornamos la respuesta mágica para demostrar la innovación:
                return Ok(new
                {
                    Success = true,
                    Message = "Migración Universal iniciada con éxito.",
                    MapeoGeneradoPorIA = diccionarioMapeo,
                    MuestraDatosExtraidos = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        [HttpPost("crear-captacion-manual")]
        public async Task<IActionResult> CrearCaptacionManual([FromBody] Propiedad propInput)
        {
            if (propInput == null) return BadRequest("Datos inválidos.");

            var resultado = await _migrationService.CrearCaptacionManual(propInput);

            if (resultado == null)
                return StatusCode(500, "Error interno al guardar la captación.");

            return Ok(new { message = "Captación generada y enviada al Bróker.", id = resultado.PropiedadId });
        }

        [HttpPut("aprobar-propiedad/{id}")]
        public async Task<IActionResult> AprobarPropiedad(int id)
        {
            if (id <= 0) return BadRequest("ID de propiedad inválido.");

            var exito = await _migrationService.AprobarPropiedad(id);

            if (!exito)
                return NotFound("No se encontró la propiedad o no pudo ser aprobada.");

            return Ok(new { message = "Propiedad aprobada con éxito." });
        }

        [HttpPut("actualizar-propiedad")]
        public async Task<IActionResult> ActualizarPropiedad([FromBody] Propiedad propInput)
        {
            if (propInput == null || propInput.PropiedadId <= 0)
                return BadRequest("Datos de propiedad inválidos.");

            var resultado = await _migrationService.ActualizarPropiedad(propInput);

            if (!resultado)
                return NotFound("No se encontró la propiedad para actualizar.");

            return Ok(new { message = "Propiedad actualizada con éxito" });
        }

        [HttpDelete("propiedad/{id}")]
        public async Task<IActionResult> DeletePropiedad(int id)
        {
            var success = await _migrationService.EliminarPropiedad(id);
            if (!success) return NotFound("No se pudo eliminar");
            return Ok(new { Success = true });
        }

        [HttpPost("analisis-manual")]
        public async Task<IActionResult> CrearAnalisisManual([FromBody] AnalisisMercado input, [FromQuery] int empresaId)
        {
            try
            {
                if (input == null)
                {
                    return BadRequest("Los datos del análisis no pueden ser nulos.");
                }

                // Asignamos el EmpresaId que viene del frontend
                input.EmpresaId = empresaId;

                // Llamamos al servicio que creaste
                int nuevoId = await _migrationService.CrearAnalisisManual(input);

                // Retornamos el ID al frontend
                return Ok(nuevoId);
            }
            catch (Exception ex)
            {
                // Loguear el error si tienes un logger
                return StatusCode(500, $"Error interno al crear el análisis: {ex.Message}");
            }
        }

        [HttpPut("analisis/{id}")]
        public async Task<IActionResult> ActualizarAnalisis(int id, [FromBody] AnalisisMercado input)
        {
            try
            {
                // 1. Validación básica
                if (input == null)
                {
                    return BadRequest("Los datos del análisis no pueden estar vacíos.");
                }

                // 2. Llamada al método de tu Service
                var resultado = await _migrationService.ActualizarAnalisis(id, input);

                // 3. Si el service devuelve false, significa que el registro no existe
                if (!resultado)
                {
                    return NotFound($"No se encontró el análisis con ID {id} para actualizar.");
                }

                // 4. Retorno exitoso
                return Ok(new { message = "Análisis y cálculos técnicos actualizados correctamente." });
            }
            catch (Exception ex)
            {
                // Loggear el error interno
                return StatusCode(500, $"Error interno al procesar la actualización: {ex.Message}");
            }
        }


        [HttpDelete("analisis/{id}")]
        public async Task<IActionResult> DeleteAnalisis(int id)
        {
            // Para borrar un ACM de la tabla
            await _migrationService.EliminarAnalisis(id);
            return Ok(new { Success = true });
        }
    }
}
