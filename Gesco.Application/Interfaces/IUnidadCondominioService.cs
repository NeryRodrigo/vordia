using System.Collections.Generic;
using System.Threading.Tasks;
using Gesco.Domain.Entities;

namespace Gesco.Application.Interfaces
{
    public interface IUnidadCondominioService
    {
        Task<IEnumerable<UnidadCondominio>> GetAllByEmpresaAsync(int empresaId);
        Task<UnidadCondominio?> GetByIdAsync(int unidadId);
        Task<UnidadCondominio> CreateAsync(UnidadCondominio unidad);
        Task UpdateAsync(UnidadCondominio unidad);
        Task DeleteAsync(int unidadId);
        Task SeedAguzDuplexesAsync(int empresaId);
    }
}
