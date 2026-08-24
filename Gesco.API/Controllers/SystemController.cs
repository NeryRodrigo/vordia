using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gesco.API.Services;

namespace Gesco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly ISessionEpochService _epoch;

        public SystemController(ISessionEpochService epoch)
        {
            _epoch = epoch;
        }

        /// <summary>
        /// Versión / epoch de sesión. El front lo consulta para avisar y limpiar sesiones tras un deploy.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("version")]
        public IActionResult Version()
        {
            return Ok(new
            {
                app = "Vordia",
                sessionEpoch = _epoch.Epoch,
                startedAtUtc = _epoch.StartedAtUtc.ToString("o"),
            });
        }
    }
}
