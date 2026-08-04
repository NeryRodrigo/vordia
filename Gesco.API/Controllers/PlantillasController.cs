using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gesco.Domain.Entities;
using Gesco.Application.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Compression;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlantillasController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "plantillas");

        public PlantillasController(IApplicationDbContext context)
        {
            _context = context;
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlantillas()
        {
            var plantillas = await _context.PlantillasDocumento.ToListAsync();

            // Asegurar que las 5 plantillas oficiales estén siempre presentes y apuntando a los archivos correctos
            var officialTemplates = new List<PlantillaDocumento>
            {
                new PlantillaDocumento
                {
                    Nombre = "CONTRATO DE ALQUILER",
                    Descripcion = "Contrato de Alquiler de Dúplex Oficial",
                    Tipo = "PRINCIPAL",
                    ArchivoUrl = "/plantillas/CONTRATO_DE_ALQUILER.docx",
                    VariablesSoportadas = "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {Duplex}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {FechaFirma}, {FechaEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}",
                    FechaActualizacion = DateTime.Now
                },
                new PlantillaDocumento
                {
                    Nombre = "PAGARÉ DE GARANTÍA PROFESIONAL",
                    Descripcion = "Firma conjunta Locatario y Codeudor",
                    Tipo = "GARANTÍA",
                    ArchivoUrl = "/plantillas/PAGARE_DE_GARANTIA_PROFESIONAL.docx",
                    VariablesSoportadas = "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {Duplex}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {FechaFirma}, {FechaEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}",
                    FechaActualizacion = DateTime.Now
                },
                new PlantillaDocumento
                {
                    Nombre = "ACTA DE ENTREGA Y RECEPCIÓN",
                    Descripcion = "Recepción del inmueble",
                    Tipo = "ANEXO",
                    ArchivoUrl = "/plantillas/ACTA_DE_ENTREGA_Y_RECEPCION.docx",
                    VariablesSoportadas = "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {Duplex}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {FechaFirma}, {FechaEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}",
                    FechaActualizacion = DateTime.Now
                },
                new PlantillaDocumento
                {
                    Nombre = "INVENTARIO DE ENTREGA DE INMUEBLE",
                    Descripcion = "Detalle del inventario al ingresar",
                    Tipo = "ANEXO",
                    ArchivoUrl = "/plantillas/INVENTARIO_DE_ENTREGA_DE_INMUEBLE.docx",
                    VariablesSoportadas = "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {Duplex}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {FechaFirma}, {FechaEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}",
                    FechaActualizacion = DateTime.Now
                },
                new PlantillaDocumento
                {
                    Nombre = "ANEXO FOTOGRÁFICO CONDOMINIO",
                    Descripcion = "Anexo de fotos del estado de entrega",
                    Tipo = "ANEXO",
                    ArchivoUrl = "/plantillas/ANEXO_FOTOGRAFICO_CONDOMINIO.docx",
                    VariablesSoportadas = "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {Duplex}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {FechaFirma}, {FechaEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}",
                    FechaActualizacion = DateTime.Now
                }
            };

            bool changed = false;
            foreach (var official in officialTemplates)
            {
                var existing = plantillas.FirstOrDefault(p => p.Nombre.Equals(official.Nombre, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    _context.PlantillasDocumento.Add(official);
                    changed = true;
                }
                else if (string.IsNullOrEmpty(existing.ArchivoUrl) || existing.ArchivoUrl != official.ArchivoUrl)
                {
                    existing.ArchivoUrl = official.ArchivoUrl;
                    existing.VariablesSoportadas = official.VariablesSoportadas;
                    existing.Tipo = official.Tipo;
                    _context.PlantillasDocumento.Update(existing);
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                plantillas = await _context.PlantillasDocumento.ToListAsync();
            }

            return Ok(plantillas);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlantilla([FromBody] CreatePlantillaRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest("El nombre es requerido.");

            var plantilla = new PlantillaDocumento
            {
                Nombre = req.Nombre.Trim(),
                Descripcion = req.Descripcion?.Trim(),
                Tipo = string.IsNullOrWhiteSpace(req.Tipo) ? "PRINCIPAL" : req.Tipo.Trim(),
                VariablesSoportadas = req.VariablesSoportadas?.Trim(),
                FechaActualizacion = DateTime.Now
            };

            _context.PlantillasDocumento.Add(plantilla);
            await _context.SaveChangesAsync();

            return Ok(plantilla);
        }

        [HttpPut("{id}/upload")]
        public async Task<IActionResult> UploadArchivo(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se proporcionó un archivo válido.");

            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null)
                return NotFound("Plantilla no encontrada.");

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"plantilla_{id}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(_storagePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            plantilla.ArchivoUrl = $"/plantillas/{fileName}";
            plantilla.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(plantilla);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlantilla(int id)
        {
            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null)
                return NotFound("Plantilla no encontrada.");

            if (!string.IsNullOrEmpty(plantilla.ArchivoUrl))
            {
                var relative = plantilla.ArchivoUrl.TrimStart('/');
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    try { System.IO.File.Delete(fullPath); } catch { }
                }
            }

            _context.PlantillasDocumento.Remove(plantilla);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private async Task<(byte[] bytes, string fileName)> GenerarDocumentoBytes(
            int id, int contratoId, string? locatario, string? cedLoc, string? domLoc,
            string? cod, string? cedCod, string? domCod,
            string? duplex, decimal montoAlq, decimal montoGar, decimal montoPag, string? fFirma)
        {
            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null) return (Array.Empty<byte>(), "documento.docx");

            // Inicializamos con lo que viene del QueryString (Frontend) o valores por defecto
            string nombreLocatario = locatario ?? "";
            string cedulaLocatario = cedLoc ?? "";
            string domicilioLocatario = domLoc ?? "Asunción, Paraguay";
            string nombreCodeudor = cod ?? "_____________________";
            string cedulaCodeudor = cedCod ?? "_____________________";
            string domicilioCodeudor = domCod ?? "_____________________";
            string nombreDuplex = duplex ?? "";
            string valMontoAlquiler = montoAlq > 0 ? $"Gs. {montoAlq:N0}" : "";
            string valMontoGarantia = montoGar > 0 ? $"Gs. {montoGar:N0}" : "";
            string valMontoPagare = montoPag > 0 ? $"Gs. {montoPag:N0}" : "";
            string fechaFirma = fFirma ?? DateTime.Now.ToString("dd/MM/yyyy");

            // Si hay un contratoId, intentamos enriquecer los datos desde la BD si faltan
            if (contratoId > 0)
            {
                var c = await _context.ContratosInquilinos
                    .Include(x => x.Inquilino)
                    .Include(x => x.Unidad)
                    .FirstOrDefaultAsync(x => x.ContratoId == contratoId);

                if (c != null)
                {
                    nombreLocatario = string.IsNullOrEmpty(nombreLocatario) ? (c.Inquilino?.RazonSocial ?? "") : nombreLocatario;
                    cedulaLocatario = string.IsNullOrEmpty(cedulaLocatario) ? (c.Inquilino?.Documento ?? "") : cedulaLocatario;
                    nombreDuplex = string.IsNullOrEmpty(nombreDuplex) ? (c.Unidad?.CodigoUnidad ?? "") : nombreDuplex;
                    valMontoAlquiler = montoAlq == 0 ? $"Gs. {c.MontoMensualAlquiler:N0}" : valMontoAlquiler;
                    valMontoGarantia = montoGar == 0 ? $"Gs. {c.DepositoGarantia:N0}" : valMontoGarantia;
                }
            }

            // Lógica de apertura de archivo (mantenemos tu lógica de Stream)
            byte[] docBytes;
            string templatePath = "";
            if (!string.IsNullOrEmpty(plantilla.ArchivoUrl))
            {
                var relative = plantilla.ArchivoUrl.TrimStart('/');
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative.Replace('/', Path.DirectorySeparatorChar));
            }

            using (var memStream = new MemoryStream())
            {
                if (!string.IsNullOrEmpty(templatePath) && System.IO.File.Exists(templatePath))
                {
                    using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(memStream);
                    }
                }
                else { /* ... lógica de creación de doc vacío si no existe ... */ }

                memStream.Position = 0;
                using (var wordDoc = WordprocessingDocument.Open(memStream, true))
                {
                    var body = wordDoc.MainDocumentPart?.Document.Body;
                    if (body != null)
                    {
                        // Diccionario de reemplazos para mayor limpieza
                        var replacer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "{Locatario}", nombreLocatario },
                            { "{CedulaLocatario}", cedulaLocatario },
                            { "{DomicilioLocatario}", domicilioLocatario },
                            { "{Codeudor}", nombreCodeudor },
                            { "{CedulaCodeudor}", cedulaCodeudor },
                            { "{DomicilioCodeudor}", domicilioCodeudor },
                            { "{Duplex}", nombreDuplex },
                            { "{MontoAlquiler}", valMontoAlquiler },
                            { "{MontoGarantia}", valMontoGarantia },
                            { "{MontoPagare}", valMontoPagare },
                            { "{FechaFirma}", fechaFirma }
                        };

                        foreach (var para in body.Descendants<Paragraph>())
                        {
                            foreach (var run in para.Descendants<Run>())
                            {
                                var textNodes = run.Descendants<Text>().ToList();
                                foreach (var textNode in textNodes)
                                {
                                    foreach (var item in replacer)
                                    {
                                        if (textNode.Text.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                                        {
                                            textNode.Text = textNode.Text.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
                                        }
                                    }
                                }
                            }
                        }
                        wordDoc.MainDocumentPart?.Document.Save();
                    }
                }
                docBytes = memStream.ToArray();
            }

            var downloadName = $"{plantilla.Nombre.Replace(" ", "_")}_{nombreLocatario.Replace(" ", "_")}.docx";
            return (docBytes, downloadName);
        }

        [HttpGet("generar/{id}")]
        public async Task<IActionResult> GenerarDocumento(int id)
        {
            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null) return NotFound();

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", plantilla.ArchivoUrl.TrimStart('/'));
            if (!System.IO.File.Exists(templatePath)) return NotFound();

            byte[] docBytes;
            using (var memStream = new MemoryStream())
            {
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(memStream);
                }

                memStream.Position = 0;
                using (var wordDoc = WordprocessingDocument.Open(memStream, true))
                {
                    var body = wordDoc.MainDocumentPart.Document.Body;

                    // PASO 1: UNIFICAR CADA PÁRRAFO (Para arreglar etiquetas rotas por Word)
                    foreach (var para in body.Descendants<Paragraph>())
                    {
                        var texts = para.Descendants<Text>().ToList();
                        if (texts.Count > 1)
                        {
                            // Si el párrafo tiene el texto dividido, intentamos unirlo en el primer nodo
                            // solo si detectamos que hay una etiqueta "{" en el contenido total
                            string fullText = string.Join("", texts.Select(t => t.Text));
                            if (fullText.Contains("{"))
                            {
                                texts[0].Text = fullText;
                                for (int i = 1; i < texts.Count; i++) texts[i].Text = "";
                            }
                        }
                    }

                    // PASO 2: REEMPLAZAR USANDO LAS LLAVES ENVIADAS DESDE REACT
                    foreach (var queryItem in Request.Query)
                    {
                        // Buscamos {Variable} exactamente como viene de la URL
                        string tag = "{" + queryItem.Key + "}";
                        string value = queryItem.Value.ToString();

                        foreach (var textNode in body.Descendants<Text>())
                        {
                            if (textNode.Text.Contains(tag, StringComparison.OrdinalIgnoreCase))
                            {
                                textNode.Text = textNode.Text.Replace(tag, value, StringComparison.OrdinalIgnoreCase);
                            }
                        }
                    }
                    wordDoc.MainDocumentPart.Document.Save();
                }
                docBytes = memStream.ToArray();
            }

            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{plantilla.Nombre}.docx");
        }

        [HttpGet("generar-doc")]
        public async Task<IActionResult> GenerarDocumentoCompat([FromQuery] int plantillaId, [FromQuery] int contratoId = 0,
            [FromQuery] string? locatario = null,
            [FromQuery] string? cedulaLocatario = null,
            [FromQuery] string? domicilioLocatario = null,
            [FromQuery] string? codeudor = null,
            [FromQuery] string? cedulaCodeudor = null,
            [FromQuery] string? domicilioCodeudor = null,
            [FromQuery] string? duplex = null,
            [FromQuery] decimal montoAlquiler = 0,
            [FromQuery] decimal montoGarantia = 0,
            [FromQuery] decimal montoPagare = 0,
            [FromQuery] string? fechaFirma = null)
        {
            var res = await GenerarDocumentoBytes(plantillaId, contratoId, locatario, cedulaLocatario, domicilioLocatario,
                                                codeudor, cedulaCodeudor, domicilioCodeudor,
                                                duplex, montoAlquiler, montoGarantia, montoPagare, fechaFirma);
            if (res.bytes == null || res.bytes.Length == 0)
                return NotFound("Plantilla no encontrada.");

            return File(res.bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", res.fileName);
        }

        [HttpGet("generar-zip/{contratoId}")]
        public async Task<IActionResult> GenerarTodosZip(
            int contratoId,
            [FromQuery] string? locatario = null,
            [FromQuery] string? cedulaLocatario = null,
            [FromQuery] string? domicilioLocatario = null,
            [FromQuery] string? codeudor = null,
            [FromQuery] string? cedulaCodeudor = null,
            [FromQuery] string? domicilioCodeudor = null,
            [FromQuery] string? duplex = null,
            [FromQuery] decimal montoAlquiler = 0,
            [FromQuery] decimal montoGarantia = 0,
            [FromQuery] decimal montoPagare = 0,
            [FromQuery] string? fechaFirma = null)
        {
            var plantillas = await _context.PlantillasDocumento.ToListAsync();
            if (plantillas.Count == 0) return NotFound("No hay plantillas registradas.");

            string nombreLocatario = locatario ?? "Inquilino";
            if (contratoId > 0)
            {
                var c = await _context.ContratosInquilinos.Include(x => x.Inquilino).FirstOrDefaultAsync(x => x.ContratoId == contratoId);
                if (c != null) nombreLocatario = c.Inquilino?.RazonSocial ?? nombreLocatario;
            }

            using (var zipMS = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(zipMS, ZipArchiveMode.Create, true))
                {
                    foreach (var plantilla in plantillas)
                    {
                        // ESTA ES LA LÍNEA QUE DEBÍAS CORREGIR:
                        var (bytes, fileName) = await GenerarDocumentoBytes(
                            plantilla.PlantillaId,
                            contratoId,
                            locatario,
                            cedulaLocatario,
                            domicilioLocatario,
                            codeudor,
                            cedulaCodeudor,
                            domicilioCodeudor,
                            duplex,
                            montoAlquiler,
                            montoGarantia,
                            montoPagare,
                            fechaFirma);

                        if (bytes != null && bytes.Length > 0)
                        {
                            var zipEntry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(bytes, 0, bytes.Length);
                            }
                        }
                    }
                }

                zipMS.Position = 0;
                var zipBytes = zipMS.ToArray();
                var zipName = $"Documentos_Alquiler_{nombreLocatario.Replace(" ", "_")}.zip";
                return File(zipBytes, "application/zip", zipName);
            }
        }

        public class CreatePlantillaRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? Tipo { get; set; }
            public string? VariablesSoportadas { get; set; }
        }
    }
}
