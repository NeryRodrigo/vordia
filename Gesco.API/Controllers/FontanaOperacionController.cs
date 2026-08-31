using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gesco.API.Controllers
{
    [Route("api/Fontana")]
    [ApiController]
    public class FontanaOperacionController : ControllerBase
    {
        private readonly IApplicationDbContext _db;

        public FontanaOperacionController(IApplicationDbContext db)
        {
            _db = db;
        }

        // ─── Finanzas (movimientos) ─────────────────────────────────

        public class MovimientoRequest
        {
            [Required] public int EmpresaId { get; set; }
            [Required] public string Tipo { get; set; } = "ingreso";
            [Required] public string Propiedad { get; set; } = "Fontana 1";
            [Required] public string Categoria { get; set; } = "Otros ingresos";
            [Required] public string Concepto { get; set; } = "";
            [Required] public decimal Monto { get; set; }
            public DateTime? Fecha { get; set; }
            public string? MetodoPago { get; set; }
            public string? Origen { get; set; }
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> ListarMovimientos(
            [FromQuery] int empresaId,
            [FromQuery] string? propiedad = null,
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? tipo = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var q = _db.FontanaMovimientos.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId && m.Activo);

            if (!string.IsNullOrWhiteSpace(propiedad) &&
                !propiedad.Equals("Todas", StringComparison.OrdinalIgnoreCase))
            {
                var p = propiedad.Trim();
                q = q.Where(m => m.Propiedad == p);
            }

            if (!string.IsNullOrWhiteSpace(tipo) &&
                !tipo.Equals("todos", StringComparison.OrdinalIgnoreCase))
            {
                var t = tipo.Trim().ToLowerInvariant();
                q = q.Where(m => m.Tipo == t);
            }

            if (DateTime.TryParse(from, out var fFrom))
                q = q.Where(m => m.Fecha >= fFrom.Date);
            if (DateTime.TryParse(to, out var fTo))
                q = q.Where(m => m.Fecha <= fTo.Date);

            var list = await q.OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.MovimientoId)
                .Select(m => new
                {
                    id = m.MovimientoId.ToString(),
                    movimientoId = m.MovimientoId,
                    empresaId = m.EmpresaId,
                    tipo = m.Tipo,
                    propiedad = m.Propiedad,
                    categoria = m.Categoria,
                    concepto = m.Concepto,
                    monto = m.Monto,
                    fecha = m.Fecha.ToString("yyyy-MM-dd"),
                    metodoPago = m.MetodoPago,
                    origen = m.Origen,
                    createdAt = m.FechaCreacion
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("movimientos")]
        public async Task<IActionResult> CrearMovimiento([FromBody] MovimientoRequest req)
        {
            if (req.EmpresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            if (string.IsNullOrWhiteSpace(req.Concepto))
                return BadRequest(new { mensaje = "concepto requerido" });
            if (req.Monto <= 0) return BadRequest(new { mensaje = "monto inválido" });

            var tipoRaw = string.IsNullOrWhiteSpace(req.Tipo) ? "ingreso" : req.Tipo.Trim();
            var tipo = tipoRaw.ToLowerInvariant();
            if (tipo != "ingreso" && tipo != "gasto") tipo = "ingreso";

            var entity = new FontanaMovimiento
            {
                EmpresaId = req.EmpresaId,
                Tipo = tipo,
                Propiedad = string.IsNullOrWhiteSpace(req.Propiedad) ? "Fontana 1" : req.Propiedad.Trim(),
                Categoria = string.IsNullOrWhiteSpace(req.Categoria) ? "Otros ingresos" : req.Categoria.Trim(),
                Concepto = req.Concepto.Trim(),
                Monto = req.Monto,
                Fecha = (req.Fecha ?? DateTime.UtcNow).Date,
                MetodoPago = req.MetodoPago?.Trim(),
                Origen = req.Origen?.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _db.FontanaMovimientos.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = entity.MovimientoId.ToString(),
                movimientoId = entity.MovimientoId,
                empresaId = entity.EmpresaId,
                tipo = entity.Tipo,
                propiedad = entity.Propiedad,
                categoria = entity.Categoria,
                concepto = entity.Concepto,
                monto = entity.Monto,
                fecha = entity.Fecha.ToString("yyyy-MM-dd"),
                metodoPago = entity.MetodoPago,
                origen = entity.Origen,
                createdAt = entity.FechaCreacion
            });
        }

        [HttpDelete("movimientos/{id:int}")]
        public async Task<IActionResult> EliminarMovimiento(int id, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var entity = await _db.FontanaMovimientos
                .FirstOrDefaultAsync(m => m.MovimientoId == id && m.EmpresaId == empresaId && m.Activo);
            if (entity == null) return NoContent();
            entity.Activo = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ─── Personal ───────────────────────────────────────────────

        public class PersonalRequest
        {
            [Required] public int EmpresaId { get; set; }
            public int? PersonalId { get; set; }
            [Required] public string Nombre { get; set; } = "";
            [Required] public string Funcion { get; set; } = "";
            public string? Telefono { get; set; }
            public List<string>? Propiedades { get; set; }
            public decimal Salario { get; set; }
            public DateTime? FechaPago { get; set; }
            public string EstadoPago { get; set; } = "PENDIENTE";
        }

        private static object MapPersonal(FontanaPersonal e)
        {
            List<string> props;
            try
            {
                props = JsonSerializer.Deserialize<List<string>>(e.PropiedadesJson) ?? new List<string>();
            }
            catch
            {
                props = new List<string>();
            }

            return new
            {
                id = e.PersonalId.ToString(),
                personalId = e.PersonalId,
                empresaId = e.EmpresaId,
                nombre = e.Nombre,
                funcion = e.Funcion,
                telefono = e.Telefono,
                propiedades = props,
                salario = e.Salario,
                fechaPago = e.FechaPago.ToString("yyyy-MM-dd"),
                estadoPago = e.EstadoPago,
                createdAt = e.FechaCreacion,
                updatedAt = e.FechaActualizacion
            };
        }

        [HttpGet("personal")]
        public async Task<IActionResult> ListarPersonal([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var list = await _db.FontanaPersonal.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
            return Ok(list.Select(MapPersonal));
        }

        [HttpPost("personal")]
        public async Task<IActionResult> UpsertPersonal([FromBody] PersonalRequest req)
        {
            if (req.EmpresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Funcion))
                return BadRequest(new { mensaje = "nombre y función requeridos" });

            var props = (req.Propiedades ?? new List<string>())
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (props.Count == 0) props.Add("Fontana 1");

            var estadoRaw = string.IsNullOrWhiteSpace(req.EstadoPago) ? "PENDIENTE" : req.EstadoPago.Trim();
            var estado = estadoRaw.ToUpperInvariant();
            if (estado != "PAGADO" && estado != "PENDIENTE") estado = "PENDIENTE";

            FontanaPersonal? entity = null;
            if (req.PersonalId is > 0)
            {
                entity = await _db.FontanaPersonal
                    .FirstOrDefaultAsync(p => p.PersonalId == req.PersonalId && p.EmpresaId == req.EmpresaId && p.Activo);
            }

            if (entity == null)
            {
                entity = new FontanaPersonal
                {
                    EmpresaId = req.EmpresaId,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.FontanaPersonal.Add(entity);
            }

            entity.Nombre = req.Nombre.Trim();
            entity.Funcion = req.Funcion.Trim();
            entity.Telefono = string.IsNullOrWhiteSpace(req.Telefono) ? null : req.Telefono.Trim();
            entity.PropiedadesJson = JsonSerializer.Serialize(props);
            entity.Salario = req.Salario < 0 ? 0 : req.Salario;
            entity.FechaPago = (req.FechaPago ?? DateTime.UtcNow).Date;
            entity.EstadoPago = estado;
            entity.Activo = true;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(MapPersonal(entity));
        }

        [HttpDelete("personal/{id:int}")]
        public async Task<IActionResult> EliminarPersonal(int id, [FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var entity = await _db.FontanaPersonal
                .FirstOrDefaultAsync(p => p.PersonalId == id && p.EmpresaId == empresaId && p.Activo);
            if (entity == null) return NoContent();
            entity.Activo = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Marca salario PAGADO y genera gastos de Personal (uno por Fontana asignada).</summary>
        [HttpPost("personal/{id:int}/pagar")]
        public async Task<IActionResult> PagarSalario(int id, [FromQuery] int empresaId, [FromQuery] string? fecha = null)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });

            var emp = await _db.FontanaPersonal
                .FirstOrDefaultAsync(p => p.PersonalId == id && p.EmpresaId == empresaId && p.Activo);
            if (emp == null) return NotFound(new { mensaje = "Empleado no encontrado" });
            if (emp.EstadoPago == "PAGADO")
                return BadRequest(new { mensaje = "El salario ya figura como pagado" });

            List<string> props;
            try
            {
                props = JsonSerializer.Deserialize<List<string>>(emp.PropiedadesJson) ?? new List<string>();
            }
            catch
            {
                props = new List<string>();
            }
            if (props.Count == 0) props.Add("Fontana 1");

            var fechaPago = DateTime.TryParse(fecha, out var fp) ? fp.Date : DateTime.UtcNow.Date;
            var parte = Math.Round(emp.Salario / props.Count, 0, MidpointRounding.AwayFromZero);
            var restante = emp.Salario;

            for (var i = 0; i < props.Count; i++)
            {
                var monto = i == props.Count - 1 ? restante : parte;
                restante -= monto;
                _db.FontanaMovimientos.Add(new FontanaMovimiento
                {
                    EmpresaId = empresaId,
                    Tipo = "gasto",
                    Propiedad = props[i],
                    Categoria = "Personal",
                    Concepto = $"Salario · {emp.Nombre} ({emp.Funcion})",
                    Monto = monto,
                    Fecha = fechaPago,
                    MetodoPago = "Transferencia",
                    Origen = $"salario:{emp.PersonalId}",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            emp.EstadoPago = "PAGADO";
            emp.FechaPago = fechaPago;
            emp.FechaActualizacion = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Salario pagado y gasto de personal registrado", personal = MapPersonal(emp) });
        }

        [HttpPost("personal/reiniciar-mes")]
        public async Task<IActionResult> ReiniciarMes([FromQuery] int empresaId)
        {
            if (empresaId <= 0) return BadRequest(new { mensaje = "empresaId requerido" });
            var list = await _db.FontanaPersonal
                .Where(p => p.EmpresaId == empresaId && p.Activo)
                .ToListAsync();
            foreach (var e in list)
            {
                e.EstadoPago = "PENDIENTE";
                e.FechaActualizacion = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Estados reiniciados a PENDIENTE", cantidad = list.Count });
        }
    }
}
