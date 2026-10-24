using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// Estudio de precios de VORDIA: los prospectos responden una encuesta pública (sin cuenta)
    /// y el equipo de HQ ve las respuestas y el análisis por producto.
    /// </summary>
    [ApiController]
    public class EstudioPreciosController : ControllerBase
    {
        private static readonly string[] Productos = { "habitat", "stay", "dine", "retail", "boutique", "ops", "agro", "conta" };
        private readonly IApplicationDbContext _db;

        public EstudioPreciosController(IApplicationDbContext db) => _db = db;

        public class RespuestaRequest
        {
            public string Producto { get; set; } = "";
            public string? Origen { get; set; }
            public int? Tamano { get; set; }
            public string? Ciudad { get; set; }
            public string? HerramientaActual { get; set; }
            public decimal? GastoActualGs { get; set; }
            public string? ProblemaPrincipal { get; set; }
            public string? Decisor { get; set; }
            public decimal MuyBaratoGs { get; set; }
            public decimal BaratoGs { get; set; }
            public decimal CaroGs { get; set; }
            public decimal MuyCaroGs { get; set; }
            public decimal PrecioMostradoGs { get; set; }
            public int IntencionCompra { get; set; }
            public string? Comentario { get; set; }
            public string? ContactoNombre { get; set; }
            public string? ContactoEmpresa { get; set; }
            public string? ContactoDato { get; set; }
            public bool AceptaContacto { get; set; }
            /// <summary>Honeypot: solo un bot lo completa.</summary>
            public string? Sitio { get; set; }
        }

        private static string? Corto(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);

        [HttpPost("api/public/estudio-precios")]
        [AllowAnonymous]
        [EnableRateLimiting("leads")]
        public async Task<IActionResult> Responder([FromBody] RespuestaRequest r)
        {
            if (!string.IsNullOrWhiteSpace(r.Sitio)) return Ok(new { mensaje = "Gracias" });
            var producto = (r.Producto ?? "").Trim().ToLowerInvariant();
            if (!Productos.Contains(producto)) return BadRequest(new { mensaje = "Producto inválido" });
            if (r.MuyBaratoGs <= 0 || r.BaratoGs <= r.MuyBaratoGs || r.CaroGs <= r.BaratoGs || r.MuyCaroGs <= r.CaroGs)
                return BadRequest(new { mensaje = "Los cuatro precios tienen que ir de menor a mayor." });
            if (r.MuyCaroGs > 100_000_000) return BadRequest(new { mensaje = "Revisá los montos: parecen demasiado altos." });
            if (r.IntencionCompra is < 1 or > 5 || r.PrecioMostradoGs <= 0) return BadRequest(new { mensaje = "Falta la intención de compra." });

            var empresaId = await _db.Empresas.Where(e => e.Rubro == "VORDIA").OrderBy(e => e.EmpresaId).Select(e => e.EmpresaId).FirstOrDefaultAsync();
            if (empresaId == 0) return StatusCode(503, new { mensaje = "Estudio no disponible" });

            _db.HqEstudioPreciosRespuestas.Add(new HqEstudioPrecioRespuesta
            {
                EmpresaId = empresaId,
                Producto = producto,
                Origen = Corto(r.Origen, 60),
                Tamano = r.Tamano is > 0 and < 1_000_000 ? r.Tamano : null,
                Ciudad = Corto(r.Ciudad, 80),
                HerramientaActual = Corto(r.HerramientaActual, 40),
                GastoActualGs = r.GastoActualGs is >= 0 and < 100_000_000 ? r.GastoActualGs : null,
                ProblemaPrincipal = Corto(r.ProblemaPrincipal, 80),
                Decisor = Corto(r.Decisor, 20),
                MuyBaratoGs = r.MuyBaratoGs,
                BaratoGs = r.BaratoGs,
                CaroGs = r.CaroGs,
                MuyCaroGs = r.MuyCaroGs,
                PrecioMostradoGs = r.PrecioMostradoGs,
                IntencionCompra = r.IntencionCompra,
                Comentario = Corto(r.Comentario, 1000),
                // Datos de contacto solo con consentimiento explícito
                ContactoNombre = r.AceptaContacto ? Corto(r.ContactoNombre, 150) : null,
                ContactoEmpresa = r.AceptaContacto ? Corto(r.ContactoEmpresa, 150) : null,
                ContactoDato = r.AceptaContacto ? Corto(r.ContactoDato, 150) : null,
                AceptaContacto = r.AceptaContacto,
                Fecha = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "¡Gracias! Tu respuesta nos ayuda a definir precios justos." });
        }

        [HttpGet("api/Mercado/estudio-precios")]
        public async Task<IActionResult> Listar([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var filas = await _db.HqEstudioPreciosRespuestas.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.Fecha)
                .Take(5000)
                .ToListAsync();
            return Ok(filas);
        }

        [HttpDelete("api/Mercado/estudio-precios/{respuestaId:int}")]
        public async Task<IActionResult> Eliminar(int respuestaId, [FromQuery] int empresaId)
        {
            var r = await _db.HqEstudioPreciosRespuestas.FirstOrDefaultAsync(x => x.RespuestaId == respuestaId && x.EmpresaId == empresaId);
            if (r == null) return NotFound();
            _db.HqEstudioPreciosRespuestas.Remove(r);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
