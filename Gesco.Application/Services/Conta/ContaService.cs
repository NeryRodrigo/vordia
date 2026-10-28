using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Services.Conta
{
    public class ContaClienteRequest
    {
        public string RazonSocial { get; set; } = "";
        /// <summary>RUC con o sin DV ("80012345-6").</summary>
        public string Ruc { get; set; } = "";
        public string? Regimen { get; set; }
        public bool ObligacionIva { get; set; } = true;
        public bool ObligacionIre { get; set; } = true;
        public bool ObligacionIrp { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoWhatsapp { get; set; }
        public string? ContactoEmail { get; set; }
    }

    public class ContaComprobanteRequest
    {
        public int ClienteId { get; set; }
        public string Tipo { get; set; } = "Compra";
        public int TipoComprobante { get; set; } = 109;
        public string Timbrado { get; set; } = "";
        public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Credito { get; set; }
        public int ContraparteTipoId { get; set; } = 11;
        /// <summary>RUC o documento de la contraparte ("80012345-6" o cédula).</summary>
        public string Contraparte { get; set; } = "";
        public string? ContraparteNombre { get; set; }
        public decimal Gravado10 { get; set; }
        public decimal Gravado5 { get; set; }
        public decimal Exento { get; set; }
        public string? NumeroAsociado { get; set; }
        public string? TimbradoAsociado { get; set; }
        public bool? ImputaIva { get; set; }
        public bool? ImputaIre { get; set; }
        public bool? ImputaIrp { get; set; }
    }

    public record ArchivoSubido(string Nombre, byte[] Contenido);

    public class ResultadoCarga
    {
        public int Cargados { get; set; }
        public int Aprobados { get; set; }
        public int Observados { get; set; }
        public int Duplicados { get; set; }
        public List<string> Mensajes { get; } = new();
    }

    public class PanelClienteDto
    {
        public int ClienteId { get; set; }
        public string RazonSocial { get; set; } = "";
        public string Ruc { get; set; } = "";
        public int Ventas { get; set; }
        public int Compras { get; set; }
        public int Observados { get; set; }
        public decimal IvaDebito { get; set; }
        public decimal IvaCredito { get; set; }
        public decimal IvaSaldo { get; set; }
        public DateTime Vencimiento { get; set; }
        public int DiasParaVencer { get; set; }
        /// <summary>verde | amarillo | rojo</summary>
        public string Semaforo { get; set; } = "verde";
        public string? Accion { get; set; }
        public string TokenPortal { get; set; } = "";
        public string? ContactoWhatsapp { get; set; }
    }

    /// <summary>
    /// VORDIA CONTA: el estudio contable carga comprobantes de sus clientes (XML, QR, portal o manual),
    /// el sistema los valida, arma los libros, estima el IVA, avisa los vencimientos y genera el archivo RG90
    /// según la Especificación Técnica de la DNIT (junio 2021).
    /// </summary>
    public class ContaService
    {
        private readonly IApplicationDbContext _db;
        public ContaService(IApplicationDbContext db) => _db = db;

        private static readonly CultureInfo Py = CultureInfo.GetCultureInfo("es-PY");
        public static int PeriodoDe(DateTime f) => f.Year * 100 + f.Month;
        /// <summary>Período en curso: el mes que se está juntando y vence el mes que viene.</summary>
        public static int PeriodoActualDeclarar(DateTime hoy) => PeriodoDe(hoy);

        /// <summary>
        /// Vencimiento mensual (IVA y registro de comprobantes) según el calendario perpetuo de la DNIT por último
        /// dígito del RUC: 0→7, 1→9, 2→11 … 9→25 del mes siguiente al período; si cae en fin de semana, pasa al lunes.
        /// </summary>
        public static DateTime Vencimiento(string ruc, int periodo)
        {
            var ultimo = ruc.LastOrDefault(char.IsDigit);
            var dia = 7 + 2 * (ultimo == default ? 0 : ultimo - '0');
            var mes = new DateTime(periodo / 100, periodo % 100, 1).AddMonths(1);
            var f = new DateTime(mes.Year, mes.Month, dia);
            while (f.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) f = f.AddDays(1);
            return f;
        }

        private static string NuevoToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        private static string? Corto(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);

        // ───────────── Clientes ─────────────

        public Task<List<ContaCliente>> ClientesAsync(int empresaId) =>
            _db.ContaClientes.AsNoTracking().Where(c => c.EmpresaId == empresaId && c.Activo).OrderBy(c => c.RazonSocial).ToListAsync();

        public async Task<ContaCliente> GuardarClienteAsync(int empresaId, ContaClienteRequest r, int? clienteId = null)
        {
            var (ruc, dv) = ContaParser.SepararRuc(r.Ruc);
            if (string.IsNullOrWhiteSpace(r.RazonSocial)) throw new InvalidOperationException("La razón social es obligatoria");
            if (string.IsNullOrWhiteSpace(ruc) || !ruc.All(char.IsLetterOrDigit)) throw new InvalidOperationException("Ingresá el RUC (ej. 80012345-6)");
            var dvCalc = ContaParser.DigitoVerificador(ruc).ToString(CultureInfo.InvariantCulture);
            if (dv != null && dv != dvCalc) throw new InvalidOperationException($"El dígito verificador del RUC {ruc} es {dvCalc}, no {dv}");
            if (!(r.ObligacionIva || r.ObligacionIre || r.ObligacionIrp)) throw new InvalidOperationException("Marcá al menos una obligación (IVA, IRE o IRP)");
            if (await _db.ContaClientes.AnyAsync(c => c.EmpresaId == empresaId && c.Activo && c.Ruc == ruc && c.ClienteId != (clienteId ?? 0)))
                throw new InvalidOperationException("Ese RUC ya está cargado en el estudio");

            var c = clienteId.HasValue
                ? await _db.ContaClientes.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException()
                : new ContaCliente { EmpresaId = empresaId, TokenPortal = NuevoToken(), FechaAlta = DateTime.UtcNow };
            c.RazonSocial = Corto(r.RazonSocial, 250)!;
            c.Ruc = ruc;
            c.Dv = dvCalc;
            c.Regimen = Corto(r.Regimen, 30);
            c.ObligacionIva = r.ObligacionIva;
            c.ObligacionIre = r.ObligacionIre;
            c.ObligacionIrp = r.ObligacionIrp;
            c.ContactoNombre = Corto(r.ContactoNombre, 150);
            c.ContactoWhatsapp = Corto(r.ContactoWhatsapp, 40);
            c.ContactoEmail = Corto(r.ContactoEmail, 150);
            if (!clienteId.HasValue) _db.ContaClientes.Add(c);
            await _db.SaveChangesAsync();
            return c;
        }

        // ───────────── Panel ─────────────

        public async Task<List<PanelClienteDto>> PanelAsync(int empresaId, int periodo)
        {
            var hoy = DateTime.UtcNow.AddHours(-3).Date;
            var clientes = await ClientesAsync(empresaId);
            var comp = await _db.ContaComprobantes.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Periodo == periodo)
                .Select(x => new { x.ClienteId, x.Tipo, x.Estado, x.Iva10, x.Iva5, x.ImputaIva })
                .ToListAsync();

            return clientes.Select(c =>
            {
                var de = comp.Where(x => x.ClienteId == c.ClienteId).ToList();
                // Solo liquida IVA quien tiene la obligación activa
                var debito = c.ObligacionIva ? de.Where(x => x.Tipo == "Venta").Sum(x => x.Iva10 + x.Iva5) : 0;
                var credito = c.ObligacionIva ? de.Where(x => x.Tipo == "Compra" && x.ImputaIva).Sum(x => x.Iva10 + x.Iva5) : 0;
                var vence = Vencimiento(c.Ruc, periodo);
                var dias = (vence - hoy).Days;
                var observados = de.Count(x => x.Estado == "Observado");
                var sinMovimiento = de.Count == 0;
                var semaforo = observados > 0 && dias <= 5 ? "rojo"
                    : sinMovimiento && dias <= 5 ? "rojo"
                    : observados > 0 || sinMovimiento ? "amarillo" : "verde";
                string? accion = observados > 0 ? $"Revisar {observados} comprobante{(observados == 1 ? "" : "s")} observado{(observados == 1 ? "" : "s")}"
                    : sinMovimiento ? "Pedir las facturas del mes al cliente"
                    : dias >= 0 ? "Listo para generar el RG90 y presentar"
                    : $"Venció el {vence:dd/MM}: confirmá que se presentó";
                return new PanelClienteDto
                {
                    ClienteId = c.ClienteId, RazonSocial = c.RazonSocial, Ruc = $"{c.Ruc}-{c.Dv}",
                    Ventas = de.Count(x => x.Tipo == "Venta"), Compras = de.Count(x => x.Tipo == "Compra"), Observados = observados,
                    IvaDebito = debito, IvaCredito = credito, IvaSaldo = debito - credito,
                    Vencimiento = vence, DiasParaVencer = dias, Semaforo = semaforo, Accion = accion,
                    TokenPortal = c.TokenPortal, ContactoWhatsapp = c.ContactoWhatsapp
                };
            })
            .OrderBy(p => p.Semaforo == "rojo" ? 0 : p.Semaforo == "amarillo" ? 1 : 2).ThenBy(p => p.DiasParaVencer)
            .ToList();
        }

        // ───────────── Comprobantes ─────────────

        public Task<List<ContaComprobante>> ComprobantesAsync(int empresaId, int? clienteId, int? periodo, string? estado, string? tipo) =>
            _db.ContaComprobantes.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId
                            && (clienteId == null || x.ClienteId == clienteId)
                            && (periodo == null || x.Periodo == periodo)
                            && (estado == null || x.Estado == estado)
                            && (tipo == null || x.Tipo == tipo))
                .OrderBy(x => x.Estado == "Observado" ? 0 : 1).ThenByDescending(x => x.FechaCarga)
                .Take(2000).ToListAsync();

        /// <summary>Asigna un comprobante leído al cliente del estudio según el RUC emisor (venta) o receptor (compra).</summary>
        private static (ContaCliente? cliente, string tipo) Asignar(ComprobanteLeido l, List<ContaCliente> clientes, ContaCliente? forzado)
        {
            if (forzado != null)
            {
                if (forzado.Ruc == l.EmisorRuc) return (forzado, "Venta");
                if (forzado.Ruc == l.ReceptorRuc) return (forzado, "Compra");
                return (null, "");
            }
            var venta = clientes.FirstOrDefault(c => c.Ruc == l.EmisorRuc);
            if (venta != null) return (venta, "Venta");
            var compra = clientes.FirstOrDefault(c => c.Ruc == l.ReceptorRuc);
            return compra != null ? (compra, "Compra") : (null, "");
        }

        private async Task<bool> EsDuplicadoAsync(int clienteId, string tipo, string? cdc, string doc, string timbrado, string numero, int? excluirId = null)
        {
            if (!string.IsNullOrWhiteSpace(cdc))
                return await _db.ContaComprobantes.AnyAsync(x => x.ClienteId == clienteId && x.Cdc == cdc && x.ComprobanteId != (excluirId ?? 0));
            return await _db.ContaComprobantes.AnyAsync(x => x.ClienteId == clienteId && x.Tipo == tipo && x.ContraparteDoc == doc
                && x.Timbrado == timbrado && x.Numero == numero && x.ComprobanteId != (excluirId ?? 0));
        }

        private static void Imputacion(ContaComprobante x, ContaCliente c, bool? iva = null, bool? ire = null, bool? irp = null)
        {
            x.ImputaIva = iva ?? c.ObligacionIva;
            x.ImputaIre = ire ?? c.ObligacionIre;
            x.ImputaIrp = irp ?? c.ObligacionIrp;
            if (!(x.ImputaIva || x.ImputaIre || x.ImputaIrp)) x.ImputaIva = true;
        }

        private static void Revalidar(ContaComprobante x, IEnumerable<string>? avisos = null)
        {
            var obs = ContaParser.Validar(x.Gravado10, x.Gravado5, x.Exento, x.Total, x.Iva10, x.Iva5, x.Fecha,
                x.ContraparteDoc, x.ContraparteDv, x.ContraparteTipoId, x.Numero, x.Timbrado, x.Electronico);
            if (!x.Electronico && x.TipoComprobante is 110 or 111 && (string.IsNullOrWhiteSpace(x.NumeroAsociado) || string.IsNullOrWhiteSpace(x.TimbradoAsociado)))
                obs.Add("Las notas de crédito y débito necesitan el número y timbrado de la factura asociada.");
            if (avisos != null) obs.AddRange(avisos);
            x.Observaciones = obs.Count == 0 ? null : Corto(string.Join(" ", obs), 1000);
            x.Estado = obs.Count == 0 ? "Aprobado" : "Observado";
        }

        private async Task<int> GuardarLeidosAsync(int empresaId, List<ComprobanteLeido> leidos, string origen, string? archivo, string? usuario, ContaCliente? forzado, ResultadoCarga res)
        {
            var clientes = forzado != null ? new List<ContaCliente> { forzado } : await _db.ContaClientes.Where(c => c.EmpresaId == empresaId && c.Activo).ToListAsync();
            foreach (var l in leidos)
            {
                var (cliente, tipo) = Asignar(l, clientes, forzado);
                if (cliente == null)
                {
                    res.Mensajes.Add(forzado != null
                        ? $"{l.Numero}: la factura no está a nombre de {forzado.RazonSocial} (RUC {forzado.Ruc})."
                        : $"{l.Numero}: ningún cliente del estudio tiene el RUC {l.EmisorRuc} (emisor) ni {l.ReceptorRuc ?? "—"} (receptor).");
                    continue;
                }
                var contraDoc = tipo == "Venta" ? (l.ReceptorRuc ?? "") : l.EmisorRuc;
                var contraDv = tipo == "Venta" ? l.ReceptorDv : l.EmisorDv;
                if (await EsDuplicadoAsync(cliente.ClienteId, tipo, l.Cdc, contraDoc, l.Timbrado, l.Numero))
                {
                    res.Duplicados++;
                    continue;
                }
                var x = new ContaComprobante
                {
                    EmpresaId = empresaId, ClienteId = cliente.ClienteId, Tipo = tipo, Origen = origen, Electronico = l.Electronico, Cdc = l.Cdc,
                    TipoComprobante = l.TipoComprobante, Timbrado = l.Timbrado, Numero = l.Numero, Fecha = l.Fecha.Date, Credito = l.Credito,
                    ContraparteTipoId = tipo == "Venta" ? l.ReceptorTipoId : 11, ContraparteDoc = contraDoc, ContraparteDv = contraDv,
                    ContraparteNombre = Corto(tipo == "Venta" ? l.ReceptorNombre : l.EmisorNombre, 250),
                    Gravado10 = l.Gravado10, Gravado5 = l.Gravado5, Exento = l.Exento, Total = l.Total, Iva10 = l.Iva10, Iva5 = l.Iva5,
                    Periodo = PeriodoDe(l.Fecha), ArchivoNombre = Corto(archivo, 150), CargadoPor = Corto(usuario, 150), FechaCarga = DateTime.UtcNow
                };
                Imputacion(x, cliente);
                Revalidar(x, l.Avisos);
                _db.ContaComprobantes.Add(x);
                res.Cargados++;
                if (x.Estado == "Aprobado") res.Aprobados++; else res.Observados++;
            }
            await _db.SaveChangesAsync();
            return res.Cargados;
        }

        /// <summary>Carga XML del SIFEN (sueltos o dentro de un ZIP).</summary>
        public async Task<ResultadoCarga> ImportarXmlAsync(int empresaId, IEnumerable<ArchivoSubido> archivos, string? usuario, ContaCliente? forzado = null, string origen = "XML")
        {
            var res = new ResultadoCarga();
            foreach (var a in archivos)
            {
                var xmls = new List<(string nombre, string texto)>();
                if (a.Nombre.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var zip = new ZipArchive(new MemoryStream(a.Contenido), ZipArchiveMode.Read);
                    foreach (var e in zip.Entries.Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).Take(500))
                    {
                        if (e.Length > 5_000_000) { res.Mensajes.Add($"{e.Name}: archivo demasiado grande"); continue; }
                        using var sr = new StreamReader(e.Open(), Encoding.UTF8);
                        xmls.Add((e.Name, await sr.ReadToEndAsync()));
                    }
                }
                else xmls.Add((a.Nombre, Encoding.UTF8.GetString(a.Contenido)));

                foreach (var (nombre, texto) in xmls)
                {
                    var leidos = ContaParser.LeerXml(texto, out var errores);
                    res.Mensajes.AddRange(errores.Select(e => $"{nombre}: {e}"));
                    await GuardarLeidosAsync(empresaId, leidos, origen, nombre, usuario, forzado, res);
                }
            }
            return res;
        }

        /// <summary>Carga por QR del KuDE (texto leído con la cámara o pegado).</summary>
        public async Task<ResultadoCarga> ImportarQrAsync(int empresaId, string texto, string? usuario, ContaCliente? forzado = null, string origen = "QR")
        {
            var res = new ResultadoCarga();
            var l = ContaParser.LeerQr(texto, out var error);
            if (l == null) { res.Mensajes.Add(error ?? "QR inválido"); return res; }
            await GuardarLeidosAsync(empresaId, new List<ComprobanteLeido> { l }, origen, null, usuario, forzado, res);
            return res;
        }

        /// <summary>Carga manual de comprobantes preimpresos (no electrónicos).</summary>
        public async Task<ContaComprobante> CrearManualAsync(int empresaId, ContaComprobanteRequest r, string? usuario)
        {
            var cliente = await _db.ContaClientes.FirstOrDefaultAsync(c => c.ClienteId == r.ClienteId && c.EmpresaId == empresaId) ?? throw new KeyNotFoundException();
            var x = new ContaComprobante { EmpresaId = empresaId, ClienteId = cliente.ClienteId, Origen = "Manual", CargadoPor = Corto(usuario, 150), FechaCarga = DateTime.UtcNow };
            await AplicarAsync(x, r, cliente);
            _db.ContaComprobantes.Add(x);
            await _db.SaveChangesAsync();
            return x;
        }

        public async Task<ContaComprobante?> EditarAsync(int empresaId, int comprobanteId, ContaComprobanteRequest r)
        {
            var x = await _db.ContaComprobantes.FirstOrDefaultAsync(c => c.ComprobanteId == comprobanteId && c.EmpresaId == empresaId);
            if (x == null) return null;
            var cliente = await _db.ContaClientes.FirstAsync(c => c.ClienteId == x.ClienteId);
            r.ClienteId = x.ClienteId;
            await AplicarAsync(x, r, cliente, comprobanteId);
            await _db.SaveChangesAsync();
            return x;
        }

        private async Task AplicarAsync(ContaComprobante x, ContaComprobanteRequest r, ContaCliente cliente, int? excluirId = null)
        {
            if (r.Tipo is not ("Compra" or "Venta")) throw new InvalidOperationException("Tipo inválido");
            var (doc, dv) = ContaParser.SepararRuc(r.Contraparte);
            if (string.IsNullOrWhiteSpace(doc)) throw new InvalidOperationException("Ingresá el RUC o documento de la contraparte");
            x.Tipo = r.Tipo;
            x.TipoComprobante = r.TipoComprobante;
            x.Timbrado = (r.Timbrado ?? "").Trim();
            x.Numero = (r.Numero ?? "").Trim();
            x.Fecha = r.Fecha.Date;
            x.Credito = r.Credito;
            x.ContraparteTipoId = r.ContraparteTipoId;
            x.ContraparteDoc = doc;
            x.ContraparteDv = dv ?? (r.ContraparteTipoId == 11 ? ContaParser.DigitoVerificador(doc).ToString(CultureInfo.InvariantCulture) : null);
            x.ContraparteNombre = Corto(r.ContraparteNombre, 250);
            x.NumeroAsociado = Corto(r.NumeroAsociado, 20);
            x.TimbradoAsociado = Corto(r.TimbradoAsociado, 8);
            x.Gravado10 = Math.Round(r.Gravado10);
            x.Gravado5 = Math.Round(r.Gravado5);
            x.Exento = Math.Round(r.Exento);
            x.Total = x.Gravado10 + x.Gravado5 + x.Exento;
            x.Iva10 = Math.Round(x.Gravado10 / 11m);
            x.Iva5 = Math.Round(x.Gravado5 / 21m);
            x.Periodo = PeriodoDe(x.Fecha);
            Imputacion(x, cliente, r.ImputaIva, r.ImputaIre, r.ImputaIrp);
            if (!x.Electronico && await EsDuplicadoAsync(cliente.ClienteId, x.Tipo, null, doc, x.Timbrado, x.Numero, excluirId))
                throw new InvalidOperationException("Ese comprobante ya está cargado para este cliente");
            Revalidar(x);
        }

        public async Task<ContaComprobante?> AprobarAsync(int empresaId, int comprobanteId)
        {
            var x = await _db.ContaComprobantes.FirstOrDefaultAsync(c => c.ComprobanteId == comprobanteId && c.EmpresaId == empresaId);
            if (x == null) return null;
            x.Estado = "Aprobado";
            x.Observaciones = x.Observaciones == null ? null : Corto($"Aprobado por el contador. {x.Observaciones}", 1000);
            await _db.SaveChangesAsync();
            return x;
        }

        public async Task<bool> EliminarAsync(int empresaId, int comprobanteId)
        {
            var x = await _db.ContaComprobantes.FirstOrDefaultAsync(c => c.ComprobanteId == comprobanteId && c.EmpresaId == empresaId);
            if (x == null) return false;
            _db.ContaComprobantes.Remove(x);
            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────── Libros y RG90 ─────────────

        private static string Monto(decimal m) => Math.Round(m).ToString("0", CultureInfo.InvariantCulture);
        private static string SN(bool b) => b ? "S" : "N";

        /// <summary>
        /// Archivo RG90 (Especificación Técnica DNIT, junio 2021): TXT delimitado por tabulaciones, UTF-8, sin encabezado,
        /// comprimido en ZIP con el mismo nombre: &lt;RUC&gt;_REG_MMAAAA_XXXXX. Solo comprobantes NO electrónicos y aprobados:
        /// los electrónicos y virtuales Marangatu ya los tiene.
        /// </summary>
        public async Task<(string archivo, byte[] zip, int filas, int electronicos, int sinAprobar)> ExportarRg90Async(int empresaId, int clienteId, int periodo, string? usuario)
        {
            var c = await _db.ContaClientes.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException();
            var todos = await _db.ContaComprobantes.AsNoTracking().Where(x => x.ClienteId == clienteId && x.Periodo == periodo).OrderBy(x => x.Tipo).ThenBy(x => x.Fecha).ToListAsync();
            var electronicos = todos.Count(x => x.Electronico);
            var sinAprobar = todos.Count(x => !x.Electronico && x.Estado != "Aprobado");
            var filas = todos.Where(x => !x.Electronico && x.Estado == "Aprobado").Take(5000).ToList();
            if (filas.Count == 0) throw new InvalidOperationException(electronicos > 0
                ? "No hay comprobantes preimpresos aprobados: los electrónicos ya están en Marangatu y no van en el archivo."
                : "No hay comprobantes aprobados para ese período.");

            var sb = new StringBuilder();
            foreach (var x in filas)
            {
                var nombre = x.ContraparteTipoId is 11 or 12 or 15 ? "" : (x.ContraparteNombre ?? "");
                var numero = x.TipoComprobante is 112 or 106 ? "" : x.Numero;
                var asociado = x.TipoComprobante is 110 or 111;
                var campos = new List<string>
                {
                    x.Tipo == "Venta" ? "1" : "2",
                    x.ContraparteTipoId.ToString(CultureInfo.InvariantCulture),
                    x.ContraparteDoc,
                    nombre,
                    x.TipoComprobante.ToString(CultureInfo.InvariantCulture),
                    x.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    x.Timbrado,
                    numero,
                    Monto(x.Gravado10), Monto(x.Gravado5), Monto(x.Exento), Monto(x.Total),
                    x.Credito ? "2" : "1",
                    "N",
                    SN(x.ImputaIva), SN(x.ImputaIre), SN(x.ImputaIrp)
                };
                if (x.Tipo == "Compra") campos.Add("N"); // NO IMPUTA
                campos.Add(asociado ? x.NumeroAsociado ?? "" : "");
                campos.Add(asociado ? x.TimbradoAsociado ?? "" : "");
                sb.Append(string.Join('\t', campos)).Append("\r\n");
            }

            var mmaaaa = $"{periodo % 100:00}{periodo / 100}";
            var n = await _db.ContaExportaciones.CountAsync(e => e.ClienteId == clienteId && e.Periodo == periodo) + 1;
            var archivo = $"{c.Ruc}_REG_{mmaaaa}_V{n:0000}";
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var e = zip.CreateEntry($"{archivo}.txt");
                await using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                await w.WriteAsync(sb.ToString());
            }
            _db.ContaExportaciones.Add(new ContaExportacion { EmpresaId = empresaId, ClienteId = clienteId, Periodo = periodo, Archivo = archivo, Filas = filas.Count, GeneradoPor = Corto(usuario, 150), Fecha = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return (archivo, ms.ToArray(), filas.Count, electronicos, sinAprobar);
        }

        /// <summary>Libro de compras o ventas en CSV para Excel (separador ; y UTF-8 con BOM).</summary>
        public async Task<(string archivo, byte[] csv)> LibroCsvAsync(int empresaId, int clienteId, int periodo, string tipo)
        {
            var c = await _db.ContaClientes.FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException();
            var filas = await _db.ContaComprobantes.AsNoTracking().Where(x => x.ClienteId == clienteId && x.Periodo == periodo && x.Tipo == tipo).OrderBy(x => x.Fecha).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Fecha;Tipo;Timbrado;Número;RUC/Documento;Razón social;Gravado 10%;IVA 10%;Gravado 5%;IVA 5%;Exento;Total;Condición;Electrónico;CDC;Estado;Observaciones");
            foreach (var x in filas)
                sb.AppendLine(string.Join(';', new[]
                {
                    x.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), x.TipoComprobante.ToString(CultureInfo.InvariantCulture), x.Timbrado, x.Numero,
                    x.ContraparteDv != null ? $"{x.ContraparteDoc}-{x.ContraparteDv}" : x.ContraparteDoc,
                    (x.ContraparteNombre ?? "").Replace(';', ','),
                    Monto(x.Gravado10), Monto(x.Iva10), Monto(x.Gravado5), Monto(x.Iva5), Monto(x.Exento), Monto(x.Total),
                    x.Credito ? "Crédito" : "Contado", x.Electronico ? "Sí" : "No", x.Cdc ?? "", x.Estado, (x.Observaciones ?? "").Replace(';', ',')
                }));
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return ($"Libro_{tipo}s_{c.Ruc}_{periodo % 100:00}{periodo / 100}.csv", bytes);
        }

        // ───────────── Portal del cliente (link privado) ─────────────

        public async Task<(ContaCliente cliente, string estudio)?> PortalAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length != 32) return null;
            var c = await _db.ContaClientes.FirstOrDefaultAsync(x => x.TokenPortal == token && x.Activo);
            if (c == null) return null;
            var estudio = await _db.Empresas.Where(e => e.EmpresaId == c.EmpresaId).Select(e => e.RazonSocial).FirstOrDefaultAsync() ?? "Tu estudio contable";
            return (c, estudio);
        }

        public async Task<int> PortalRecibidosMesAsync(int clienteId)
        {
            var desde = DateTime.UtcNow.AddDays(-31);
            return await _db.ContaComprobantes.CountAsync(x => x.ClienteId == clienteId && x.Origen == "Portal" && x.FechaCarga >= desde);
        }

        // ───────────── Datos de demostración ─────────────

        private static string RucDemo(int baseNum) => baseNum.ToString(CultureInfo.InvariantCulture);
        private static string DvDe(string ruc) => ContaParser.DigitoVerificador(ruc).ToString(CultureInfo.InvariantCulture);

        /// <summary>Genera un CDC con estructura y dígito verificador válidos (solo para datos de demostración).</summary>
        private static string CdcDemo(int tipo, string rucEmisor, string dv, int est, int pun, int num, DateTime fecha, Random rnd)
        {
            var baseCdc = $"{tipo:00}{rucEmisor.PadLeft(8, '0')}{dv}{est:000}{pun:000}{num:0000000}2{fecha:yyyyMMdd}1{rnd.Next(100000000, 999999999)}";
            return baseCdc + ContaParser.DigitoVerificador(baseCdc);
        }

        /// <summary>Carga clientes y comprobantes de ejemplo (mes a declarar y mes actual) para una demo inmediata.</summary>
        public async Task<int> CargarDemoAsync(int empresaId)
        {
            if (await _db.ContaClientes.AnyAsync(c => c.EmpresaId == empresaId)) throw new InvalidOperationException("El estudio ya tiene clientes: la demo solo se carga en un estudio vacío");
            var rnd = new Random(2026);
            var clientes = new[]
            {
                ("Panadería San Blas S.R.L.", 80091234, "IRE General", true, true, false, "0981 123 456"),
                ("Ferretería Los Andes S.A.", 80074512, "IRE General", true, true, false, "0982 555 210"),
                ("Lucía Benítez (Consultorio odontológico)", 4567891, "IRP", true, false, true, "0971 334 908"),
                ("Agroveterinaria El Corral E.A.S.", 80110678, "IRE Simple", true, true, false, "0985 778 112"),
                ("Hostel Cordillera", 80098765, "IRE Resimple", false, true, false, "0991 620 444"),
                ("Transportes Ypacaraí S.A.", 80057321, "IRE General", true, true, false, "0983 401 117"),
            }.Select(t => new ContaCliente
            {
                EmpresaId = empresaId, RazonSocial = t.Item1, Ruc = RucDemo(t.Item2), Dv = DvDe(RucDemo(t.Item2)), Regimen = t.Item3,
                ObligacionIva = t.Item4, ObligacionIre = t.Item5, ObligacionIrp = t.Item6, ContactoWhatsapp = t.Item7,
                TokenPortal = NuevoToken(), FechaAlta = DateTime.UtcNow
            }).ToList();
            _db.ContaClientes.AddRange(clientes);
            await _db.SaveChangesAsync();

            var proveedores = new[]
            {
                ("Distribuidora Central S.A.", 80012457), ("Combustibles del Este S.A.", 80033218), ("Insumos Médicos Guaraní S.R.L.", 80045672),
                ("Imprenta Rápida E.A.S.", 80067123), ("Servicios de Internet Py S.A.", 80022894), ("Mayorista La Cosecha S.A.", 80081236)
            };
            var hoy = DateTime.UtcNow.AddHours(-3).Date;
            var mesDeclarar = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);
            var lista = new List<ContaComprobante>();
            var numVenta = 1000;

            foreach (var c in clientes)
            {
                foreach (var mes in new[] { mesDeclarar, new DateTime(hoy.Year, hoy.Month, 1) })
                {
                    var diasMes = mes.Month == hoy.Month && mes.Year == hoy.Year ? Math.Max(1, hoy.Day - 1) : DateTime.DaysInMonth(mes.Year, mes.Month);
                    var cantVentas = c.Regimen == "IRP" ? 3 : 5;
                    for (var i = 0; i < cantVentas; i++)
                    {
                        var fecha = mes.AddDays(rnd.Next(0, diasMes));
                        var g10 = rnd.Next(30, 900) * 10_000m;
                        var electronico = i % 2 == 0;
                        var receptor = proveedores[rnd.Next(proveedores.Length)];
                        var rucR = RucDemo(receptor.Item2);
                        numVenta++;
                        lista.Add(Nuevo(c, "Venta", electronico, electronico ? CdcDemo(1, c.Ruc, c.Dv, 1, 1, numVenta, fecha, rnd) : null,
                            fecha, rucR, DvDe(rucR), receptor.Item1, g10, 0, 0, $"001-001-{numVenta:0000000}"));
                    }
                    for (var i = 0; i < 6; i++)
                    {
                        var p = proveedores[(i + c.ClienteId) % proveedores.Length];
                        var rucP = RucDemo(p.Item2);
                        var fecha = mes.AddDays(rnd.Next(0, diasMes));
                        var g10 = rnd.Next(10, 300) * 10_000m;
                        var g5 = i == 2 ? rnd.Next(5, 80) * 10_000m : 0;
                        var electronico = i % 3 != 0;
                        var num = rnd.Next(1, 99999);
                        lista.Add(Nuevo(c, "Compra", electronico, electronico ? CdcDemo(1, rucP, DvDe(rucP), 1, 2, num, fecha, rnd) : null,
                            fecha, rucP, DvDe(rucP), p.Item1, g10, g5, i == 4 ? 50_000m : 0, $"001-002-{num:0000000}"));
                    }
                }
            }

            // Casos para mostrar la validación automática
            var obsIva = lista.First(x => x.Tipo == "Compra" && !x.Electronico && x.Periodo == PeriodoDe(mesDeclarar));
            obsIva.Iva10 += 15_000;
            var obsRuc = lista.Last(x => x.Tipo == "Compra" && !x.Electronico && x.Periodo == PeriodoDe(mesDeclarar));
            obsRuc.ContraparteDv = ((int.Parse(obsRuc.ContraparteDv!, CultureInfo.InvariantCulture) + 1) % 10).ToString(CultureInfo.InvariantCulture);
            foreach (var x in lista) Revalidar(x);

            _db.ContaComprobantes.AddRange(lista);
            await _db.SaveChangesAsync();
            return lista.Count;

            ContaComprobante Nuevo(ContaCliente c, string tipo, bool electronico, string? cdc, DateTime fecha, string doc, string dv, string nombre, decimal g10, decimal g5, decimal exento, string numero)
            {
                var x = new ContaComprobante
                {
                    EmpresaId = empresaId, ClienteId = c.ClienteId, Tipo = tipo, Origen = "Demo", Electronico = electronico, Cdc = cdc,
                    TipoComprobante = 109, Timbrado = (17000000 + rnd.Next(100000, 999999)).ToString(CultureInfo.InvariantCulture), Numero = numero,
                    Fecha = fecha, ContraparteTipoId = 11, ContraparteDoc = doc, ContraparteDv = dv, ContraparteNombre = nombre,
                    Gravado10 = g10, Gravado5 = g5, Exento = exento, Total = g10 + g5 + exento,
                    Iva10 = Math.Round(g10 / 11m), Iva5 = Math.Round(g5 / 21m), Periodo = PeriodoDe(fecha), CargadoPor = "Demo", FechaCarga = DateTime.UtcNow
                };
                Imputacion(x, c);
                return x;
            }
        }
    }
}
