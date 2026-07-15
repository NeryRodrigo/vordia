using Gesco.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IModuloService
    {
        Task<List<ModuloDto>> GetModulosPorRubroAsync(string rubro);
        Task<List<ModuloDto>> GetModulosByEmpresaIdAsync(int empresaId);
    }
}
