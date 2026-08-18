using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnidadesCondominioController : ControllerBase
    {
        private readonly IUnidadCondominioService _unidadService;

        public UnidadesCondominioController(IUnidadCondominioService unidadService)
        {
            _unidadService = unidadService;
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<UnidadCondominio>>> GetUnidades(int empresaId)
        {
            var unidades = await _unidadService.GetAllByEmpresaAsync(empresaId);
            return Ok(unidades);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UnidadCondominio>> GetUnidad(int id)
        {
            var unidad = await _unidadService.GetByIdAsync(id);
            if (unidad == null) return NotFound();
            return Ok(unidad);
        }

        [HttpPost("seed/{empresaId}")]
        public async Task<IActionResult> SeedAguz(int empresaId)
        {
            await _unidadService.SeedAguzDuplexesAsync(empresaId);
            return Ok(new { message = "Se han generado las 17 unidades para Condominio Aguz" });
        }

        [HttpPost]
        public async Task<ActionResult<UnidadCondominio>> PostUnidad(UnidadCondominio unidad)
        {
            var nuevaUnidad = await _unidadService.CreateAsync(unidad);
            return CreatedAtAction(nameof(GetUnidad), new { id = nuevaUnidad.UnidadId }, nuevaUnidad);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUnidad(int id, [FromBody] UpdateUnidadRequest? request)
        {
            if (request == null) return BadRequest(new { message = "Body vacío" });

            var existing = await _unidadService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = $"Unidad {id} no encontrada" });

            var patch = new UnidadCondominio
            {
                UnidadId = id,
                EmpresaId = request.EmpresaId is > 0 ? request.EmpresaId.Value : existing.EmpresaId,
                NombreCondominio = string.IsNullOrWhiteSpace(request.NombreCondominio)
                    ? existing.NombreCondominio
                    : request.NombreCondominio.Trim(),
                CodigoUnidad = string.IsNullOrWhiteSpace(request.CodigoUnidad)
                    ? existing.CodigoUnidad
                    : request.CodigoUnidad.Trim(),
                Estado = string.IsNullOrWhiteSpace(request.Estado)
                    ? existing.Estado
                    : request.Estado.Trim(),
                NisAnde = request.NisAnde,
                MedidorAgua = request.MedidorAgua,
                Observaciones = request.Observaciones ?? existing.Observaciones,
            };

            await _unidadService.UpdateAsync(patch);
            var updated = await _unidadService.GetByIdAsync(id);
            if (updated == null) return NotFound();

            // Respuesta plana (sin grafo) para el front
            return Ok(new
            {
                unidadId = updated.UnidadId,
                empresaId = updated.EmpresaId,
                nombreCondominio = updated.NombreCondominio,
                codigoUnidad = updated.CodigoUnidad,
                estado = updated.Estado,
                nisAnde = updated.NisAnde,
                medidorAgua = updated.MedidorAgua,
                observaciones = updated.Observaciones,
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUnidad(int id)
        {
            await _unidadService.DeleteAsync(id);
            return NoContent();
        }
    }
}
