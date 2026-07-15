using System.Collections.Generic;
using System.Threading.Tasks;
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IConfiguracionTimbradoService
    {
        Task<IEnumerable<ConfiguracionTimbrado>> GetAllByEmpresaAsync(int empresaId);
        Task<ConfiguracionTimbrado?> GetByIdAsync(int id);
        Task<ConfiguracionTimbrado> CreateAsync(ConfiguracionTimbrado configuracion);
        Task UpdateAsync(ConfiguracionTimbrado configuracion);
        Task DeleteAsync(int id);
    }
}
