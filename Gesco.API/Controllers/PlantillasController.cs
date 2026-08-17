using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gesco.Domain.Entities;
using Gesco.Application.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlantillasController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "plantillas");
        private static readonly CultureInfo CulturaPy = new("es-PY");

        /// <summary>Plantillas oficiales = archivos en wwwroot/plantillas.</summary>
        private static readonly List<PlantillaOficial> PlantillasOficiales = new()
        {
            new("CONTRATO DE ALQUILER DÚPLEX", "PRINCIPAL", "Contrato_Alquiler_Duplex_Oficial.docx",
                "Contrato de Alquiler de Dúplex Oficial"),
            new("PAGARÉ DE GARANTÍA PROFESIONAL", "GARANTÍA", "PAGARE_DE_GARANTIA_PROFESIONAL.docx",
                "Firma conjunta Locatario y Codeudor"),
            new("ACTA DE ENTREGA Y RECEPCIÓN", "ANEXO", "ACTA_DE_ENTREGA_Y_RECEPCION.docx",
                "Recepción del inmueble"),
            new("INVENTARIO DE ENTREGA DE INMUEBLE", "ANEXO", "INVENTARIO_DE_ENTREGA_DE_INMUEBLE.docx",
                "Detalle del inventario al ingresar"),
            new("ANEXO FOTOGRÁFICO CONDOMINIO", "ANEXO", "ANEXO_FOTOGRAFICO_CONDOMINIO.docx",
                "Anexo de fotos del estado de entrega"),
        };

        private static readonly Dictionary<string, string> AliasAOficial = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CONTRATO DE ALQUILER"] = "CONTRATO DE ALQUILER DÚPLEX",
            ["CONTRATO DE ALQUILER DUPLEX"] = "CONTRATO DE ALQUILER DÚPLEX",
            ["PAGARÉ DE GARANTÍA"] = "PAGARÉ DE GARANTÍA PROFESIONAL",
            ["PAGARE DE GARANTIA"] = "PAGARÉ DE GARANTÍA PROFESIONAL",
            ["PAGARE DE GARANTIA PROFESIONAL"] = "PAGARÉ DE GARANTÍA PROFESIONAL",
            ["ACTA DE INVENTARIO"] = "INVENTARIO DE ENTREGA DE INMUEBLE",
            ["ACTA DE ENTREGA Y RECEPCION"] = "ACTA DE ENTREGA Y RECEPCIÓN",
            ["ANEXO FOTOGRAFICO CONDOMINIO"] = "ANEXO FOTOGRÁFICO CONDOMINIO",
        };

        private const string VariablesSoportadas =
            "{Locatario}, {CedulaLocatario}, {DomicilioLocatario}, {TelefonoLocatario}, {Codeudor}, {CedulaCodeudor}, {DomicilioCodeudor}, {TelefonoCodeudor}, {Duplex}, {Condominio}, {NisAnde}, {MontoAlquiler}, {MontoGarantia}, {MontoPagare}, {MontoPagareLetras}, {FechaFirma}, {DiaEntrega}, {MesEntrega}, {AnioEntrega}, {FechaInicio}, {FechaFin}, {DuracionMeses}";

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
            var plantillas = await SyncOfficialPlantillasAsync();
            return Ok(plantillas);
        }

        private async Task<List<PlantillaDocumento>> SyncOfficialPlantillasAsync()
        {
            var plantillas = await _context.PlantillasDocumento.ToListAsync();
            bool changed = false;

            foreach (var official in PlantillasOficiales)
            {
                var archivoUrl = $"/plantillas/{official.Archivo}";
                var existing = plantillas.FirstOrDefault(p =>
                    p.Nombre.Equals(official.Nombre, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    var aliasNames = AliasAOficial
                        .Where(kv => kv.Value.Equals(official.Nombre, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Key)
                        .ToList();
                    existing = plantillas.FirstOrDefault(p =>
                        aliasNames.Any(a => p.Nombre.Equals(a, StringComparison.OrdinalIgnoreCase)));
                }

                if (existing == null)
                {
                    _context.PlantillasDocumento.Add(new PlantillaDocumento
                    {
                        Nombre = official.Nombre,
                        Descripcion = official.Descripcion,
                        Tipo = official.Tipo,
                        ArchivoUrl = archivoUrl,
                        VariablesSoportadas = VariablesSoportadas,
                        FechaActualizacion = DateTime.Now
                    });
                    changed = true;
                }
                else
                {
                    bool needsUpdate = false;
                    if (!existing.Nombre.Equals(official.Nombre, StringComparison.Ordinal))
                    {
                        existing.Nombre = official.Nombre;
                        needsUpdate = true;
                    }
                    if (existing.ArchivoUrl != archivoUrl)
                    {
                        existing.ArchivoUrl = archivoUrl;
                        needsUpdate = true;
                    }
                    if (existing.Tipo != official.Tipo)
                    {
                        existing.Tipo = official.Tipo;
                        needsUpdate = true;
                    }
                    if (existing.Descripcion != official.Descripcion)
                    {
                        existing.Descripcion = official.Descripcion;
                        needsUpdate = true;
                    }
                    if (existing.VariablesSoportadas != VariablesSoportadas)
                    {
                        existing.VariablesSoportadas = VariablesSoportadas;
                        needsUpdate = true;
                    }
                    if (needsUpdate)
                    {
                        existing.FechaActualizacion = DateTime.Now;
                        _context.PlantillasDocumento.Update(existing);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                plantillas = await _context.PlantillasDocumento.ToListAsync();
            }

            var officialNames = new HashSet<string>(
                PlantillasOficiales.Select(p => p.Nombre),
                StringComparer.OrdinalIgnoreCase);

            var toRemove = plantillas
                .Where(p => !officialNames.Contains(p.Nombre))
                .Where(p =>
                {
                    var path = ResolveTemplatePath(p.ArchivoUrl);
                    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                        return true;
                    var fileName = Path.GetFileName(path);
                    return PlantillasOficiales.Any(o =>
                        o.Archivo.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                })
                .ToList();

            if (toRemove.Count > 0)
            {
                _context.PlantillasDocumento.RemoveRange(toRemove);
                await _context.SaveChangesAsync();
                plantillas = await _context.PlantillasDocumento.ToListAsync();
            }

            var ordenTipo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["PRINCIPAL"] = 0,
                ["GARANTÍA"] = 1,
                ["GARANTIA"] = 1,
                ["ANEXO"] = 2
            };

            return plantillas
                .OrderBy(p => ordenTipo.TryGetValue(p.Tipo ?? "", out var o) ? o : 9)
                .ThenBy(p => p.Nombre)
                .ToList();
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
                VariablesSoportadas = req.VariablesSoportadas?.Trim() ?? VariablesSoportadas,
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
                var fullPath = ResolveTemplatePath(plantilla.ArchivoUrl);
                if (!string.IsNullOrEmpty(fullPath) && System.IO.File.Exists(fullPath))
                {
                    var fileName = Path.GetFileName(fullPath);
                    var esOficial = PlantillasOficiales.Any(o =>
                        o.Archivo.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                    if (!esOficial)
                    {
                        try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }
                    }
                }
            }

            _context.PlantillasDocumento.Remove(plantilla);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("generar/{id}")]
        public async Task<IActionResult> GenerarDocumento(int id, [FromQuery] int contratoId = 0)
        {
            var queryVals = Request.Query.ToDictionary(
                q => q.Key,
                q => q.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            var res = await GenerarDocumentoBytes(id, contratoId, queryVals);
            if (res.bytes == null || res.bytes.Length == 0)
                return NotFound(res.fileName == "sin_plantilla"
                    ? "Plantilla no encontrada."
                    : "Archivo de plantilla no encontrado en wwwroot/plantillas. Verifique que el prototipo .docx esté publicado.");

            return File(res.bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", res.fileName);
        }

        [HttpGet("generar-doc")]
        public async Task<IActionResult> GenerarDocumentoCompat(
            [FromQuery] int plantillaId,
            [FromQuery] int contratoId = 0,
            [FromQuery] string? locatario = null,
            [FromQuery] string? cedulaLocatario = null,
            [FromQuery] string? domicilioLocatario = null,
            [FromQuery] string? telefonoLocatario = null,
            [FromQuery] string? codeudor = null,
            [FromQuery] string? cedulaCodeudor = null,
            [FromQuery] string? domicilioCodeudor = null,
            [FromQuery] string? telefonoCodeudor = null,
            [FromQuery] string? duplex = null,
            [FromQuery] string? condominio = null,
            [FromQuery] string? nisAnde = null,
            [FromQuery] decimal montoAlquiler = 0,
            [FromQuery] decimal montoGarantia = 0,
            [FromQuery] decimal montoPagare = 0,
            [FromQuery] string? montoPagareLetras = null,
            [FromQuery] string? fechaFirma = null,
            [FromQuery] string? fechaInicio = null,
            [FromQuery] string? fechaFin = null,
            [FromQuery] string? duracionMeses = null,
            [FromQuery] string? diaEntrega = null,
            [FromQuery] string? mesEntrega = null,
            [FromQuery] string? anioEntrega = null)
        {
            var queryVals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Put(string k, string? v) { if (!string.IsNullOrWhiteSpace(v)) queryVals[k] = v!; }

            Put("Locatario", locatario);
            Put("CedulaLocatario", cedulaLocatario);
            Put("DomicilioLocatario", domicilioLocatario);
            Put("TelefonoLocatario", telefonoLocatario);
            Put("Codeudor", codeudor);
            Put("CedulaCodeudor", cedulaCodeudor);
            Put("DomicilioCodeudor", domicilioCodeudor);
            Put("TelefonoCodeudor", telefonoCodeudor);
            Put("Duplex", duplex);
            Put("Condominio", condominio);
            Put("NisAnde", nisAnde);
            if (montoAlquiler > 0) Put("MontoAlquiler", montoAlquiler.ToString("N0", CulturaPy));
            if (montoGarantia > 0) Put("MontoGarantia", montoGarantia.ToString("N0", CulturaPy));
            if (montoPagare > 0) Put("MontoPagare", montoPagare.ToString("N0", CulturaPy));
            Put("MontoPagareLetras", montoPagareLetras);
            Put("FechaFirma", fechaFirma);
            Put("FechaInicio", fechaInicio);
            Put("FechaFin", fechaFin);
            Put("DuracionMeses", duracionMeses);
            Put("DiaEntrega", diaEntrega);
            Put("MesEntrega", mesEntrega);
            Put("AnioEntrega", anioEntrega);

            // También aceptar cualquier otro query param (compat)
            foreach (var q in Request.Query)
            {
                if (!queryVals.ContainsKey(q.Key) && !string.IsNullOrWhiteSpace(q.Value))
                    queryVals[q.Key] = q.Value.ToString();
            }

            var res = await GenerarDocumentoBytes(plantillaId, contratoId, queryVals);
            if (res.bytes == null || res.bytes.Length == 0)
                return NotFound("Plantilla o archivo de prototipo no encontrado.");

            return File(res.bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", res.fileName);
        }

        [HttpGet("generar-zip/{contratoId}")]
        public async Task<IActionResult> GenerarTodosZip(int contratoId)
        {
            var plantillas = await SyncOfficialPlantillasAsync();
            if (plantillas.Count == 0) return NotFound("No hay plantillas registradas.");

            var queryVals = Request.Query.ToDictionary(
                q => q.Key,
                q => q.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            string nombreLocatario = queryVals.GetValueOrDefault("Locatario")
                ?? queryVals.GetValueOrDefault("locatario")
                ?? "Inquilino";

            if (contratoId > 0)
            {
                var c = await _context.ContratosInquilinos.Include(x => x.Inquilino)
                    .FirstOrDefaultAsync(x => x.ContratoId == contratoId);
                if (c?.Inquilino != null && !string.IsNullOrWhiteSpace(c.Inquilino.RazonSocial))
                    nombreLocatario = c.Inquilino.RazonSocial;
            }

            using var zipMS = new MemoryStream();
            using (var zipArchive = new ZipArchive(zipMS, ZipArchiveMode.Create, true))
            {
                foreach (var plantilla in plantillas)
                {
                    var (bytes, fileName) = await GenerarDocumentoBytes(plantilla.PlantillaId, contratoId, queryVals);
                    if (bytes == null || bytes.Length == 0) continue;

                    var zipEntry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                    using var entryStream = zipEntry.Open();
                    await entryStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }

            zipMS.Position = 0;
            var zipName = $"Documentos_Alquiler_{SanitizeFileName(nombreLocatario)}.zip";
            return File(zipMS.ToArray(), "application/zip", zipName);
        }

        private async Task<(byte[] bytes, string fileName)> GenerarDocumentoBytes(
            int id,
            int contratoId,
            Dictionary<string, string> queryVals)
        {
            var plantilla = await _context.PlantillasDocumento.FindAsync(id);
            if (plantilla == null) return (Array.Empty<byte>(), "sin_plantilla");

            var values = await BuildReplacementValuesAsync(contratoId, queryVals);
            var templatePath = ResolveTemplatePath(plantilla.ArchivoUrl, plantilla.Nombre);

            if (string.IsNullOrEmpty(templatePath) || !System.IO.File.Exists(templatePath))
                return (Array.Empty<byte>(), "sin_archivo");

            byte[] docBytes;
            using (var memStream = new MemoryStream())
            {
                using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await fileStream.CopyToAsync(memStream);
                }

                memStream.Position = 0;
                using (var wordDoc = WordprocessingDocument.Open(memStream, true))
                {
                    ApplyReplacementsToDocument(wordDoc, values);
                    EnsureMembreteYPieComoExpediente(wordDoc, values, plantilla.Nombre);
                    wordDoc.MainDocumentPart?.Document.Save();
                }
                docBytes = memStream.ToArray();
            }

            var locatario = values.GetValueOrDefault("Locatario") ?? "Inquilino";
            var duplex = values.GetValueOrDefault("Duplex") ?? "";
            var downloadName = $"{SanitizeFileName(plantilla.Nombre)}_{SanitizeFileName(locatario)}";
            if (!string.IsNullOrWhiteSpace(duplex))
                downloadName += $"_Duplex-{SanitizeFileName(duplex)}";
            downloadName += ".docx";

            return (docBytes, downloadName);
        }

        private async Task<Dictionary<string, string>> BuildReplacementValuesAsync(
            int contratoId,
            Dictionary<string, string> queryVals)
        {
            string Get(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (queryVals.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                        return v.Trim();
                }
                return "";
            }

            var hoy = DateTime.Now;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Locatario"] = Get("Locatario", "locatario"),
                ["CedulaLocatario"] = Get("CedulaLocatario", "cedulaLocatario"),
                ["DomicilioLocatario"] = Get("DomicilioLocatario", "domicilioLocatario"),
                ["TelefonoLocatario"] = Get("TelefonoLocatario", "telefonoLocatario"),
                ["Codeudor"] = Get("Codeudor", "codeudor"),
                ["CedulaCodeudor"] = Get("CedulaCodeudor", "cedulaCodeudor"),
                ["DomicilioCodeudor"] = Get("DomicilioCodeudor", "domicilioCodeudor"),
                ["TelefonoCodeudor"] = Get("TelefonoCodeudor", "telefonoCodeudor"),
                ["Duplex"] = Get("Duplex", "duplex"),
                ["Condominio"] = Get("Condominio", "condominio"),
                ["NisAnde"] = Get("NisAnde", "nisAnde"),
                ["MontoAlquiler"] = Get("MontoAlquiler", "montoAlquiler"),
                ["MontoGarantia"] = Get("MontoGarantia", "montoGarantia"),
                ["MontoPagare"] = Get("MontoPagare", "montoPagare", "monto"),
                ["MontoPagareLetras"] = Get("MontoPagareLetras", "montoPagareLetras"),
                ["FechaFirma"] = Get("FechaFirma", "fechaFirma"),
                ["DiaEntrega"] = Get("DiaEntrega", "diaEntrega"),
                ["MesEntrega"] = Get("MesEntrega", "mesEntrega"),
                ["AnioEntrega"] = Get("AnioEntrega", "anioEntrega"),
                ["FechaInicio"] = Get("FechaInicio", "fechaInicio"),
                ["FechaFin"] = Get("FechaFin", "fechaFin"),
                ["DuracionMeses"] = Get("DuracionMeses", "duracionMeses"),
            };

            // Defaults de fecha de entrega / firma
            if (string.IsNullOrWhiteSpace(values["FechaFirma"]))
                values["FechaFirma"] = hoy.ToString("dd/MM/yyyy");
            if (string.IsNullOrWhiteSpace(values["DiaEntrega"]))
                values["DiaEntrega"] = hoy.Day.ToString();
            if (string.IsNullOrWhiteSpace(values["MesEntrega"]))
                values["MesEntrega"] = hoy.ToString("MMMM", CulturaPy);
            if (string.IsNullOrWhiteSpace(values["AnioEntrega"]))
                values["AnioEntrega"] = hoy.Year.ToString();

            if (contratoId > 0)
            {
                var c = await _context.ContratosInquilinos
                    .Include(x => x.Inquilino)
                    .Include(x => x.Unidad)
                    .FirstOrDefaultAsync(x => x.ContratoId == contratoId);

                if (c != null)
                {
                    if (string.IsNullOrWhiteSpace(values["Locatario"]))
                        values["Locatario"] = c.Inquilino?.RazonSocial ?? "";
                    if (string.IsNullOrWhiteSpace(values["CedulaLocatario"]))
                        values["CedulaLocatario"] = c.Inquilino?.Documento ?? "";
                    if (string.IsNullOrWhiteSpace(values["TelefonoLocatario"]))
                        values["TelefonoLocatario"] = c.Inquilino?.Telefono ?? "";
                    if (string.IsNullOrWhiteSpace(values["Duplex"]))
                        values["Duplex"] = ExtraerDigitoDuplex(c.Unidad?.CodigoUnidad);
                    if (string.IsNullOrWhiteSpace(values["Condominio"]))
                        values["Condominio"] = c.Unidad?.NombreCondominio ?? "";
                    if (string.IsNullOrWhiteSpace(values["NisAnde"]))
                        values["NisAnde"] = c.Unidad?.NisAnde ?? "";

                    if (string.IsNullOrWhiteSpace(values["MontoAlquiler"]) && c.MontoMensualAlquiler > 0)
                        values["MontoAlquiler"] = c.MontoMensualAlquiler.ToString("N0", CulturaPy);
                    if (string.IsNullOrWhiteSpace(values["MontoGarantia"]) && c.DepositoGarantia > 0)
                        values["MontoGarantia"] = c.DepositoGarantia.ToString("N0", CulturaPy);
                    if (string.IsNullOrWhiteSpace(values["MontoPagare"]) && c.DepositoGarantia > 0)
                        values["MontoPagare"] = c.DepositoGarantia.ToString("N0", CulturaPy);

                    if (string.IsNullOrWhiteSpace(values["FechaInicio"]))
                        values["FechaInicio"] = c.FechaInicioContrato.ToString("dd/MM/yyyy");
                    if (string.IsNullOrWhiteSpace(values["FechaFin"]))
                        values["FechaFin"] = c.FechaFinContrato.ToString("dd/MM/yyyy");
                    if (string.IsNullOrWhiteSpace(values["DuracionMeses"]))
                    {
                        var meses = ((c.FechaFinContrato.Year - c.FechaInicioContrato.Year) * 12)
                                    + c.FechaFinContrato.Month - c.FechaInicioContrato.Month;
                        if (meses < 1) meses = 12;
                        values["DuracionMeses"] = meses.ToString();
                    }
                }
            }

            // Placeholders vacíos → línea para completar a mano
            foreach (var key in values.Keys.ToList())
            {
                if (string.IsNullOrWhiteSpace(values[key]))
                    values[key] = "____________________";
            }

            // Copiar cualquier otra clave del query (por si el Word usa tags extra)
            foreach (var kv in queryVals)
            {
                if (!values.ContainsKey(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                    values[kv.Key] = kv.Value;
            }

            return values;
        }

        private static void ApplyReplacementsToDocument(WordprocessingDocument wordDoc, Dictionary<string, string> values)
        {
            var replacer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in values)
            {
                var key = kv.Key.Trim().Trim('{', '}');
                var val = kv.Value ?? "";
                replacer[$"{{{key}}}"] = val;
                replacer[$"{{{{{key}}}}}"] = val; // por si alguna plantilla usa {{Var}}
            }

            var parts = new List<OpenXmlPart>();
            if (wordDoc.MainDocumentPart != null)
            {
                parts.Add(wordDoc.MainDocumentPart);
                parts.AddRange(wordDoc.MainDocumentPart.HeaderParts);
                parts.AddRange(wordDoc.MainDocumentPart.FooterParts);
            }

            foreach (var part in parts)
            {
                var root = part is MainDocumentPart main
                    ? (DocumentFormat.OpenXml.OpenXmlElement?)main.Document?.Body
                    : part is HeaderPart hp
                        ? hp.Header
                        : part is FooterPart fp
                            ? fp.Footer
                            : null;

                if (root == null) continue;

                foreach (var para in root.Descendants<Paragraph>())
                {
                    UnifyPlaceholderRuns(para);
                    foreach (var textNode in para.Descendants<Text>())
                    {
                        var text = textNode.Text;
                        if (string.IsNullOrEmpty(text) || !text.Contains('{')) continue;

                        foreach (var item in replacer)
                        {
                            if (text.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                                text = text.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
                        }
                        textNode.Text = text;
                    }
                }
            }
        }

        /// <summary>
        /// Membrete Fontana + pie Vordia (mismo criterio visual que EXP. PDF),
        /// sin alterar el cuerpo redactado de la plantilla.
        /// </summary>
        private void EnsureMembreteYPieComoExpediente(
            WordprocessingDocument wordDoc,
            Dictionary<string, string> values,
            string? nombrePlantilla)
        {
            var main = wordDoc.MainDocumentPart;
            if (main?.Document?.Body == null) return;

            var fontanaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email", "fontanas.png");
            var vordiaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email", "VORDIA H NEGRO.png");
            if (!System.IO.File.Exists(fontanaPath) || !System.IO.File.Exists(vordiaPath))
                return;

            // Quitar headers/footers previos para unificar brand
            foreach (var hp in main.HeaderParts.ToList())
                main.DeletePart(hp);
            foreach (var fp in main.FooterParts.ToList())
                main.DeletePart(fp);

            var headerPart = main.AddNewPart<HeaderPart>();
            var footerPart = main.AddNewPart<FooterPart>();
            var headerRel = main.GetIdOfPart(headerPart);
            var footerRel = main.GetIdOfPart(footerPart);

            var fontanaImg = headerPart.AddImagePart(ImagePartType.Png);
            using (var fs = System.IO.File.OpenRead(fontanaPath))
                fontanaImg.FeedData(fs);
            var fontanaId = headerPart.GetIdOfPart(fontanaImg);

            var vordiaImg = footerPart.AddImagePart(ImagePartType.Png);
            using (var fs = System.IO.File.OpenRead(vordiaPath))
                vordiaImg.FeedData(fs);
            var vordiaId = footerPart.GetIdOfPart(vordiaImg);

            string Clean(string? v) =>
                string.IsNullOrWhiteSpace(v) || v.StartsWith("____") ? "—" : v.Trim();

            var condominio = Clean(values.GetValueOrDefault("Condominio"));
            var duplex = Clean(values.GetValueOrDefault("Duplex"));
            var fecha = Clean(values.GetValueOrDefault("FechaFirma"));
            var eyebrow = string.IsNullOrWhiteSpace(nombrePlantilla)
                ? "Documento contractual"
                : nombrePlantilla.Trim();

            headerPart.Header = BuildHeaderFontana(fontanaId, eyebrow, fecha, condominio, duplex);
            headerPart.Header.Save();

            footerPart.Footer = BuildFooterVordia(vordiaId);
            footerPart.Footer.Save();

            var sectProps = main.Document.Body.Elements<SectionProperties>().ToList();
            if (sectProps.Count == 0)
            {
                var sect = new SectionProperties();
                main.Document.Body.AppendChild(sect);
                sectProps.Add(sect);
            }

            foreach (var sect in sectProps)
            {
                sect.RemoveAllChildren<HeaderReference>();
                sect.RemoveAllChildren<FooterReference>();
                sect.PrependChild(new FooterReference { Type = HeaderFooterValues.Default, Id = footerRel });
                sect.PrependChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRel });

                // Márgenes superiores/inferiores para que entren logo + pie
                var pageMargin = sect.GetFirstChild<PageMargin>();
                if (pageMargin == null)
                {
                    pageMargin = new PageMargin();
                    sect.AppendChild(pageMargin);
                }
                pageMargin.Top = 1008;    // ~1.8 cm
                pageMargin.Bottom = 1008;
                pageMargin.Header = 360; // ~0.6 cm
                pageMargin.Footer = 360;
            }
        }

        private static Header BuildHeaderFontana(
            string imageRelId,
            string eyebrow,
            string fecha,
            string condominio,
            string duplex)
        {
            // ~68x68 px @ 96dpi → EMUs
            const long logoCx = 685800;
            const long logoCy = 685800;

            var logoCell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = "1100" },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                    new Shading { Val = ShadingPatternValues.Clear, Fill = "080E1E", Color = "auto" }),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
                    new Run(CreateInlineImage(imageRelId, logoCx, logoCy, "Fontana"))));

            var metaCell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = "6500" },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "40" }),
                    RunStyled(eyebrow.ToUpperInvariant(), "1E7BFF", 14, true)),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }),
                    RunMuted("Emisión: "), RunBold(fecha),
                    RunMuted("   ·   Condominio: "), RunBold(condominio)),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }),
                    RunMuted("Dúplex: "), RunBold(duplex)));

            var brandCell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = "2200" },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Right },
                        new SpacingBetweenLines { Before = "0", After = "0" }),
                    RunStyled("FONTANA", "0F172A", 16, true)),
                new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Right },
                        new SpacingBetweenLines { Before = "0", After = "0" }),
                    RunMuted("Di Luque · Est. 2020")));

            var table = new Table(
                new TableProperties(
                    new TableWidth { Width = "9800", Type = TableWidthUnitValues.Dxa },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Nil },
                        new LeftBorder { Val = BorderValues.Nil },
                        new RightBorder { Val = BorderValues.Nil },
                        new BottomBorder { Val = BorderValues.Nil },
                        new InsideHorizontalBorder { Val = BorderValues.Nil },
                        new InsideVerticalBorder { Val = BorderValues.Nil })),
                new TableRow(logoCell, metaCell, brandCell));

            var line = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "80", After = "120" },
                    new ParagraphBorders(
                        new BottomBorder
                        {
                            Val = BorderValues.Single,
                            Size = 18,
                            Color = "0F172A",
                            Space = 1
                        })));

            return new Header(table, line);
        }

        private static Footer BuildFooterVordia(string imageRelId)
        {
            // VORDIA H NEGRO.png es horizontal (~5818x1326). Forzarlo a cuadrado lo aplasta ("gordito").
            const long logoCy = 320040; // ~0.35"
            const long logoCx = (long)(logoCy * (5818.0 / 1326.0)); // ~1.54"

            var line = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "0", After = "60" },
                    new ParagraphBorders(
                        new TopBorder
                        {
                            Val = BorderValues.Single,
                            Size = 18,
                            Color = "0F172A",
                            Space = 1
                        })));

            var logoPara = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right },
                    new SpacingBetweenLines { Before = "0", After = "0" }),
                new Run(CreateInlineImage(imageRelId, logoCx, logoCy, "Vordia")));

            return new Footer(line, logoPara);
        }

        private static Drawing CreateInlineImage(string relationshipId, long cx, long cy, string name)
        {
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = 1U, Name = name },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = name + ".png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }))
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U
                });
        }

        private static Run RunStyled(string text, string hexColor, int halfPoints, bool bold)
        {
            var props = new RunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = halfPoints.ToString() },
                new Color { Val = hexColor });
            if (bold) props.AppendChild(new Bold());
            return new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static Run RunBold(string text) => RunStyled(text, "0F172A", 15, true);
        private static Run RunMuted(string text) => RunStyled(text, "64748B", 15, false);

        /// <summary>
        /// Word suele partir "{Locatario}" en varios runs; unifica el párrafo cuando hay llaves.
        /// </summary>
        private static void UnifyPlaceholderRuns(Paragraph para)
        {
            var texts = para.Descendants<Text>().ToList();
            if (texts.Count <= 1) return;

            var fullText = string.Concat(texts.Select(t => t.Text));
            if (!fullText.Contains('{') && !fullText.Contains('}')) return;

            texts[0].Text = fullText;
            for (int i = 1; i < texts.Count; i++)
                texts[i].Text = "";
        }

        private string? ResolveTemplatePath(string? archivoUrl, string? nombrePlantilla = null)
        {
            if (!string.IsNullOrWhiteSpace(archivoUrl))
            {
                var relative = archivoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                // Si viene como /plantillas/x.docx → wwwroot/plantillas/x.docx
                var candidate = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                if (System.IO.File.Exists(candidate))
                    return candidate;

                // A veces ArchivoUrl ya incluye "wwwroot"
                candidate = Path.Combine(Directory.GetCurrentDirectory(), relative);
                if (System.IO.File.Exists(candidate))
                    return candidate;
            }

            // Fallback por nombre de plantilla oficial
            if (!string.IsNullOrWhiteSpace(nombrePlantilla))
            {
                var nombreCanonico = AliasAOficial.TryGetValue(nombrePlantilla, out var mapped)
                    ? mapped
                    : nombrePlantilla;
                var official = PlantillasOficiales.FirstOrDefault(p =>
                    p.Nombre.Equals(nombreCanonico, StringComparison.OrdinalIgnoreCase));
                if (official != null)
                {
                    var path = Path.Combine(_storagePath, official.Archivo);
                    if (System.IO.File.Exists(path))
                        return path;
                }
            }

            // Último recurso: buscar por nombre de archivo en la carpeta
            if (!string.IsNullOrWhiteSpace(archivoUrl))
            {
                var fileName = Path.GetFileName(archivoUrl);
                var path = Path.Combine(_storagePath, fileName);
                if (System.IO.File.Exists(path))
                    return path;
            }

            return null;
        }

        private static string ExtraerDigitoDuplex(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return "";
            var m = Regex.Match(codigo, @"\d+");
            return m.Success ? m.Value : codigo.Trim();
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Documento";
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(ch => invalid.Contains(ch) || ch == ' ' ? '_' : ch).ToArray());
            return Regex.Replace(cleaned, "_+", "_").Trim('_');
        }

        private sealed record PlantillaOficial(string Nombre, string Tipo, string Archivo, string Descripcion);

        public class CreatePlantillaRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? Tipo { get; set; }
            public string? VariablesSoportadas { get; set; }
        }
    }
}
