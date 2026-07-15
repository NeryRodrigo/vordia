using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesoreriaController : ControllerBase
    {
        private readonly ITesoreriaService _tesoreriaService;

        public TesoreriaController(ITesoreriaService tesoreriaService)
        {
            _tesoreriaService = tesoreriaService;
        }

        [HttpGet("empresa/{empresaId}/flujo")]
        public async Task<ActionResult> GetFlujoCaja(int empresaId, [FromQuery] int mes, [FromQuery] int anio)
        {
            try
            {
                var flujo = await _tesoreriaService.GetFlujoCajaAsync(empresaId, mes, anio);
                return Ok(flujo);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("empresa/{empresaId}/movimiento/{tipo}/{idOrigen}")]
        public async Task<ActionResult> EliminarMovimiento(int empresaId, string tipo, int idOrigen)
        {
            await _tesoreriaService.EliminarMovimientoAsync(empresaId, tipo, idOrigen);
            return Ok(new { message = "Movimiento anulado y stock revertido." });
        }

        public class EditMovimientoDto
        {
            public decimal Monto { get; set; }
            public string Descripcion { get; set; } = string.Empty;
        }

        [HttpPut("empresa/{empresaId}/movimiento/{tipo}/{idOrigen}")]
        public async Task<ActionResult> EditarMovimiento(int empresaId, string tipo, int idOrigen, [FromBody] EditMovimientoDto dto)
        {
            await _tesoreriaService.EditarMovimientoAsync(empresaId, tipo, idOrigen, dto.Monto, dto.Descripcion);
            return Ok(new { message = "Movimiento actualizado." });
        }
    }
}
