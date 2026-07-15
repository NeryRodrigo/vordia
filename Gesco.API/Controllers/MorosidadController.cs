using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MorosidadController : ControllerBase
    {
        private readonly IMorosidadService _morosidadService;

        public MorosidadController(IMorosidadService morosidadService)
        {
            _morosidadService = morosidadService;
        }

        public class RegistrarMorosidadRequest
        {
            public int InquilinoId { get; set; }
            public int UnidadId { get; set; }
            public decimal MontoTotal { get; set; }
            public int CantidadCuotas { get; set; }
            public decimal MontoInteresDiario { get; set; }
            public string Observaciones { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] RegistrarMorosidadRequest req)
        {
            try
            {
                var morosidad = await _morosidadService.RegistrarMorosidadAsync(req.InquilinoId, req.UnidadId, req.MontoTotal, req.CantidadCuotas, req.Observaciones, req.MontoInteresDiario);
                return Ok(morosidad);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? unidadId)
        {
            var data = await _morosidadService.ObtenerMorosidadesAsync(unidadId);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetalle(int id)
        {
            var data = await _morosidadService.ObtenerDetalleMorosidadAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        public class CobrarCuotaRequest
        {
            public string MetodoPago { get; set; }
            public int ContratoActualId { get; set; }
            public string? Notas { get; set; }
        }

        [HttpPost("cuota/{cuotaId}/cobrar")]
        public async Task<IActionResult> CobrarCuota(int cuotaId, [FromBody] CobrarCuotaRequest req)
        {
            try
            {
                var cuota = await _morosidadService.CobrarCuotaAsync(cuotaId, req.MetodoPago, req.ContratoActualId, req.Notas);
                return Ok(cuota);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        public class SeguimientoRequest
        {
            public string Observacion { get; set; }
        }

        [HttpPost("cuota/{cuotaId}/seguimiento")]
        public async Task<IActionResult> AgregarSeguimiento(int cuotaId, [FromBody] SeguimientoRequest req)
        {
            try
            {
                var seg = await _morosidadService.AgregarSeguimientoAsync(cuotaId, req.Observacion);
                return Ok(seg);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
