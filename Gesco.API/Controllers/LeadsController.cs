using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    /// <summary>
    /// Endpoint público del formulario de vordia.com.py. Los leads caen en el pipeline
    /// del CRM Vordia como Prospecto y se ven en Vordia HQ.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LeadsController : ControllerBase
    {
        private readonly ICrmService _crm;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(ICrmService crm, ILogger<LeadsController> logger)
        {
            _crm = crm;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("leads")]
        public async Task<IActionResult> Crear([FromBody] CrmLeadWebRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { mensaje = "Revisá los datos del formulario." });

            // Honeypot: solo un bot completa este campo
            if (!string.IsNullOrWhiteSpace(request.Sitio))
                return Ok(new { mensaje = "Recibido" });

            try
            {
                var lead = await _crm.CrearLeadWebAsync(request);
                _logger.LogInformation("Lead web registrado: ClienteId={ClienteId} Empresa={Empresa}",
                    lead.ClienteId, lead.RazonSocial);
                return Ok(new { mensaje = "Recibido", clienteId = lead.ClienteId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "No se pudo registrar el lead web");
                return StatusCode(500, new { mensaje = "No pudimos registrar la solicitud. Escribinos por WhatsApp." });
            }
        }
    }
}
