using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gesco.Domain.Entities;
using Gesco.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GastosController : ControllerBase
    {
        private readonly GescoDbContext _context;

        public GastosController(GescoDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int empresaId, [FromQuery] int mes, [FromQuery] int anio)
        {
            // 1. Ingresos: Suma de todos los Pagos/Recibos del mes que son de Alquiler
            var ingresos = await _context.PagosRecibos
                .Where(p => p.Contrato.Unidad.EmpresaId == empresaId 
                         && p.FechaPago.Month == mes 
                         && p.FechaPago.Year == anio)
                .SumAsync(p => p.MontoPagado);

            // 2. Gastos: Suma de todos los gastos registrados en ese mes/anio para unidades de la empresa
            var gastosList = await _context.GastosMantenimiento
                .Include(g => g.Unidad)
                .Where(g => g.Unidad.EmpresaId == empresaId 
                         && g.Fecha.Month == mes 
                         && g.Fecha.Year == anio)
                .ToListAsync();

            var egresos = gastosList.Sum(g => g.Monto);

            var gananciaNeta = ingresos - egresos;
            
            decimal rentabilidad = 0;
            if (ingresos > 0)
            {
                rentabilidad = ((ingresos - egresos) / ingresos) * 100;
            }

            // Alerta si los gastos superan el 50%
            bool alertaGastosAltos = false;
            if (ingresos > 0 && (egresos / ingresos) >= 0.5m)
            {
                alertaGastosAltos = true;
            }

            return Ok(new
            {
                ingresosTotales = ingresos,
                gastosTotales = egresos,
                gananciaNeta = gananciaNeta,
                rentabilidadPorcentaje = rentabilidad,
                alertaGastosAltos = alertaGastosAltos,
                gastosDetalle = gastosList.Select(g => new
                {
                    g.GastoId,
                    g.Descripcion,
                    g.Monto,
                    Fecha = g.Fecha.ToString("yyyy-MM-dd"),
                    g.Proveedor,
                    g.NroComprobante,
                    UnidadNombre = g.Unidad.NombreCondominio + " - " + System.Text.RegularExpressions.Regex.Replace(g.Unidad.CodigoUnidad ?? "", @"\bD\s*-?\s*(\d+)\b", m => $"Dúplex {int.Parse(m.Groups[1].Value):D2}", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateGasto([FromBody] CrearGastoRequest req)
        {
            if (req.UnidadId == 0) return BadRequest("Debe seleccionar una unidad válida.");

            int finalUnidadId = req.UnidadId;

            if (req.UnidadId == -1) 
            {
                // Buscar o crear la unidad virtual "Condominio General"
                var virtualUnit = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.CodigoUnidad == "GENERAL" && u.EmpresaId == 7); // Asumimos empresaId de contexto
                if (virtualUnit == null) {
                    virtualUnit = new UnidadCondominio {
                        EmpresaId = 7, // idealmente vendría en el request
                        CodigoUnidad = "GENERAL",
                        NombreCondominio = "Todo el Condominio",
                        Estado = "VIRTUAL"
                    };
                    _context.UnidadesCondominio.Add(virtualUnit);
                    await _context.SaveChangesAsync();
                }
                finalUnidadId = virtualUnit.UnidadId;
            }

            var gasto = new GastoMantenimiento
            {
                UnidadId = finalUnidadId,
                Descripcion = req.Descripcion,
                Monto = req.Monto,
                Fecha = req.Fecha ?? DateTime.UtcNow,
                Proveedor = req.Proveedor,
                NroComprobante = req.NroComprobante,
                MetodoPago = req.MetodoPago
            };

            _context.GastosMantenimiento.Add(gasto);
            await _context.SaveChangesAsync();

            return Ok(gasto);
        }

        public class CrearGastoRequest
        {
            public int UnidadId { get; set; }
            public string Descripcion { get; set; } = null!;
            public decimal Monto { get; set; }
            public DateTime? Fecha { get; set; }
            public string? Proveedor { get; set; }
            public string? NroComprobante { get; set; }
            public string? MetodoPago { get; set; }
        }
    }
}
