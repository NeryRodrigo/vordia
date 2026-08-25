using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class CrmFinanzasService : ICrmFinanzasService
    {
        private static readonly string[] EstadosPago = { "Al dia", "Pendiente", "Vencido", "Pagado" };
        private static readonly string[] EstadosContrato = { "Borrador", "Activo", "Suspendido", "Finalizado" };
        private static readonly string[] Periodicidades = { "Mensual", "Anual" };

        private readonly IApplicationDbContext _context;

        public CrmFinanzasService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CrmContratoDto>> ListarContratosAsync(int empresaId, string? estadoContrato = null)
        {
            var query = _context.CrmContratos
                .AsNoTracking()
                .Include(c => c.Cliente)
                .Where(c => c.EmpresaId == empresaId && c.Activo);

            if (!string.IsNullOrWhiteSpace(estadoContrato))
                query = query.Where(c => c.EstadoContrato == estadoContrato.Trim());

            var list = await query.OrderByDescending(c => c.FechaCreacion).ToListAsync();
            var ids = list.Select(c => c.ContratoId).ToList();

            var pagoAgg = await _context.CrmPagos.AsNoTracking()
                .Where(p => ids.Contains(p.ContratoId))
                .GroupBy(p => p.ContratoId)
                .Select(g => new { ContratoId = g.Key, Cant = g.Count(), Total = g.Sum(x => x.Monto) })
                .ToDictionaryAsync(x => x.ContratoId, x => x);

            return list.Select(c => MapContrato(
                c,
                pagoAgg.GetValueOrDefault(c.ContratoId)?.Cant ?? 0,
                pagoAgg.GetValueOrDefault(c.ContratoId)?.Total ?? 0m
            )).ToList();
        }

        public async Task<CrmContratoDto?> ObtenerContratoAsync(int contratoId, int empresaId)
        {
            var c = await _context.CrmContratos.AsNoTracking()
                .Include(x => x.Cliente)
                .FirstOrDefaultAsync(x => x.ContratoId == contratoId && x.EmpresaId == empresaId && x.Activo);
            if (c == null) return null;

            var pagos = await _context.CrmPagos.AsNoTracking()
                .Where(p => p.ContratoId == contratoId)
                .ToListAsync();
            return MapContrato(c, pagos.Count, pagos.Sum(p => p.Monto));
        }

        public async Task<CrmContratoDto> CrearContratoAsync(CrmContratoUpsertRequest request)
        {
            await ValidarClienteAsync(request.EmpresaId, request.ClienteId);

            var entity = new CrmContrato
            {
                EmpresaId = request.EmpresaId,
                ClienteId = request.ClienteId,
                SistemaServicio = request.SistemaServicio.Trim(),
                FeeAcordado = request.FeeAcordado,
                FeeMensual = request.FeeMensual,
                FeeAnual = request.FeeAnual > 0 ? request.FeeAnual : request.FeeMensual * 12,
                FechaInicio = request.FechaInicio.Date,
                ProximoVencimiento = request.ProximoVencimiento?.Date,
                EstadoPago = Normalizar(request.EstadoPago, EstadosPago, "Pendiente"),
                EstadoContrato = Normalizar(request.EstadoContrato, EstadosContrato, "Activo"),
                EsRecurrente = request.EsRecurrente,
                Periodicidad = Normalizar(request.Periodicidad, Periodicidades, "Mensual"),
                Notas = request.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmContratos.Add(entity);
            await _context.SaveChangesAsync();

            entity = await _context.CrmContratos.AsNoTracking()
                .Include(c => c.Cliente)
                .FirstAsync(c => c.ContratoId == entity.ContratoId);

            return MapContrato(entity, 0, 0);
        }

        public async Task<CrmContratoDto?> ActualizarContratoAsync(int contratoId, CrmContratoUpsertRequest request)
        {
            var entity = await _context.CrmContratos
                .FirstOrDefaultAsync(c => c.ContratoId == contratoId && c.EmpresaId == request.EmpresaId && c.Activo);
            if (entity == null) return null;

            await ValidarClienteAsync(request.EmpresaId, request.ClienteId);

            entity.ClienteId = request.ClienteId;
            entity.SistemaServicio = request.SistemaServicio.Trim();
            entity.FeeAcordado = request.FeeAcordado;
            entity.FeeMensual = request.FeeMensual;
            entity.FeeAnual = request.FeeAnual > 0 ? request.FeeAnual : request.FeeMensual * 12;
            entity.FechaInicio = request.FechaInicio.Date;
            entity.ProximoVencimiento = request.ProximoVencimiento?.Date;
            entity.EstadoPago = Normalizar(request.EstadoPago, EstadosPago, entity.EstadoPago);
            entity.EstadoContrato = Normalizar(request.EstadoContrato, EstadosContrato, entity.EstadoContrato);
            entity.EsRecurrente = request.EsRecurrente;
            entity.Periodicidad = Normalizar(request.Periodicidad, Periodicidades, entity.Periodicidad);
            entity.Notas = request.Notas;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await ObtenerContratoAsync(contratoId, request.EmpresaId);
        }

        public async Task<bool> EliminarContratoAsync(int contratoId, int empresaId)
        {
            var entity = await _context.CrmContratos
                .FirstOrDefaultAsync(c => c.ContratoId == contratoId && c.EmpresaId == empresaId && c.Activo);
            if (entity == null) return false;
            entity.Activo = false;
            entity.EstadoContrato = "Finalizado";
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmPagoDto>> ListarPagosAsync(int empresaId, int? contratoId = null)
        {
            var query = _context.CrmPagos.AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Contrato)
                .Where(p => p.EmpresaId == empresaId);

            if (contratoId.HasValue)
                query = query.Where(p => p.ContratoId == contratoId.Value);

            var list = await query.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.PagoId).ToListAsync();
            return list.Select(MapPago).ToList();
        }

        public async Task<CrmPagoDto> RegistrarPagoAsync(CrmPagoCreateRequest request)
        {
            var contrato = await _context.CrmContratos
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.ContratoId == request.ContratoId && c.EmpresaId == request.EmpresaId && c.Activo);
            if (contrato == null)
                throw new InvalidOperationException("Contrato no encontrado.");

            var pago = new CrmPago
            {
                EmpresaId = request.EmpresaId,
                ContratoId = request.ContratoId,
                ClienteId = contrato.ClienteId,
                Monto = request.Monto,
                FechaPago = request.FechaPago.Date,
                Periodo = string.IsNullOrWhiteSpace(request.Periodo)
                    ? request.FechaPago.ToString("yyyy-MM")
                    : request.Periodo.Trim(),
                MetodoPago = request.MetodoPago?.Trim(),
                Concepto = string.IsNullOrWhiteSpace(request.Concepto) ? "Fee mensual" : request.Concepto.Trim(),
                Estado = "Registrado",
                Notas = request.Notas,
                FechaCreacion = DateTime.UtcNow,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre
            };

            _context.CrmPagos.Add(pago);

            if (request.MarcarContratoAlDia)
            {
                contrato.EstadoPago = "Al dia";
                if (request.NuevoProximoVencimiento.HasValue)
                    contrato.ProximoVencimiento = request.NuevoProximoVencimiento.Value.Date;
                else if (contrato.EsRecurrente)
                {
                    var baseDate = contrato.ProximoVencimiento?.Date ?? request.FechaPago.Date;
                    contrato.ProximoVencimiento = contrato.Periodicidad == "Anual"
                        ? baseDate.AddYears(1)
                        : baseDate.AddMonths(1);
                }
                contrato.FechaActualizacion = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            pago.Cliente = contrato.Cliente;
            pago.Contrato = contrato;
            return MapPago(pago);
        }

        public async Task<CrmIngresosResumenDto> ObtenerIngresosAsync(int empresaId)
        {
            var contratos = await ListarContratosAsync(empresaId);
            var activos = contratos.Where(c => c.EstadoContrato == "Activo").ToList();
            var recurrentes = activos.Where(c => c.EsRecurrente).ToList();

            var pagos = await ListarPagosAsync(empresaId);
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            return new CrmIngresosResumenDto
            {
                MrrActivo = recurrentes.Where(c => c.Periodicidad == "Mensual").Sum(c => c.FeeMensual)
                    + recurrentes.Where(c => c.Periodicidad == "Anual").Sum(c => c.FeeAnual / 12m),
                ArrProyectado = recurrentes.Sum(c => c.FeeAnual > 0 ? c.FeeAnual : c.FeeMensual * 12),
                CobradoMesActual = pagos.Where(p => p.FechaPago.Date >= inicioMes).Sum(p => p.Monto),
                CobradoTotal = pagos.Sum(p => p.Monto),
                ContratosActivos = activos.Count,
                ContratosPendientesPago = activos.Count(c => c.EstadoPago == "Pendiente"),
                ContratosVencidos = activos.Count(c => c.EstadoPago == "Vencido"
                    || (c.ProximoVencimiento.HasValue && c.ProximoVencimiento.Value.Date < DateTime.Today && c.EstadoPago != "Al dia")),
                Recurrentes = recurrentes,
                UltimosPagos = pagos.Take(15).ToList()
            };
        }

        private async Task ValidarClienteAsync(int empresaId, int clienteId)
        {
            var ok = await _context.CrmClientes.AnyAsync(c =>
                c.ClienteId == clienteId && c.EmpresaId == empresaId && c.Activo);
            if (!ok) throw new InvalidOperationException("Cliente no válido para esta empresa.");
        }

        private static string Normalizar(string? value, string[] allowed, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var match = allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? fallback;
        }

        private static CrmContratoDto MapContrato(CrmContrato c, int cantPagos, decimal totalCobrado) => new()
        {
            ContratoId = c.ContratoId,
            EmpresaId = c.EmpresaId,
            ClienteId = c.ClienteId,
            ClienteNombre = c.Cliente?.RazonSocial ?? "",
            SistemaServicio = c.SistemaServicio,
            FeeAcordado = c.FeeAcordado,
            FeeMensual = c.FeeMensual,
            FeeAnual = c.FeeAnual,
            FechaInicio = c.FechaInicio,
            ProximoVencimiento = c.ProximoVencimiento,
            EstadoPago = c.EstadoPago,
            EstadoContrato = c.EstadoContrato,
            EsRecurrente = c.EsRecurrente,
            Periodicidad = c.Periodicidad,
            Notas = c.Notas,
            Activo = c.Activo,
            CantidadPagos = cantPagos,
            TotalCobrado = totalCobrado
        };

        private static CrmPagoDto MapPago(CrmPago p) => new()
        {
            PagoId = p.PagoId,
            EmpresaId = p.EmpresaId,
            ContratoId = p.ContratoId,
            ClienteId = p.ClienteId,
            ClienteNombre = p.Cliente?.RazonSocial ?? "",
            SistemaServicio = p.Contrato?.SistemaServicio ?? "",
            Monto = p.Monto,
            FechaPago = p.FechaPago,
            Periodo = p.Periodo,
            MetodoPago = p.MetodoPago,
            Concepto = p.Concepto,
            Estado = p.Estado,
            Notas = p.Notas,
            UsuarioNombre = p.UsuarioNombre
        };
    }
}
