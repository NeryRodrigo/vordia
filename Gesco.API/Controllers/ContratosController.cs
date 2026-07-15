using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Gesco.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratosController : ControllerBase
    {
        private readonly IContratoInquilinoService _contratoService;
        private readonly IEmailService _emailService;
        private readonly IApplicationDbContext _context;

        public ContratosController(IContratoInquilinoService contratoService, IEmailService emailService, IApplicationDbContext context)
        {
            _contratoService = contratoService;
            _emailService = emailService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateContrato([FromBody] ContratoRequest request)
        {
            var inquilino = new Persona
            {
                EmpresaId = request.EmpresaId,
                RazonSocial = request.NombreCompleto,
                Documento = request.Cedula,
                Telefono = request.Telefono,
                Email = request.Email,
                EsCliente = true
            };

            var contrato = new ContratoInquilino
            {
                UnidadId = request.UnidadId,
                FechaIngreso = request.FechaIngreso,
                FechaInicioContrato = request.FechaInicio,
                FechaFinContrato = request.FechaFin,
                MontoMensualAlquiler = request.MontoMensual,
                DepositoGarantia = request.Garantia,
                CantidadOcupantes = request.CantidadOcupantes,
                VehiculoInfo = request.Vehiculo,
                Observaciones = request.Observaciones,
                ServiciosIncluidos = request.ServiciosIncluidos
            };

            var result = await _contratoService.CreateContratoAsync(contrato, inquilino);
            return Ok(result);
        }

        [HttpPost("pago")]
        public async Task<IActionResult> RegisterPago([FromBody] PagoRecibo pago)
        {
            var result = await _contratoService.RegisterPagoAsync(pago);
            return Ok(result);
        }

        [HttpPost("pagos-consolidados")]
        public async Task<IActionResult> RegisterPagosConsolidados([FromBody] System.Collections.Generic.List<PagoRecibo> pagos)
        {
            if (pagos == null || pagos.Count == 0)
            {
                return BadRequest("No se proporcionaron pagos para registrar.");
            }

            // Asegurar que todos tengan el mismo número de factura y fecha de pago si se procesan juntos
            string? numeroFacturaComun = pagos.Find(p => !string.IsNullOrEmpty(p.NumeroFactura))?.NumeroFactura;
            string? nroComprobanteComun = pagos.Find(p => !string.IsNullOrEmpty(p.NroComprobante))?.NroComprobante;
            string? metodoPagoComun = pagos.Find(p => !string.IsNullOrEmpty(p.MetodoPago))?.MetodoPago;
            string? bancoComun = pagos.Find(p => !string.IsNullOrEmpty(p.Banco))?.Banco;
            string? notasComun = pagos.Find(p => !string.IsNullOrEmpty(p.Notas))?.Notas;
            string? responsableComun = pagos.Find(p => !string.IsNullOrEmpty(p.ResponsableCobro))?.ResponsableCobro;
            DateTime fechaPagoComun = pagos.Find(p => p.FechaPago != default)?.FechaPago ?? DateTime.Now;

            var registrados = new System.Collections.Generic.List<PagoRecibo>();
            foreach (var pago in pagos)
            {
                pago.NumeroFactura = string.IsNullOrEmpty(pago.NumeroFactura) ? numeroFacturaComun : pago.NumeroFactura;
                pago.NroComprobante = string.IsNullOrEmpty(pago.NroComprobante) ? nroComprobanteComun : pago.NroComprobante;
                pago.MetodoPago = string.IsNullOrEmpty(pago.MetodoPago) ? metodoPagoComun : pago.MetodoPago;
                pago.Banco = string.IsNullOrEmpty(pago.Banco) ? bancoComun : pago.Banco;
                pago.Notas = string.IsNullOrEmpty(pago.Notas) ? notasComun : pago.Notas;
                pago.ResponsableCobro = string.IsNullOrEmpty(pago.ResponsableCobro) ? responsableComun : pago.ResponsableCobro;
                pago.FechaPago = pago.FechaPago == default ? fechaPagoComun : pago.FechaPago;
                
                var result = await _contratoService.RegisterPagoAsync(pago);
                registrados.Add(result);
            }

            return Ok(registrados);
        }

        [HttpGet("unidad/{unidadId}")]
        public async Task<IActionResult> GetContratoByUnidad(int unidadId)
        {
            var result = await _contratoService.GetContratoByUnidadAsync(unidadId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("unidad/{unidadId}/historico")]
        public async Task<IActionResult> GetContratosHistoricosByUnidad(int unidadId)
        {
            var result = await _contratoService.GetContratosHistoricosByUnidadAsync(unidadId);
            return Ok(result);
        }

        [HttpGet("historico")]
        public async Task<IActionResult> GetContratosHistoricos()
        {
            var result = await _contratoService.GetAllContratosHistoricosAsync();
            return Ok(result);
        }

        [HttpGet("pagos")]
        public async Task<IActionResult> GetAllPagos()
        {
            var result = await _contratoService.GetAllPagosAsync();
            return Ok(result);
        }

        [HttpDelete("pago/{id}")]
        public async Task<IActionResult> DeletePago(int id)
        {
            var result = await _contratoService.DeletePagoAsync(id);
            if (!result) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPut("pago/{id}")]
        public async Task<IActionResult> UpdatePago(int id, [FromBody] PagoRecibo pago)
        {
            if (id != pago.PagoId) return BadRequest();
            var ok = await _contratoService.UpdatePagoAsync(pago);
            if (!ok) return NotFound();
            return Ok(pago);
        }

        public class PagoComprobanteDto
        {
            public bool FacturaEmitida { get; set; }
            public string? NumeroFactura { get; set; }
            public string? ComprobanteUrl { get; set; }
        }

        [HttpPut("pago/{id}/comprobante")]
        public async Task<IActionResult> UpdatePagoComprobante(int id, [FromBody] PagoComprobanteDto dto)
        {
            var ok = await _contratoService.UpdatePagoComprobanteAsync(id, dto.FacturaEmitida, dto.NumeroFactura, dto.ComprobanteUrl);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateContrato([FromBody] UpdateContratoRequest request)
        {
            var inquilino = new Persona
            {
                PersonaId = request.InquilinoId,
                RazonSocial = request.NombreCompleto,
                Documento = request.Cedula,
                Telefono = request.Telefono,
                Email = request.Email
            };

            var contrato = new ContratoInquilino
            {
                ContratoId = request.ContratoId,
                FechaIngreso = request.FechaIngreso,
                FechaInicioContrato = request.FechaInicio,
                FechaFinContrato = request.FechaFin,
                MontoMensualAlquiler = request.MontoMensual,
                DepositoGarantia = request.Garantia,
                CantidadOcupantes = request.CantidadOcupantes,
                VehiculoInfo = request.Vehiculo,
                Observaciones = request.Observaciones,
                ServiciosIncluidos = request.ServiciosIncluidos
            };

            var result = await _contratoService.UpdateContratoAsync(contrato, inquilino);
            return Ok(result);
        }

        [HttpPost("{id}/desalojar")]
        public async Task<IActionResult> DesalojarUnidad(int id)
        {
            var result = await _contratoService.DesalojarUnidadAsync(id);
            if (!result) return NotFound(new { success = false, message = "Contrato no encontrado o no se pudo desalojar" });
            return Ok(new { success = true, message = "Unidad desalojada exitosamente" });
        }

        [HttpPost("{id}/anular-desalojo")]
        public async Task<IActionResult> AnularDesalojo(int id)
        {
            var result = await _contratoService.AnularDesalojoAsync(id);
            if (!result) return NotFound(new { success = false, message = "Contrato no encontrado o no se pudo anular el desalojo" });
            return Ok(new { success = true, message = "Desalojo anulado y unidad restablecida exitosamente" });
        }

        [HttpPost("unidad/{unidadId}/anular-desalojo")]
        public async Task<IActionResult> AnularDesalojoPorUnidad(int unidadId)
        {
            var historicos = await _contratoService.GetContratosHistoricosByUnidadAsync(unidadId);
            var ultimoDesalojado = historicos.OrderByDescending(c => c.FechaFinContrato).FirstOrDefault();
            if (ultimoDesalojado == null)
            {
                var activo = await _contratoService.GetContratoByUnidadAsync(unidadId);
                if (activo != null)
                {
                    await _contratoService.AnularDesalojoAsync(activo.ContratoId);
                    return Ok(new { success = true, message = "Desalojo/Renovación anulado y unidad restablecida exitosamente" });
                }
                return NotFound(new { success = false, message = "No se encontró ningún contrato para esta unidad" });
            }

            var result = await _contratoService.AnularDesalojoAsync(ultimoDesalojado.ContratoId);
            if (!result) return NotFound(new { success = false, message = "No se pudo anular el desalojo" });
            return Ok(new { success = true, message = "Desalojo/Renovación anulado y unidad restablecida exitosamente" });
        }

        [HttpPost("enviar-comprobante")]
        public async Task<IActionResult> EnviarComprobante([FromBody] EnviarComprobanteRequest request)
        {
            try
            {
                byte[]? pdfBytes = null;
                if (!string.IsNullOrEmpty(request.PdfBase64))
                {
                    var base64Data = request.PdfBase64.Contains(",") 
                        ? request.PdfBase64.Split(',')[1] 
                        : request.PdfBase64;
                    pdfBytes = Convert.FromBase64String(base64Data);
                }
                else
                {
                    pdfBytes = GenerateComprobantePdf(request);
                }

                var exito = await _emailService.EnviarComprobanteAsync(
                    request.EmailDestino,
                    request.NombreCliente,
                    request.NumeroComprobante,
                    request.TipoEmision,
                    request.Monto,
                    request.Concepto,
                    pdfBytes
                );

                if (exito)
                    return Ok(new { success = true, message = $"Comprobante enviado por correo exitosamente a {request.EmailDestino}." });

                return BadRequest(new { success = false, message = "No se pudo procesar el envío del correo." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error SMTP: {ex.Message}" });
            }
        }

        private byte[] GenerateComprobantePdf(EnviarComprobanteRequest request)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var monto = request.Monto;
            var montoStr = monto.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            var iva10 = Math.Round(monto / 11m);
            var iva10Str = iva10.ToString("N0", new System.Globalization.CultureInfo("es-PY"));
            var rucCli = !string.IsNullOrEmpty(request.RucCliente) ? request.RucCliente : "5865896";
            var dirCli = !string.IsNullOrEmpty(request.DireccionCliente) ? request.DireccionCliente : "MERLO DE PORTUGAL Y CAPITÁN PEDRO MAYOR ECHAURI";

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(1.2f, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Black));

                    page.Content().Border(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Column(col =>
                    {
                        // FILA 1: HEADER (2 COLUMNAS)
                        col.Item().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Row(row =>
                        {
                            // Izquierda: Emisor
                            row.RelativeItem().Padding(12).Column(c =>
                            {
                                c.Item().Text("Marcelo David Ortigoza Maidana")
                                    .FontFamily("Times New Roman").Bold().FontSize(18).FontColor(QuestPDF.Helpers.Colors.Black);
                                
                                c.Item().PaddingTop(4).Text("ACTIVIDADES PROFESIONALES, CIENTÍFICAS Y TÉCNICAS - OTROS ALOJAMIENTOS DE CORTO PLAZO.").Bold().FontSize(6.5f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                                c.Item().Text("ACTIVIDADES DE SERVICIOS DE ARQUITECTURA E INGENIERÍA.").Bold().FontSize(6.5f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                                c.Item().Text("COMERCIO AL POR MAYOR DE MERCANCÍAS A CAMBIO DE UNA RETRIBUCIÓN O POR CONTRATA.").Bold().FontSize(6.5f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                                c.Item().Text("INTERMEDIACIÓN EN LA COMPRA, VENTA Y ARRENDAMIENTO DE INMUEBLES.").Bold().FontSize(6.5f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);

                                c.Item().PaddingTop(10).Text("Merlo de Portugal y Capitán Pedro Mayor Echauri - Cel.: (0994) 254 655").FontSize(8).SemiBold();
                                c.Item().Text("Luque - Paraguay").FontSize(8).SemiBold();
                            });

                            // Derecha: Timbrado
                            row.ConstantItem(210).BorderLeft(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("TIMBRADO Nº 18093421").Bold().FontSize(8.5f);
                                c.Item().AlignCenter().Text("FECHA INICIO VIGENCIA: 11/JUNIO/2025").Bold().FontSize(7.5f);
                                c.Item().AlignCenter().Text("FECHA FIN VIGENCIA: 30/JUNIO/2026").Bold().FontSize(7.5f);

                                c.Item().PaddingTop(12).AlignCenter().Text("RUC: 3389403-5").Bold().FontSize(12);
                                c.Item().AlignCenter().Text(request.TipoEmision.ToUpper().Contains("FACTURA") ? "FACTURA" : "FACTURA").Bold().FontSize(20);
                                c.Item().AlignCenter().Text($"Nº {request.NumeroComprobante}").Bold().FontSize(11);
                            });
                        });

                        // FILA 2: FECHA Y CONDICION
                        col.Item().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Row(row =>
                        {
                            row.RelativeItem().Padding(6).PaddingLeft(10).Text(t =>
                            {
                                t.Span("FECHA DE EMISIÓN: ").Bold().FontSize(9);
                                t.Span(DateTime.Now.ToString("dd de MMMM de yyyy", new System.Globalization.CultureInfo("es-ES"))).Bold().FontSize(9);
                            });
                            row.ConstantItem(210).BorderLeft(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Text("COND. DE VENTA    CONTADO [X]    CRÉDITO [ ]").Bold().FontSize(8);
                        });

                        // FILA 3: RUC CLIENTE Y REMISION
                        col.Item().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Row(row =>
                        {
                            row.RelativeItem().Padding(6).PaddingLeft(10).Text(t =>
                            {
                                t.Span("RUC: ").Bold().FontSize(9);
                                t.Span(rucCli).Bold().FontSize(9);
                            });
                            row.RelativeItem().BorderLeft(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Text("NOTA DE REMISIÓN:").Bold().FontSize(9);
                        });

                        // FILA 4: RAZON SOCIAL
                        col.Item().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Text(t =>
                        {
                            t.Span("NOMBRE O RAZÓN SOCIAL: ").Bold().FontSize(9);
                            t.Span(request.NombreCliente.ToUpper()).Bold().FontSize(10);
                        });

                        // FILA 5: DIRECCION Y TELEFONO
                        col.Item().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Row(row =>
                        {
                            row.RelativeItem().Padding(6).PaddingLeft(10).Text(t =>
                            {
                                t.Span("DIRECCIÓN: ").Bold().FontSize(9);
                                t.Span(dirCli.ToUpper()).Bold().FontSize(9);
                            });
                            row.ConstantItem(180).BorderLeft(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Text("TELÉFONO:").Bold().FontSize(9);
                        });

                        // FILA 6: TABLA DE ITEMS
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // CANT
                                columns.RelativeColumn();    // DESCRIPCION
                                columns.ConstantColumn(85);  // PRECIO UNITARIO
                                columns.ConstantColumn(60);  // EXENTAS
                                columns.ConstantColumn(60);  // 5%
                                columns.ConstantColumn(80);  // 10%
                            });

                            // Encabezado de la tabla
                            table.Cell().BorderBottom(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("CANT.").Bold().FontSize(8);
                            table.Cell().BorderBottom(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("D E S C R I P C I Ó N").Bold().FontSize(8);
                            table.Cell().BorderBottom(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("PRECIO\nUNITARIO").Bold().FontSize(7.5f);
                            table.Cell().BorderBottom(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("EXENTAS").Bold().FontSize(8);
                            table.Cell().BorderBottom(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("5%").Bold().FontSize(8);
                            table.Cell().BorderBottom(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).AlignCenter().Text("10%").Bold().FontSize(8);

                            // Fila de Ítem
                            table.Cell().BorderBottom(1).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).AlignCenter().Text("1").Bold().FontSize(9);
                            table.Cell().BorderBottom(1).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).Text(request.Concepto).Bold().FontSize(9);
                            table.Cell().BorderBottom(1).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).AlignRight().Text(montoStr).Bold().FontSize(9);
                            table.Cell().BorderBottom(1).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).Text("");
                            table.Cell().BorderBottom(1).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).Text("");
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(8).AlignRight().Text(montoStr).Bold().FontSize(9);

                            // Filas vacías para dar altura al cuerpo de factura
                            for (int i = 0; i < 4; i++)
                            {
                                table.Cell().BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Text("");
                                table.Cell().BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Text("");
                                table.Cell().BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Text("");
                                table.Cell().BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Text("");
                                table.Cell().BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(10).Text("");
                                table.Cell().Padding(10).Text("");
                            }

                            // Sub Totales
                            table.Cell().ColumnSpan(5).BorderTop(1.5f).BorderRight(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Text("SUB TOTALES:").Bold().FontSize(8.5f);
                            table.Cell().BorderTop(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).AlignRight().Text(montoStr).Bold().FontSize(9.5f);
                        });

                        // TOTAL EN LETRAS
                        col.Item().BorderTop(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Row(row =>
                        {
                            row.RelativeItem().Padding(6).PaddingLeft(10).Text(t =>
                            {
                                t.Span("TOTAL A PAGAR GUARANÍES: ").Bold().FontSize(9);
                                t.Span(ConvertirMontoALetras((long)monto)).Bold().Italic().FontSize(9.5f);
                            });
                            row.ConstantItem(120).BorderLeft(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).AlignRight().Text(montoStr).Bold().FontSize(11);
                        });

                        // LIQUIDACIÓN DEL IVA
                        col.Item().BorderTop(1.5f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(6).PaddingLeft(10).Row(row =>
                        {
                            row.RelativeItem().Text("LIQUIDACIÓN DEL IVA:   (5%) 0").Bold().FontSize(8.5f);
                            row.RelativeItem().Text($"(10%) {iva10Str}").Bold().FontSize(8.5f);
                            row.RelativeItem().AlignRight().Text($"TOTAL IVA: {iva10Str}").Bold().FontSize(8.5f);
                        });

                        // FOOTER IMPRENTA LEGAL
                        col.Item().BorderTop(1f).BorderColor(QuestPDF.Helpers.Colors.Black).Padding(4).PaddingLeft(10).Row(row =>
                        {
                            row.RelativeItem().Text("GRAFICONT de Arnaldo Rojas - HAB. Nº 1.365 - RUC: 709715 - 5 - Gaspar R. de Francia 483 c/ Vapor Cué, Luque\n05 Tal. 50x3 - Numerados del 0001.551 al 0001.800 - E-mail: graficontajn@hotmail.com - Cel.: (0981) 749 696 - (0974) 749 696").FontSize(5.5f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                            row.ConstantItem(180).AlignRight().Text("ORIGINAL: CLIENTE\nDUPL.: NO VÁLIDO PARA CRÉDITO FISCAL\nTRIPL.: ARCHIVO TRIBUTARIO").FontSize(6f).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private string ConvertirMontoALetras(long monto)
        {
            if (monto == 0) return "CERO GUARANÍES";
            return NumeroALetras(monto) + " GUARANÍES";
        }

        private string NumeroALetras(long n)
        {
            if (n == 0) return "CERO";
            if (n < 0) return "MENOS " + NumeroALetras(Math.Abs(n));

            string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIUNO", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE" };
            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (n <= 29) return unidades[n];
            if (n < 100)
            {
                var d = n / 10;
                var u = n % 10;
                return decenas[d] + (u > 0 ? " Y " + unidades[u] : "");
            }
            if (n == 100) return "CIEN";
            if (n < 1000)
            {
                var c = n / 100;
                var r = n % 100;
                return centenas[c] + (r > 0 ? " " + NumeroALetras(r) : "");
            }
            if (n == 1000) return "MIL";
            if (n < 1000000)
            {
                var m = n / 1000;
                var r = n % 1000;
                var pref = (m == 1) ? "MIL" : NumeroALetras(m) + " MIL";
                return pref + (r > 0 ? " " + NumeroALetras(r) : "");
            }
            if (n == 1000000) return "UN MILLON";
            if (n < 1000000000000L)
            {
                var mill = n / 1000000;
                var r = n % 1000000;
                var pref = (mill == 1) ? "UN MILLON" : NumeroALetras(mill) + " MILLONES";
                return pref + (r > 0 ? " " + NumeroALetras(r) : "");
            }
            return n.ToString();
        }

        [HttpGet("alertas-vencimiento")]
        public async Task<IActionResult> GetAlertasVencimiento([FromQuery] int diasMaximos = 5)
        {
            var today = DateTime.Now.Date;
            var contratosActivos = await _context.ContratosInquilinos
                .Include(c => c.Inquilino)
                .Include(c => c.Unidad)
                .Include(c => c.Pagos)
                .Where(c => c.EstaActivo)
                .ToListAsync();

            var alertas = new List<object>();

            foreach (var contrato in contratosActivos)
            {
                var diaPago = contrato.DiaVencimiento > 0 ? contrato.DiaVencimiento : 10;
                int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
                var fechaVencimiento = new DateTime(today.Year, today.Month, Math.Min(diaPago, daysInMonth));

                if (today > fechaVencimiento)
                {
                    var proxMes = today.AddMonths(1);
                    fechaVencimiento = new DateTime(proxMes.Year, proxMes.Month, Math.Min(diaPago, DateTime.DaysInMonth(proxMes.Year, proxMes.Month)));
                }

                int diasRestantes = (int)(fechaVencimiento.Date - today).TotalDays;

                if (diasRestantes <= diasMaximos && diasRestantes >= -5)
                {
                    bool yaPago = contrato.Pagos.Any(p =>
                        p.AnioPagado == fechaVencimiento.Year &&
                        p.MesPagado == fechaVencimiento.Month);

                    if (!yaPago)
                    {
                        alertas.Add(new
                        {
                            contratoId = contrato.ContratoId,
                            unidadNombre = FormatearNombreUnidad(contrato.Unidad != null ? $"{contrato.Unidad.NombreCondominio} - {contrato.Unidad.CodigoUnidad}" : $"Unidad #{contrato.UnidadId}"),
                            inquilinoNombre = contrato.Inquilino?.RazonSocial ?? "Cliente",
                            email = contrato.Inquilino?.Email ?? string.Empty,
                            telefono = contrato.Inquilino?.Telefono ?? string.Empty,
                            montoMensual = contrato.MontoMensualAlquiler > 0 ? contrato.MontoMensualAlquiler : contrato.MontoMensualGs,
                            fechaVencimiento = fechaVencimiento.ToString("yyyy-MM-dd"),
                            diasRestantes = diasRestantes,
                            estadoAlerta = diasRestantes < 0 ? "VENCIDO" : (diasRestantes == 0 ? "VENCE HOY" : $"VENCE EN {diasRestantes} DÍAS")
                        });
                    }
                }
            }

            return Ok(alertas.OrderBy(a => ((dynamic)a).diasRestantes));
        }

        [HttpPost("enviar-alerta-vencimiento")]
        public async Task<IActionResult> EnviarAlertaVencimiento([FromBody] EnviarAlertaVencimientoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailDestino))
                return BadRequest(new { success = false, message = "El correo de destino es requerido." });

            request.NombreUnidad = FormatearNombreUnidad(request.NombreUnidad);

            var result = await _emailService.EnviarAlertaVencimientoAsync(
                request.EmailDestino,
                request.NombreCliente,
                request.NombreUnidad,
                request.MontoMensual,
                request.FechaVencimiento,
                request.DiasRestantes
            );

            if (result)
                return Ok(new { success = true, message = "Alerta de vencimiento enviada al inquilino y administrador correctamente." });

            return StatusCode(500, new { success = false, message = "Error al enviar la alerta por SMTP." });
        }

        [HttpPost("simular-alerta-5-dias")]
        public async Task<IActionResult> SimularAlerta5Dias([FromQuery] string? emailPrueba = null, [FromQuery] int? contratoId = null)
        {
            var today = DateTime.Now.Date;
            var contrato = contratoId.HasValue
                ? await _context.ContratosInquilinos.Include(c => c.Inquilino).Include(c => c.Unidad).FirstOrDefaultAsync(c => c.ContratoId == contratoId.Value)
                : await _context.ContratosInquilinos.Include(c => c.Inquilino).Include(c => c.Unidad)
                    .FirstOrDefaultAsync(c => c.EstaActivo && c.Unidad != null && (c.Unidad.NombreCondominio.Contains("Aguz") || c.Unidad.CodigoUnidad.StartsWith("D-")));

            if (contrato == null)
            {
                contrato = await _context.ContratosInquilinos.Include(c => c.Inquilino).Include(c => c.Unidad).FirstOrDefaultAsync(c => c.EstaActivo);
            }

            if (contrato == null)
                return NotFound(new { success = false, message = "No se encontró ningún contrato activo para la simulación." });

            string emailInquilino = !string.IsNullOrWhiteSpace(emailPrueba) ? emailPrueba : (contrato.Inquilino?.Email ?? "");
            string nombreInquilino = contrato.Inquilino?.RazonSocial ?? "Norma Villalba";
            string nombreUnidadRaw = contrato.Unidad != null ? $"{contrato.Unidad.NombreCondominio} - {contrato.Unidad.CodigoUnidad}" : "Aguz 2 - D-02";
            string nombreUnidad = FormatearNombreUnidad(nombreUnidadRaw);
            decimal monto = contrato.MontoMensualAlquiler > 0 ? contrato.MontoMensualAlquiler : contrato.MontoMensualGs;
            var fechaVencimiento = today.AddDays(5);

            var result = await _emailService.EnviarAlertaVencimientoAsync(
                emailInquilino,
                nombreInquilino,
                nombreUnidad,
                monto > 0 ? monto : 4500000m,
                fechaVencimiento,
                5
            );

            return Ok(new
            {
                success = result,
                message = result ? "Alerta simulada de 5 días enviada correctamente." : "Error al enviar la alerta.",
                datosEnviados = new
                {
                    emailDestino = emailInquilino,
                    cliente = nombreInquilino,
                    unidad = nombreUnidad,
                    monto = monto,
                    fechaVencimiento = fechaVencimiento.ToString("dd/MM/yyyy"),
                    diasRestantes = 5
                }
            });
        }

        private static string FormatearNombreUnidad(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;
            return Regex.Replace(
                texto,
                @"\bD\s*-?\s*(\d+)\b",
                m => $"Dúplex {int.Parse(m.Groups[1].Value):D2}",
                RegexOptions.IgnoreCase);
        }
    }

    public class EnviarAlertaVencimientoRequest
    {
        public string EmailDestino { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreUnidad { get; set; } = string.Empty;
        public decimal MontoMensual { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int DiasRestantes { get; set; }
    }

    public class EnviarComprobanteRequest
    {
        public string EmailDestino { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string? RucCliente { get; set; }
        public string? DireccionCliente { get; set; }
        public string NumeroComprobante { get; set; } = string.Empty;
        public string TipoEmision { get; set; } = "Factura";
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string? PdfBase64 { get; set; }
    }

    public class UpdateContratoRequest : ContratoRequest
    {
        public int ContratoId { get; set; }
        public int InquilinoId { get; set; }
    }

    public class ContratoRequest
    {
        public int EmpresaId { get; set; }
        public int UnidadId { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal MontoMensual { get; set; }
        public decimal Garantia { get; set; }
        public int CantidadOcupantes { get; set; }
        public string? Vehiculo { get; set; }
        public string? Observaciones { get; set; }
        public string? ServiciosIncluidos { get; set; }
    }
}

