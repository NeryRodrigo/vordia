using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("condominio/{empresaId}")]
        public async Task<IActionResult> GetDashboardCondominio(int empresaId)
        {
            var data = await _dashboardService.GetDashboardDataAsync(empresaId);
            return Ok(data);
        }
    }
}
