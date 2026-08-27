using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FichasIncompletasController : ControllerBase
    {
        private readonly IApplicationDbContext _db;

        public FichasIncompletasController(IApplicationDbContext db)
        {
            _db = db;
        }

        public class UpsertRequest
        {
            [Required]
            public int EmpresaId { get; set; }

            [Required]
            public int UnidadId { get; set; }

            public string? CodigoUnidad { get; set; }
            public string? NombreCondominio { get; set; }
            public string? NombreInquilino { get; set; }
            public string Modo { get; set; } = "edit";

            [Required]
            public string PayloadJson { get; set; } = "{}";

            public int? UsuarioId { get; set; }
            public string? UsuarioNombre { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var list = await _db.FichasIncompletas.AsNoTracking()
                .Where(f => f.EmpresaId == empresaId && f.Activo)
                .OrderByDescending(f => f.FechaActualizacion)
                .Select(f => new
                {
                    f.FichaIncompletaId,
                    f.EmpresaId,
                    f.UnidadId,
                    f.CodigoUnidad,
                    f.NombreCondominio,
                    f.NombreInquilino,
                    f.Modo,
                    f.UsuarioNombre,
                    f.FechaCreacion,
                    f.FechaActualizacion
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("unidad/{unidadId:int}")]
        public async Task<IActionResult> ObtenerPorUnidad(int unidadId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0 || unidadId <= 0)
                return BadRequest(new { mensaje = "empresaId y unidadId requeridos" });

            var item = await _db.FichasIncompletas.AsNoTracking()
                .FirstOrDefaultAsync(f => f.EmpresaId == empresaId && f.UnidadId == unidadId && f.Activo);

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UpsertRequest request)
        {
            if (request.EmpresaId <= 0 || request.UnidadId <= 0)
                return BadRequest(new { mensaje = "empresaId y unidadId requeridos" });
            if (string.IsNullOrWhiteSpace(request.PayloadJson))
                return BadRequest(new { mensaje = "PayloadJson requerido" });

            var modo = string.IsNullOrWhiteSpace(request.Modo) ? "edit" : request.Modo.Trim().ToLowerInvariant();
            if (modo is not ("edit" or "contratar" or "renovar")) modo = "edit";

            var entity = await _db.FichasIncompletas
                .FirstOrDefaultAsync(f => f.EmpresaId == request.EmpresaId && f.UnidadId == request.UnidadId);

            if (entity == null)
            {
                entity = new FichaIncompleta
                {
                    EmpresaId = request.EmpresaId,
                    UnidadId = request.UnidadId,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.FichasIncompletas.Add(entity);
            }

            entity.CodigoUnidad = request.CodigoUnidad?.Trim();
            entity.NombreCondominio = request.NombreCondominio?.Trim();
            entity.NombreInquilino = string.IsNullOrWhiteSpace(request.NombreInquilino)
                ? "Sin nombre"
                : request.NombreInquilino.Trim();
            entity.Modo = modo;
            entity.PayloadJson = request.PayloadJson;
            entity.UsuarioId = request.UsuarioId;
            entity.UsuarioNombre = request.UsuarioNombre?.Trim();
            entity.Activo = true;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new
            {
                entity.FichaIncompletaId,
                entity.EmpresaId,
                entity.UnidadId,
                entity.CodigoUnidad,
                entity.NombreCondominio,
                entity.NombreInquilino,
                entity.Modo,
                entity.FechaActualizacion
            });
        }

        [HttpDelete("unidad/{unidadId:int}")]
        public async Task<IActionResult> Completar(int unidadId, [FromQuery] int empresaId)
        {
            if (empresaId <= 0 || unidadId <= 0)
                return BadRequest(new { mensaje = "empresaId y unidadId requeridos" });

            var entity = await _db.FichasIncompletas
                .FirstOrDefaultAsync(f => f.EmpresaId == empresaId && f.UnidadId == unidadId && f.Activo);
            if (entity == null) return NoContent();

            entity.Activo = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
