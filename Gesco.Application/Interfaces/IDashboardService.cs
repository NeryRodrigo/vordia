using Gesco.Application.DTOs;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardCondominioDto> GetDashboardDataAsync(int empresaId);
    }
}
