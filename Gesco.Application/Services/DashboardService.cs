using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class DashboardService : IDashboardService
    {
        //private readonly GescoDbContext _context;
        private readonly IApplicationDbContext _context;

        public DashboardService(/*GescoDbContext*/IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardCondominioDto> GetDashboardDataAsync(int empresaId)
        {
            var now = DateTime.Now;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            var unidades = await _context.UnidadesCondominio
                .Where(u => u.EmpresaId == empresaId)
                .Include(u => u.Contratos.Where(c => c.EstaActivo))
                    .ThenInclude(c => c.Inquilino)
                .Include(u => u.Contratos.Where(c => c.EstaActivo))
                    .ThenInclude(c => c.Pagos)
                .ToListAsync();

            var dto = new DashboardCondominioDto
            {
                Summary = new DashboardSummaryDto()
            };

            var ocupados = unidades.Where(u => u.Estado == "Ocupado").ToList();
            var disponibles = unidades.Where(u => u.Estado == "Disponible").ToList();

            dto.Summary.TotalOcupados = ocupados.Count;
            dto.Summary.TotalDisponibles = disponibles.Count;

            // Limpiar registros simulados por bot prematuramente (antes de fecha de emisión)
            var prematuros = await _context.GastosMantenimiento
                .Where(g => g.Descripcion == "Luz" && g.MesReferencia == currentMonth && g.AnioReferencia == currentYear && g.MetodoPago == "Débito Automático/Web Scraping")
                .ToListAsync();
            
            var aEliminar = new List<GastoMantenimiento>();
            foreach (var g in prematuros)
            {
                var uni = unidades.FirstOrDefault(u => u.UnidadId == g.UnidadId);
                var nisStr = uni?.NisAnde ?? "";
                if (!int.TryParse(nisStr.Length >= 2 ? nisStr.Substring(nisStr.Length - 2) : "0", out int nis)) nis = 15;
                int emisionDay = Math.Max(1, ((nis % 15) + 10) - 5);
                if (now.Day < emisionDay)
                {
                    aEliminar.Add(g);
                }
            }
            if (aEliminar.Any())
            {
                _context.GastosMantenimiento.RemoveRange(aEliminar);
                await _context.SaveChangesAsync();
            }

            var todosGastosLuz = await _context.GastosMantenimiento
                .Where(g => g.Descripcion == "Luz")
                .OrderByDescending(g => g.AnioReferencia)
                .ThenByDescending(g => g.MesReferencia)
                .ThenByDescending(g => g.Fecha)
                .ToListAsync();

            // Dúplex Ocupados Detalle
            dto.Ocupados = ocupados.Select(u => {
                var c = u.Contratos.FirstOrDefault();
                
                var nisStr = u.NisAnde ?? "";
                if (!int.TryParse(nisStr.Length >= 2 ? nisStr.Substring(nisStr.Length - 2) : "0", out int nisNum))
                    nisNum = 15;
                    
                int diaVencimiento = (nisNum % 15) + 10;
                var fechaVencimientoActual = new DateTime(currentYear, currentMonth, diaVencimiento);
                int diaEmision = Math.Max(1, diaVencimiento - 5);
                
                var gastoLuzMesActual = todosGastosLuz.FirstOrDefault(g => g.UnidadId == u.UnidadId && g.MesReferencia == currentMonth && g.AnioReferencia == currentYear);
                bool facturaEmitidaEsteMes = gastoLuzMesActual != null || now.Day >= diaEmision;
                
                bool andePagado;
                decimal andeDeuda = 0;
                DateTime? andeFechaPago = null;
                DateTime? andeFechaVencimiento = null;
                int andeConsumoKw = 0;
                
                if (facturaEmitidaEsteMes)
                {
                    // Al emitirse la factura de la ANDE para el mes actual, su estado de pago debe ser pendiente,
                    // sin fecha de pago (null) y con la fecha de vencimiento.
                    andePagado = false;
                    andeFechaPago = null;
                    andeFechaVencimiento = fechaVencimientoActual;
                    
                    if (gastoLuzMesActual != null)
                    {
                        andeDeuda = gastoLuzMesActual.Monto;
                        andeConsumoKw = Math.Max(1, (int)Math.Round((gastoLuzMesActual.Monto - 85000) / 450m));
                    }
                    else if (!string.IsNullOrEmpty(u.NisAnde) && u.NisAnde != "N/A" && u.NisAnde != "SIN NIS")
                    {
                        bool isVencido = now > fechaVencimientoActual;
                        andeConsumoKw = 160 + ((Math.Abs(u.NisAnde.GetHashCode()) % 30) * 5);
                        int montoBase = 85000 + (andeConsumoKw * 450);
                        andeDeuda = isVencido ? montoBase + (montoBase * 0.15m) : montoBase;
                    }
                }
                else
                {
                    // Aún no se emitió la factura de este mes. Mostrar último pago de cada factura (p. ej. Junio)
                    var ultimoPago = todosGastosLuz.FirstOrDefault(g => g.UnidadId == u.UnidadId);
                    if (ultimoPago != null)
                    {
                        andePagado = true;
                        andeFechaPago = ultimoPago.Fecha;
                        andeFechaVencimiento = new DateTime(ultimoPago.AnioReferencia ?? currentYear, ultimoPago.MesReferencia ?? currentMonth, diaVencimiento);
                        andeDeuda = ultimoPago.Monto;
                        andeConsumoKw = Math.Max(1, (int)Math.Round((ultimoPago.Monto - 85000) / 450m));
                    }
                    else
                    {
                        // Si no hay ningún pago anterior en el historial, mostrar como pendiente este mes
                        andePagado = false;
                        andeFechaVencimiento = fechaVencimientoActual;
                        if (!string.IsNullOrEmpty(u.NisAnde) && u.NisAnde != "N/A" && u.NisAnde != "SIN NIS")
                        {
                            bool isVencido = now > fechaVencimientoActual;
                            andeConsumoKw = 160 + ((Math.Abs(u.NisAnde.GetHashCode()) % 30) * 5);
                            int montoBase = 85000 + (andeConsumoKw * 450);
                            andeDeuda = isVencido ? montoBase + (montoBase * 0.15m) : montoBase;
                        }
                    }
                }

                return new DashboardDetailDto {
                    UnidadId = u.UnidadId,
                    Duplex = u.CodigoUnidad ?? "S/D",
                    Condominio = u.NombreCondominio ?? "Aguz",
                    Inquilino = c?.Inquilino?.RazonSocial ?? "S/I",
                    Cedula = c?.Inquilino?.Documento ?? "N/A",
                    NisAnde = u.NisAnde ?? "N/A",
                    Ocupantes = c?.CantidadOcupantes,
                    Monto = c?.MontoMensualAlquiler ?? 0,
                    AndePagadoEsteMes = andePagado,
                    AndeMontoDeuda = andeDeuda,
                    AndeFechaPago = andeFechaPago,
                    AndeFacturaEmitida = facturaEmitidaEsteMes,
                    AndeFechaVencimiento = andeFechaVencimiento,
                    AndeConsumoKw = andeConsumoKw
                };
            }).ToList();

            // Dúplex Disponibles Detalle
            dto.Disponibles = disponibles.Select(u => new DashboardDetailDto {
                UnidadId = u.UnidadId,
                Duplex = u.CodigoUnidad ?? "S/D",
                Condominio = u.NombreCondominio ?? "Aguz",
                Inquilino = "Disponible",
                NisAnde = u.NisAnde ?? "N/A",
                Monto = 0 
            }).ToList();

            // Objetivos Mes: Sumatoria de alquiler de los ocupados en el mes actual
            dto.ObjetivosMes = ocupados.Select(u => {
                var c = u.Contratos.FirstOrDefault();
                return new DashboardDetailDto {
                    UnidadId = u.UnidadId,
                    Duplex = u.CodigoUnidad ?? "S/D",
                    Condominio = u.NombreCondominio ?? "Aguz",
                    Inquilino = c?.Inquilino?.RazonSocial ?? "S/I",
                    Monto = c?.MontoMensualAlquiler ?? 0
                };
            }).Where(d => d.Monto > 0).ToList();

            dto.Summary.ObjetivoMes = dto.ObjetivosMes.Sum(x => x.Monto);

            // Cobrado Mes: Pagos con fecha del mes actual (solo alquiler)
            dto.Cobrados = ocupados.SelectMany(u => {
                var c = u.Contratos.FirstOrDefault();
                if (c == null) return new List<DashboardDetailDto>();
                var pagosContrato = c.Pagos.Where(p => p.FechaPago.Month == currentMonth && p.FechaPago.Year == currentYear && (p.TipoPago == "Alquiler" || string.IsNullOrEmpty(p.TipoPago))).ToList();
                return pagosContrato.Select(p => new DashboardDetailDto {
                    UnidadId = u.UnidadId,
                    Duplex = u.CodigoUnidad ?? "S/D",
                    Condominio = u.NombreCondominio ?? "Aguz",
                    Inquilino = c.Inquilino?.RazonSocial ?? "S/I",
                    Monto = p.MontoPagado,
                    Fecha = p.FechaPago
                });
            }).ToList();

            dto.Summary.CobradoMes = dto.Cobrados.Sum(x => x.Monto);

            // Falta cobrar
            foreach (var u in ocupados)
            {
                var c = u.Contratos.FirstOrDefault();
                if (c != null)
                {
                    decimal montoPagado = c.Pagos.Where(p => p.FechaPago.Month == currentMonth && p.FechaPago.Year == currentYear && (p.TipoPago == "Alquiler" || string.IsNullOrEmpty(p.TipoPago))).Sum(p => p.MontoPagado);
                    decimal falta = c.MontoMensualAlquiler - montoPagado;
                    if (falta > 0)
                    {
                        dto.FaltaCobrar.Add(new DashboardDetailDto {
                            UnidadId = u.UnidadId,
                            Duplex = u.CodigoUnidad ?? "S/D",
                            Condominio = u.NombreCondominio ?? "Aguz",
                            Inquilino = c.Inquilino?.RazonSocial ?? "S/I",
                            MontoRestante = falta,
                            Cedula = c.Inquilino?.Documento ?? "N/A"
                        });
                    }
                }
            }
            dto.Summary.FaltaCobrar = dto.FaltaCobrar.Sum(x => x.MontoRestante ?? 0);

            // Vencimientos: Inquilinos con pago vencido este mes
            foreach (var u in ocupados)
            {
                var c = u.Contratos.FirstOrDefault();
                if (c != null)
                {
                    // Asumimos vencimiento el día 5 del mes si no está especificado
                    int diaVencimiento = c.FechaInicioContrato.Day <= 28 ? c.FechaInicioContrato.Day : 5;
                    if (diaVencimiento < 1 || diaVencimiento > 28) diaVencimiento = 5;
                    var fechaVencimiento = new DateTime(currentYear, currentMonth, diaVencimiento);

                    if (now.Date > fechaVencimiento.Date)
                    {
                        decimal montoPagado = c.Pagos.Where(p => p.FechaPago.Month == currentMonth && p.FechaPago.Year == currentYear && (p.TipoPago == "Alquiler" || string.IsNullOrEmpty(p.TipoPago))).Sum(p => p.MontoPagado);
                        decimal falta = c.MontoMensualAlquiler - montoPagado;
                        if (falta > 0)
                        {
                            dto.Vencimientos.Add(new DashboardDetailDto {
                                UnidadId = u.UnidadId,
                                Duplex = u.CodigoUnidad ?? "S/D",
                                Condominio = u.NombreCondominio ?? "Aguz",
                                Inquilino = c.Inquilino?.RazonSocial ?? "S/I",
                                Monto = falta,
                                Fecha = fechaVencimiento
                            });
                        }
                    }
                }
            }
            dto.Summary.TotalVencimientos = dto.Vencimientos.Sum(x => x.Monto);

            // Morosos: Desalojados con saldos arrastrados
            var morosidades = await _context.MorosidadesInquilino
                .Include(m => m.Inquilino)
                .Include(m => m.Unidad)
                .Where(m => m.Estado != "Pagado" && m.Unidad.EmpresaId == empresaId)
                .ToListAsync();

            dto.Morosos = morosidades.Select(m => new DashboardDetailDto {
                UnidadId = m.UnidadId,
                Duplex = m.Unidad?.CodigoUnidad ?? "S/D",
                Condominio = m.Unidad?.NombreCondominio ?? "Aguz",
                Inquilino = m.Inquilino?.RazonSocial ?? "S/I",
                Monto = m.SaldoPendiente,
                InteresAcumulado = m.SaldoPendiente * 0.10m, // Estimación o se puede sacar de base de datos
            }).ToList();

            dto.Summary.TotalMorosos = dto.Morosos.Sum(x => x.Monto);

            return dto;
        }
    }
}
