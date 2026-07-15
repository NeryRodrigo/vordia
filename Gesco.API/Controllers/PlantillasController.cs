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

            if (plantillas.Count == 0)
            {
                // Seeding por defecto
                plantillas = new List<PlantillaDocumento>
                {
                    new PlantillaDocumento
                    {
                        Nombre = "CONTRATO DE ALQUILER DÚPLEX",
                        Descripcion = "Formato Estándar OIMA",
                        Tipo = "PRINCIPAL",
                        VariablesSoportadas = "{Locatario}, {Codeudor}, {MontoAlquiler}, {Duplex}",
                        FechaActualizacion = DateTime.Now
                    },
                    new PlantillaDocumento
                    {
                        Nombre = "PAGARÉ DE GARANTÍA",
                        Descripcion = "Firma conjunta Locatario y Codeudor",
                        Tipo = "GARANTÍA",
                        VariablesSoportadas = "{MontoGarantia}, {FechaFirma}, {Locatario}",
                        FechaActualizacion = DateTime.Now
                    },
                    new PlantillaDocumento
                    {
                        Nombre = "ACTA DE INVENTARIO",
                        Descripcion = "Recepción del inmueble",
                        Tipo = "ANEXO",
                        VariablesSoportadas = "{Duplex}, {Locatario}, {FechaEntrega}",
                        FechaActualizacion = DateTime.Now
                    }
                };

                _context.PlantillasDocumento.AddRange(plantillas);
                await _context.SaveChangesAsync();
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

        [HttpGet("generar/{id}")]
        public async Task<IActionResult> GenerarDocumento(int id, [FromQuery] int contratoId = 0, [FromQuery] string? locatario = null, [FromQuery] string? duplex = null, [FromQuery] decimal monto = 0)
        {
            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null)
                return NotFound("Plantilla no encontrada.");

            string nombreLocatario = locatario ?? "Juan Pérez";
            string cedulaLocatario = "1.234.567";
            string domicilioLocatario = "Asunción, Paraguay";
            string nombreCodeudor = "María López";
            string cedulaCodeudor = "4.567.890";
            string domicilioCodeudor = "Luque, Paraguay";
            string nombreDuplex = duplex ?? "Dúplex A-01";
            string montoAlquiler = monto > 0 ? $"Gs. {monto:N0}" : "Gs. 2.500.000";
            string montoGarantia = monto > 0 ? $"Gs. {monto:N0}" : "Gs. 2.500.000";
            string montoPagare = monto > 0 ? $"Gs. {(monto * 3):N0}" : "Gs. 7.500.000";
            string fechaFirma = DateTime.Now.ToString("dd/MM/yyyy");
            string fechaEntrega = DateTime.Now.ToString("dd/MM/yyyy");
            string fechaInicio = DateTime.Now.ToString("dd/MM/yyyy");
            string fechaFin = DateTime.Now.AddYears(1).ToString("dd/MM/yyyy");
            string duracionMeses = "12";

            if (contratoId > 0)
            {
                var c = await _context.ContratosInquilinos
                    .Include(x => x.Inquilino)
                    .Include(x => x.Unidad)
                    .FirstOrDefaultAsync(x => x.ContratoId == contratoId);

                if (c != null)
                {
                    nombreLocatario = c.Inquilino?.RazonSocial ?? nombreLocatario;
                    cedulaLocatario = !string.IsNullOrEmpty(c.Inquilino?.Documento) ? c.Inquilino.Documento : cedulaLocatario;
                    domicilioLocatario = !string.IsNullOrEmpty(c.Inquilino?.Telefono) ? $"Asunción (Tel: {c.Inquilino.Telefono})" : domicilioLocatario;
                    nombreDuplex = c.Unidad?.CodigoUnidad ?? (c.Unidad?.NombreCondominio ?? nombreDuplex);
                    montoAlquiler = $"Gs. {c.MontoMensualAlquiler:N0}";
                    montoGarantia = $"Gs. {c.DepositoGarantia:N0}";
                    montoPagare = $"Gs. {(c.MontoMensualAlquiler * 3):N0}";
                    fechaFirma = c.FechaInicioContrato.ToString("dd/MM/yyyy");
                    fechaEntrega = c.FechaIngreso.ToString("dd/MM/yyyy");
                    fechaInicio = c.FechaInicioContrato.ToString("dd/MM/yyyy");
                    fechaFin = c.FechaFinContrato.ToString("dd/MM/yyyy");
                    var meses = (int)Math.Round((c.FechaFinContrato - c.FechaInicioContrato).TotalDays / 30.0);
                    duracionMeses = meses > 0 ? meses.ToString() : "12";
                }
            }

            // Si no tiene archivo Word cargado, generamos un documento Word en memoria con las variables
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
                else
                {
                    // Crear plantilla básica de ejemplo
                    using (var wordDoc = WordprocessingDocument.Create(memStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
                    {
                        var mainPart = wordDoc.AddMainDocumentPart();
                        mainPart.Document = new Document(new Body(
                            new Paragraph(new Run(new Text(plantilla.Nombre.ToUpper()) { } )),
                            new Paragraph(new Run(new Text($"Documento autogenerado para: {nombreLocatario}"))),
                            new Paragraph(new Run(new Text($"Dúplex: {nombreDuplex} - Alquiler: {montoAlquiler}"))),
                            new Paragraph(new Run(new Text($"Fecha de firma: {fechaFirma}")))
                        ));
                        mainPart.Document.Save();
                    }
                }

                memStream.Position = 0;
                using (var wordDoc = WordprocessingDocument.Open(memStream, true))
                {
                    var body = wordDoc.MainDocumentPart?.Document.Body;
                    if (body != null)
                    {
                        foreach (var para in body.Descendants<Paragraph>())
                        {
                            var text = para.InnerText;
                            if (!string.IsNullOrEmpty(text) && text.Contains("{"))
                            {
                                var newText = text
                                    .Replace("{Locatario}", nombreLocatario)
                                    .Replace("{locatario}", nombreLocatario)
                                    .Replace("{CedulaLocatario}", cedulaLocatario)
                                    .Replace("{cedulaLocatario}", cedulaLocatario)
                                    .Replace("{DomicilioLocatario}", domicilioLocatario)
                                    .Replace("{domicilioLocatario}", domicilioLocatario)
                                    .Replace("{Codeudor}", nombreCodeudor)
                                    .Replace("{codeudor}", nombreCodeudor)
                                    .Replace("{CedulaCodeudor}", cedulaCodeudor)
                                    .Replace("{cedulaCodeudor}", cedulaCodeudor)
                                    .Replace("{DomicilioCodeudor}", domicilioCodeudor)
                                    .Replace("{domicilioCodeudor}", domicilioCodeudor)
                                    .Replace("{Duplex}", nombreDuplex)
                                    .Replace("{duplex}", nombreDuplex)
                                    .Replace("{MontoAlquiler}", montoAlquiler)
                                    .Replace("{montoAlquiler}", montoAlquiler)
                                    .Replace("{MontoGarantia}", montoGarantia)
                                    .Replace("{montoGarantia}", montoGarantia)
                                    .Replace("{MontoPagare}", montoPagare)
                                    .Replace("{montoPagare}", montoPagare)
                                    .Replace("{FechaFirma}", fechaFirma)
                                    .Replace("{fechaFirma}", fechaFirma)
                                    .Replace("{FechaEntrega}", fechaEntrega)
                                    .Replace("{fechaEntrega}", fechaEntrega)
                                    .Replace("{FechaInicio}", fechaInicio)
                                    .Replace("{fechaInicio}", fechaInicio)
                                    .Replace("{FechaFin}", fechaFin)
                                    .Replace("{fechaFin}", fechaFin)
                                    .Replace("{DuracionMeses}", duracionMeses)
                                    .Replace("{duracionMeses}", duracionMeses);

                                if (newText != text)
                                {
                                    para.RemoveAllChildren<Run>();
                                    para.AppendChild(new Run(new Text(newText)));
                                }
                            }
                        }
                        wordDoc.MainDocumentPart?.Document.Save();
                    }
                }

                docBytes = memStream.ToArray();
            }

            var downloadName = $"{plantilla.Nombre.Replace(" ", "_")}_{nombreLocatario.Replace(" ", "_")}.docx";
            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", downloadName);
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
