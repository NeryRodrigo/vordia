using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Gesco.Application.Services
{
    public class CajaPosService : ICajaPosService
    {
        private readonly IApplicationDbContext _context;

        public CajaPosService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> RealizarVentaAsync(int empresaId, RealizarVentaRequest request)
        {
            // Buscar o crear cliente genérico
            var cliente = await _context.Personas.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.RazonSocial == "CLIENTE OCASIONAL");
            if (cliente == null)
            {
                cliente = new Persona
                {
                    EmpresaId = empresaId,
                    RazonSocial = "CLIENTE OCASIONAL",
                    Documento = "0",
                    EsCliente = true
                };
                _context.Personas.Add(cliente);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID
            }

            // 1. Crear cabecera de la venta
            var cabecera = new VentaCabecera
            {
                EmpresaId = empresaId,
                PersonaId = cliente.PersonaId, // Usar el ID seguro
                Fecha = DateTime.Now,
                NroFactura = request.NroFactura,
                Total = request.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario)
            };

            // 2. Procesar detalles y descontar stock
            foreach (var det in request.Detalles)
            {
                var detalle = new VentaDetalle
                {
                    ProductoId = det.ProductoId,
                    Descripcion = det.Descripcion,
                    Cantidad = det.Cantidad,
                    PrecioUnitario = det.PrecioUnitario,
                    Subtotal = det.Cantidad * det.PrecioUnitario,
                    IvaPorcentaje = det.IvaPorcentaje
                };
                cabecera.Detalles.Add(detalle);

                // Descuento de stock global
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.ProductoId == det.ProductoId && p.EmpresaId == empresaId);
                if (producto != null)
                {
                    producto.StockActual -= det.Cantidad;
                }
            }

            _context.VentaCabeceras.Add(cabecera);
            
            // Todo esto ocurre en una sola transacción gracias a EF Core SaveChangesAsync
            await _context.SaveChangesAsync();

            // 3. Auto-avanzar la secuencia de timbrado si corresponde
            var partesFactura = request.NroFactura.Split('-');
            if (partesFactura.Length == 3 && int.TryParse(partesFactura[2], out var numSecuencial))
            {
                var timbrado = await _context.ConfiguracionesTimbrado
                    .FirstOrDefaultAsync(t => t.EmpresaId == empresaId && t.EstaActivo);
                if (timbrado != null && numSecuencial > timbrado.UltimoNumeroUsado)
                {
                    timbrado.UltimoNumeroUsado = numSecuencial;
                    await _context.SaveChangesAsync();
                }
            }

            return cabecera.VentaCabeceraId;
        }

        public async Task<string> GetSiguienteFacturaAsync(int empresaId)
        {
            var timbrado = await _context.ConfiguracionesTimbrado
                .FirstOrDefaultAsync(t => t.EmpresaId == empresaId && t.EstaActivo);
            
            int siguienteNumero = 1;
            if (timbrado != null)
            {
                siguienteNumero = timbrado.UltimoNumeroUsado + 1;
                if (siguienteNumero < timbrado.NumeroDesde) siguienteNumero = timbrado.NumeroDesde;
            }
            else
            {
                // Fallback: Buscar el número secuencial más alto registrado
                var ultimaVenta = await _context.VentaCabeceras
                    .Where(v => v.EmpresaId == empresaId)
                    .OrderByDescending(v => v.VentaCabeceraId)
                    .FirstOrDefaultAsync();

                if (ultimaVenta != null && !string.IsNullOrEmpty(ultimaVenta.NroFactura))
                {
                    var partes = ultimaVenta.NroFactura.Split('-');
                    if (partes.Length == 3 && int.TryParse(partes[2], out var numSec))
                    {
                        siguienteNumero = numSec + 1;
                    }
                }
            }

            return $"001-001-{siguienteNumero.ToString().PadLeft(7, '0')}";
        }

        public async Task<byte[]> GenerarFacturaPdfAsync(int ventaId)
        {
            // Registrar licencia de uso libre (Community) de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // 1. Cargar la venta con detalles e información del cliente
            var venta = await _context.VentaCabeceras
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.VentaCabeceraId == ventaId);

            if (venta == null)
            {
                throw new Exception("Venta no encontrada.");
            }

            // Generar un CDC ficticio pero super realista de 44 caracteres paraguayo
            string cdc = $"01800045121{venta.NroFactura.Replace("-", "")}{DateTime.Now.Year}992745082154".PadRight(44, '0');

            // 2. Construir el documento PDF con QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                    // --- HEADER ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("INVIPINT S.A.").Bold().FontSize(18).FontColor(Colors.Blue.Darken4);
                            col.Item().Text("FÁBRICA DE PINTURAS - RETAIL & DISTRIBUCIÓN").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken1);
                            col.Item().Text("Dirección: Av. Mariscal López 450 - Asunción, Paraguay").FontSize(8);
                            col.Item().Text("Teléfono: +595 21 600 700 | Email: ventas@invipint.com.py").FontSize(8);
                        });

                        row.ConstantItem(180).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(col =>
                        {
                            col.Item().AlignCenter().Text("FACTURA").Bold().FontSize(12).FontColor(Colors.Blue.Darken4);
                            col.Item().Text($"RUC: 80004512-1").Bold().FontSize(9);
                            col.Item().Text($"Timbrado N°: 15412984").FontSize(8);
                            col.Item().Text($"Validez: 15/05/2026 - 15/05/2027").FontSize(8);
                            col.Item().Text($"N°: {venta.NroFactura}").Bold().FontSize(10).FontColor(Colors.Red.Medium);
                        });
                    });

                    // --- CONTENT ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Datos del Cliente
                        col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(x => { x.Span("Cliente: ").Bold(); x.Span(venta.Cliente?.RazonSocial ?? "CLIENTE OCASIONAL"); });
                                c.Item().Text(x => { x.Span("RUC / CI: ").Bold(); x.Span(venta.Cliente?.Documento ?? "0"); });
                                c.Item().Text(x => { x.Span("Teléfono: ").Bold(); x.Span(venta.Cliente?.Telefono ?? "N/A"); });
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(x => { x.Span("Fecha/Hora: ").Bold(); x.Span(venta.Fecha.ToString("dd/MM/yyyy HH:mm")); });
                                c.Item().Text(x => { x.Span("Condición: ").Bold(); x.Span("Contado"); });
                                c.Item().Text(x => { x.Span("Moneda: ").Bold(); x.Span("Guaraníes (PYG)"); });
                            });
                        });

                        col.Item().PaddingTop(15);

                        // Tabla de Artículos
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Descripción
                                columns.RelativeColumn(1); // IVA
                                columns.RelativeColumn(1); // Cantidad
                                columns.RelativeColumn(1.5f); // Precio Unitario
                                columns.RelativeColumn(1.5f); // Subtotal
                            });

                            // Cabecera Tabla
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Descripción").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5).AlignCenter().Text("IVA").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5).AlignCenter().Text("Cant").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5).AlignRight().Text("P. Unitario").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5).AlignRight().Text("Subtotal").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            // Filas
                            foreach (var d in venta.Detalles)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(d.Descripcion).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{d.IvaPorcentaje}%").FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(d.Cantidad.ToString("0")).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(string.Format("{0:N0} Gs.", d.PrecioUnitario)).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(string.Format("{0:N0} Gs.", d.Subtotal)).Bold().FontSize(9);
                            }
                        });

                        col.Item().PaddingTop(15);

                        // Totales y Desglose de Impuestos
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Liquidación de IVA (Partida Doble)").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                                decimal iva10 = venta.Total - (venta.Total / 1.1m);
                                c.Item().Text(x => { x.Span("Gravadas 10%: ").FontSize(8); x.Span(string.Format("{0:N0} Gs.", venta.Total / 1.1m)).FontSize(8); });
                                c.Item().Text(x => { x.Span("IVA 10%: ").Bold().FontSize(8); x.Span(string.Format("{0:N0} Gs.", iva10)).Bold().FontSize(8); });
                            });

                            row.ConstantItem(180).Background(Colors.Grey.Lighten5).Padding(8).Column(c =>
                            {
                                c.Item().AlignRight().Text(x => { x.Span("TOTAL NETO: ").Bold().FontSize(12).FontColor(Colors.Blue.Darken4); x.Span(string.Format("{0:N0} Gs.", venta.Total)).Bold().FontSize(12).FontColor(Colors.Blue.Darken4); });
                            });
                        });

                        // Sección SIFEN Factura Electrónica
                        col.Item().PaddingTop(25).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().AlignCenter().Text("ESTE DOCUMENTO ES UNA REPRESENTACIÓN GRÁFICA DE UN DOCUMENTO ELECTRÓNICO (KUDE)").Bold().FontSize(7).FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).AlignCenter().Text($"CDC: {cdc}").FontColor(Colors.Grey.Darken2).FontSize(7);
                            c.Item().AlignCenter().Text("Aprobado por el Sistema Integrado de Facturación Electrónica Nacional (SIFEN) - Paraguay").FontSize(6).Italic();
                        });
                    });

                    // --- FOOTER ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" de ").FontSize(8);
                        x.TotalPages().FontSize(8);
                        x.Span(" | Generado por GESCO Core ERP con tecnología QuestPDF").FontSize(8).Italic();
                    });
                });
            });

            // 3. Renderizar el PDF a memoria
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerarCompraPdfAsync(int compraId)
        {
            // Registrar licencia de uso libre (Community) de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // 1. Cargar la compra con detalles e información del proveedor y producto
            var compra = await _context.CompraCabeceras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(c => c.CompraCabeceraId == compraId);

            if (compra == null)
            {
                throw new Exception("Compra no encontrada.");
            }

            // 2. Construir el documento PDF con QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Grey.Darken3));

                    // --- HEADER ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("INVIPINT S.A.").Bold().FontSize(18).FontColor(Colors.Red.Darken4);
                            col.Item().Text("REGISTRO DE EGRESOS & COMPRAS DE MATERIA PRIMA").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken1);
                            col.Item().Text("Dirección: Av. Mariscal López 450 - Asunción, Paraguay").FontSize(8);
                            col.Item().Text("Módulo: Tesorería y Finanzas Operativas").FontSize(8);
                        });

                        row.ConstantItem(180).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(col =>
                        {
                            col.Item().AlignCenter().Text("EGRESO / COMPRA").Bold().FontSize(12).FontColor(Colors.Red.Darken4);
                            col.Item().Text($"ID Operación: #{compra.CompraCabeceraId}").Bold().FontSize(9);
                            col.Item().Text($"Factura Prov: {compra.NroFactura}").FontSize(8);
                            col.Item().Text($"Fecha Registro: {compra.FechaFactura:dd/MM/yyyy}").FontSize(8);
                            col.Item().Text($"TOTAL EGRESO").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"{string.Format("{0:N0} Gs.", compra.Total)}").Bold().FontSize(10).FontColor(Colors.Red.Medium);
                        });
                    });

                    // --- CONTENT ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Datos del Proveedor
                        col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(x => { x.Span("Proveedor: ").Bold(); x.Span(compra.Proveedor?.RazonSocial ?? "PROVEEDOR GENÉRICO"); });
                                c.Item().Text(x => { x.Span("RUC: ").Bold(); x.Span(compra.Proveedor?.Ruc ?? "0"); });
                                c.Item().Text(x => { x.Span("Contacto: ").Bold(); x.Span(compra.Proveedor?.Telefono ?? "N/A"); });
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(x => { x.Span("Fecha Compra: ").Bold(); x.Span(compra.FechaFactura.ToString("dd/MM/yyyy HH:mm")); });
                                c.Item().Text(x => { x.Span("Tipo Transacción: ").Bold(); x.Span("Egreso de Caja (Efectivo/Transferencia)"); });
                                c.Item().Text(x => { x.Span("Moneda: ").Bold(); x.Span("Guaraníes (PYG)"); });
                            });
                        });

                        col.Item().PaddingTop(15);

                        // Tabla de Artículos
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Producto / Descripción
                                columns.RelativeColumn(1); // Cantidad
                                columns.RelativeColumn(2); // Costo Unitario
                                columns.RelativeColumn(2); // Subtotal Costo
                            });

                            // Cabecera Tabla
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Red.Darken4).Padding(5).Text("Producto / Insumo").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Red.Darken4).Padding(5).AlignCenter().Text("Cant").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Red.Darken4).Padding(5).AlignRight().Text("Costo Unitario").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background(Colors.Red.Darken4).Padding(5).AlignRight().Text("Subtotal").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            // Filas
                            foreach (var d in compra.Detalles)
                            {
                                string desc = d.Producto?.Descripcion ?? $"Insumo #{d.ProductoId}";
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(desc).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(d.Cantidad.ToString("0")).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(string.Format("{0:N0} Gs.", d.CostoUnitario)).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(string.Format("{0:N0} Gs.", d.Subtotal)).Bold().FontSize(9);
                            }
                        });

                        col.Item().PaddingTop(25);

                        // Totales y Nota de Pago
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Observaciones y Contabilidad").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                                c.Item().Text("Este documento certifica la salida de fondos de la caja chica para la adquisición de mercaderías e insumos para el stock de INVIPINT S.A.").FontSize(8).Italic();
                            });

                            row.ConstantItem(180).Background(Colors.Grey.Lighten5).Padding(8).Column(c =>
                            {
                                c.Item().AlignRight().Text(x => { x.Span("TOTAL EGRESO: ").Bold().FontSize(12).FontColor(Colors.Red.Darken4); x.Span(string.Format("{0:N0} Gs.", compra.Total)).Bold().FontSize(12).FontColor(Colors.Red.Darken4); });
                            });
                        });
                    });

                    // --- FOOTER ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" de ").FontSize(8);
                        x.TotalPages().FontSize(8);
                        x.Span(" | Comprobante Interno de Egreso - GESCO ERP").FontSize(8).Italic();
                    });
                });
            });

            // 3. Renderizar el PDF a memoria
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}
