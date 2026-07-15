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
    public class ContactosController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public ContactosController(IApplicationDbContext context)
        {
            _context = context;
        }

        // --- CLIENTES (PERSONAS) ---

        [HttpGet("clientes/{empresaId}")]
        public async Task<ActionResult> GetClientes(int empresaId)
        {
            var clientes = await _context.Personas
                .Where(p => p.EmpresaId == empresaId && p.EsCliente)
                .ToListAsync();
            return Ok(clientes);
        }

        [HttpPost("clientes")]
        public async Task<ActionResult> CrearCliente([FromBody] Persona cliente)
        {
            cliente.EsCliente = true;
            _context.Personas.Add(cliente);
            await _context.SaveChangesAsync();
            return Ok(cliente);
        }

        [HttpPut("clientes/{id}")]
        public async Task<ActionResult> UpdateCliente(int id, [FromBody] Persona clienteActualizado)
        {
            var cliente = await _context.Personas.FindAsync(id);
            if (cliente == null || !cliente.EsCliente) return NotFound();

            cliente.RazonSocial = clienteActualizado.RazonSocial;
            cliente.Documento = clienteActualizado.Documento;
            cliente.Telefono = clienteActualizado.Telefono;
            cliente.Email = clienteActualizado.Email;

            await _context.SaveChangesAsync();
            return Ok(cliente);
        }

        [HttpDelete("clientes/{id}")]
        public async Task<ActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Personas.FindAsync(id);
            if (cliente == null || !cliente.EsCliente) return NotFound();

            _context.Personas.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- PROVEEDORES ---

        [HttpGet("proveedores/{empresaId}")]
        public async Task<ActionResult> GetProveedores(int empresaId)
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .ToListAsync();
            return Ok(proveedores);
        }

        [HttpPost("proveedores")]
        public async Task<ActionResult> CrearProveedor([FromBody] ProveedorDtoRequest dto)
        {
            var proveedor = new Proveedor
            {
                EmpresaId = dto.EmpresaId,
                RazonSocial = dto.RazonSocial,
                Ruc = dto.Ruc,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Activo = true
            };
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return Ok(proveedor);
        }
    }

    public class ProveedorDtoRequest
    {
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
    }
}
