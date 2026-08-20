using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Gesco.Domain.Entities;
using Gesco.Infrastructure.Persistence;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentosController : ControllerBase
    {
        private readonly GescoDbContext _context;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
        private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx", ".xls", ".xlsx"
        };

        public DocumentosController(GescoDbContext context)
        {
            _context = context;
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost("upload")]
        [EnableRateLimiting("uploads")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> UploadDocument([FromForm] int contratoId, [FromForm] string tipoDocumento, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No se ha proporcionado un archivo.");
            if (file.Length > 15 * 1024 * 1024) return BadRequest("El archivo supera el límite de 15 MB.");

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
                return BadRequest("Tipo de archivo no permitido.");

            var safeTipo = string.Concat((tipoDocumento ?? "doc").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
            if (string.IsNullOrEmpty(safeTipo)) safeTipo = "doc";

            var fileName = $"{contratoId}_{safeTipo}_{Guid.NewGuid()}{ext.ToLowerInvariant()}";
            var filePath = Path.Combine(_storagePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var doc = await _context.DocumentosInquilino
                .FirstOrDefaultAsync(d => d.ContratoId == contratoId && d.TipoDocumento == tipoDocumento);

            if (doc == null)
            {
                doc = new DocumentoInquilino
                {
                    ContratoId = contratoId,
                    TipoDocumento = tipoDocumento,
                    Url = $"/documents/{fileName}",
                    EsEntregado = true
                };
                _context.DocumentosInquilino.Add(doc);
            }
            else
            {
                doc.Url = $"/documents/{fileName}";
                doc.EsEntregado = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { url = doc.Url });
        }

        [HttpGet("contrato/{contratoId}")]
        public async Task<IActionResult> GetDocumentsByContrato(int contratoId)
        {
            var docs = await _context.DocumentosInquilino
                .Where(d => d.ContratoId == contratoId)
                .ToListAsync();
            return Ok(docs);
        }

        [HttpPost("scan-contract")]
        public async Task<IActionResult> ScanContract(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No se ha proporcionado un archivo.");

            // Simulación de procesamiento OCR inteligente (retraso para simular IA)
            await Task.Delay(1200);

            string fileName = file.FileName.ToLower();
            string nombreCompleto = "Juan Manuel Pérez Bogado";
            string cedula = "4.567.890";
            string telefono = "0981555444";
            string email = "juan.perez.bogado@gmail.com";
            decimal montoMensual = 1800000;
            DateTime fechaInicio = new DateTime(2026, 07, 01);
            DateTime fechaFin = new DateTime(2027, 06, 30);

            if (fileName.Contains("maria") || fileName.Contains("gomez"))
            {
                nombreCompleto = "María Giménez Gómez";
                cedula = "3.245.678";
                telefono = "0971222333";
                email = "maria.gomez@hotmail.com";
                montoMensual = 2200000;
                fechaInicio = new DateTime(2026, 08, 01);
                fechaFin = new DateTime(2027, 07, 31);
            }
            else if (fileName.Contains("carlos") || fileName.Contains("silva"))
            {
                nombreCompleto = "Carlos Silva Duarte";
                cedula = "5.012.345";
                telefono = "0994888999";
                email = "carlos.silva@outlook.com";
                montoMensual = 1500000;
                fechaInicio = new DateTime(2026, 07, 15);
                fechaFin = new DateTime(2027, 07, 14);
            }

            return Ok(new
            {
                nombreCompleto,
                cedula,
                telefono,
                email,
                montoMensual,
                fechaInicio = fechaInicio.ToString("yyyy-MM-dd"),
                fechaFin = fechaFin.ToString("yyyy-MM-dd"),
                garantia = montoMensual
            });
        }

        [HttpPost("save-url")]
        public async Task<IActionResult> SaveDocumentUrl([FromBody] SaveUrlRequest req)
        {
            if (req == null || req.ContratoId <= 0 || string.IsNullOrEmpty(req.TipoDocumento))
                return BadRequest("Datos inválidos.");

            var doc = await _context.DocumentosInquilino
                .FirstOrDefaultAsync(d => d.ContratoId == req.ContratoId && d.TipoDocumento == req.TipoDocumento);

            if (doc == null)
            {
                doc = new DocumentoInquilino
                {
                    ContratoId = req.ContratoId,
                    TipoDocumento = req.TipoDocumento,
                    Url = req.Url,
                    EsEntregado = !string.IsNullOrEmpty(req.Url)
                };
                _context.DocumentosInquilino.Add(doc);
            }
            else
            {
                doc.Url = req.Url;
                doc.EsEntregado = !string.IsNullOrEmpty(req.Url);
            }

            await _context.SaveChangesAsync();
            return Ok(doc);
        }

        public class SaveUrlRequest
        {
            public int ContratoId { get; set; }
            public string TipoDocumento { get; set; } = null!;
            public string? Url { get; set; }
        }
    }
}
