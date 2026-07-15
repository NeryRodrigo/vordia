using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadosController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public EmpleadosController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{empresaId}")]
        public async Task<ActionResult> GetEmpleados(int empresaId)
        {
            var empleados = await _context.Empleados
                .Where(e => e.EmpresaId == empresaId && e.Activo)
                .ToListAsync();
            return Ok(empleados);
        }

        [HttpPost]
        public async Task<ActionResult> CrearEmpleado([FromBody] Empleado empleado)
        {
            empleado.Activo = true;
            _context.Empleados.Add(empleado);
            await _context.SaveChangesAsync();
            return Ok(empleado);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateEmpleado(int id, [FromBody] Empleado empleadoActualizado)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            empleado.NombreCompleto = empleadoActualizado.NombreCompleto;
            empleado.Documento = empleadoActualizado.Documento;
            empleado.Cargo = empleadoActualizado.Cargo;
            empleado.SalarioBase = empleadoActualizado.SalarioBase;
            empleado.FechaIngreso = empleadoActualizado.FechaIngreso;

            await _context.SaveChangesAsync();
            return Ok(empleado);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmpleado(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            empleado.Activo = false; // Soft delete
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
