using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Gesco.Application.Services.Conta
{
    /// <summary>Datos leídos de un comprobante antes de asignarlo a un cliente.</summary>
    public class ComprobanteLeido
    {
        public string? Cdc { get; set; }
        public bool Electronico { get; set; }
        public int TipoComprobante { get; set; } = 109;
        public string Timbrado { get; set; } = "";
        public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Credito { get; set; }
        public string EmisorRuc { get; set; } = "";
        public string? EmisorDv { get; set; }
        public string? EmisorNombre { get; set; }
        public string? ReceptorRuc { get; set; }
        public string? ReceptorDv { get; set; }
        public string? ReceptorNombre { get; set; }
        /// <summary>Tipo de identificación del receptor (Tabla 3 RG90): 11 RUC, 12 cédula, 15 sin nombre.</summary>
        public int ReceptorTipoId { get; set; } = 11;
        public decimal Gravado10 { get; set; }
        public decimal Gravado5 { get; set; }
        public decimal Exento { get; set; }
        public decimal Total { get; set; }
        public decimal Iva10 { get; set; }
        public decimal Iva5 { get; set; }
        public List<string> Avisos { get; } = new();
    }

    /// <summary>
    /// Lectura precisa de comprobantes paraguayos: dígito verificador (módulo 11), CDC de 44 dígitos,
    /// QR del KuDE y XML del documento electrónico (SIFEN). No usa IA: los datos salen del propio documento.
    /// </summary>
    public static class ContaParser
    {
        /// <summary>Dígito verificador de la SET (módulo 11, base máxima 11). Sirve para RUC y CDC.</summary>
        public static int DigitoVerificador(string numero, int baseMax = 11)
        {
            var sb = new StringBuilder();
            foreach (var c in numero.ToUpperInvariant())
                sb.Append(char.IsDigit(c) ? c.ToString() : ((int)c).ToString(CultureInfo.InvariantCulture));
            var digitos = sb.ToString();
            int total = 0, k = 2;
            for (var i = digitos.Length - 1; i >= 0; i--)
            {
                if (k > baseMax) k = 2;
                total += (digitos[i] - '0') * k;
                k++;
            }
            var resto = total % 11;
            return resto > 1 ? 11 - resto : 0;
        }

        public static bool RucValido(string ruc, string dv) =>
            !string.IsNullOrWhiteSpace(ruc) && int.TryParse(dv, out var d) && DigitoVerificador(ruc.Trim()) == d;

        /// <summary>Separa "80012345-6" en RUC y DV.</summary>
        public static (string ruc, string? dv) SepararRuc(string? texto)
        {
            var t = (texto ?? "").Trim().Replace(" ", "");
            var i = t.LastIndexOf('-');
            return i > 0 && i == t.Length - 2 ? (t[..i], t[(i + 1)..]) : (t, null);
        }

        /// <summary>Tipo de documento electrónico (iTiDE) → código de comprobante de la RG90.</summary>
        public static int TipoRg90(int iTiDE) => iTiDE switch
        {
            1 => 109, // factura electrónica
            4 => 101, // autofactura electrónica
            5 => 110, // nota de crédito
            6 => 111, // nota de débito
            _ => 109
        };

        /// <summary>
        /// CDC (44 dígitos): tipo(2) RUC emisor(8) DV(1) establecimiento(3) punto(3) número(7)
        /// tipo contribuyente(1) fecha AAAAMMDD(8) tipo emisión(1) código de seguridad(9) DV del CDC(1).
        /// </summary>
        public static ComprobanteLeido? LeerCdc(string cdc, out string? error)
        {
            error = null;
            cdc = (cdc ?? "").Trim();
            if (cdc.Length != 44 || !cdc.All(char.IsDigit)) { error = "El CDC debe tener 44 dígitos"; return null; }
            if (DigitoVerificador(cdc[..43]) != cdc[43] - '0') { error = "El CDC no es válido (dígito verificador incorrecto)"; return null; }
            if (!DateTime.TryParseExact(cdc.Substring(25, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            { error = "El CDC tiene una fecha inválida"; return null; }
            return new ComprobanteLeido
            {
                Cdc = cdc,
                Electronico = true,
                TipoComprobante = TipoRg90(int.Parse(cdc[..2], CultureInfo.InvariantCulture)),
                EmisorRuc = cdc.Substring(2, 8).TrimStart('0'),
                EmisorDv = cdc.Substring(10, 1),
                Numero = $"{cdc.Substring(11, 3)}-{cdc.Substring(14, 3)}-{cdc.Substring(17, 7)}",
                Fecha = fecha
            };
        }

        private static decimal Num(string? s) =>
            decimal.TryParse((s ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        /// <summary>
        /// Texto del QR del KuDE (https://ekuatia.set.gov.py/consultas/qr?nVersion=…&amp;Id=CDC&amp;dTotGralOpe=…&amp;dTotIVA=…).
        /// El QR trae el total y el IVA total, no el desglose: si no se puede deducir con certeza, se avisa.
        /// </summary>
        public static ComprobanteLeido? LeerQr(string texto, out string? error)
        {
            error = null;
            texto = (texto ?? "").Trim();
            var q = texto.IndexOf('?');
            if (q < 0) { error = "No es un QR de factura electrónica (falta el link de e-Kuatia)"; return null; }
            var p = HttpUtility.ParseQueryString(texto[(q + 1)..]);
            var id = p["Id"] ?? p["id"];
            if (string.IsNullOrWhiteSpace(id)) { error = "El QR no trae el CDC"; return null; }
            var c = LeerCdc(id, out error);
            if (c == null) return null;

            // dFeEmiDE viene en hexadecimal (texto "AAAA-MM-DDTHH:MM:SS")
            var feHex = p["dFeEmiDE"];
            if (!string.IsNullOrWhiteSpace(feHex) && feHex.Length % 2 == 0)
            {
                try
                {
                    var bytes = Enumerable.Range(0, feHex.Length / 2).Select(i => Convert.ToByte(feHex.Substring(i * 2, 2), 16)).ToArray();
                    if (DateTime.TryParse(Encoding.ASCII.GetString(bytes), CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)) c.Fecha = f.Date;
                }
                catch (FormatException) { /* se usa la fecha del CDC */ }
            }

            var rucRec = p["dRucRec"];
            if (!string.IsNullOrWhiteSpace(rucRec)) c.ReceptorRuc = rucRec.Trim();
            else if (!string.IsNullOrWhiteSpace(p["dNumIDRec"])) { c.ReceptorRuc = p["dNumIDRec"]!.Trim(); c.ReceptorTipoId = 12; }
            else c.ReceptorTipoId = 15;

            c.Total = Math.Round(Num(p["dTotGralOpe"]));
            var iva = Math.Round(Num(p["dTotIVA"]));
            InferirDesglose(c, iva);
            return c;
        }

        /// <summary>Con total e IVA total: si cierra exacto con 10% o 5% puro se asume ese desglose; si no, se avisa.</summary>
        private static void InferirDesglose(ComprobanteLeido c, decimal ivaTotal)
        {
            if (ivaTotal <= 0) { c.Exento = c.Total; return; }
            if (Math.Abs(Math.Round(c.Total / 11m) - ivaTotal) <= 1) { c.Gravado10 = c.Total; c.Iva10 = ivaTotal; return; }
            if (Math.Abs(Math.Round(c.Total / 21m) - ivaTotal) <= 1) { c.Gravado5 = c.Total; c.Iva5 = ivaTotal; return; }
            // Mezcla de tasas o exentos: se estima 10% sobre la base que justifica el IVA y el resto exento
            c.Gravado10 = Math.Min(c.Total, ivaTotal * 11m);
            c.Iva10 = ivaTotal;
            c.Exento = c.Total - c.Gravado10;
            c.Avisos.Add("El QR solo trae el total y el IVA: verificá el desglose 10% / 5% / exento con el XML o la factura.");
        }

        private static string? V(XElement root, string nombre) =>
            root.Descendants().FirstOrDefault(e => e.Name.LocalName == nombre)?.Value?.Trim();

        private static XElement? E(XElement root, string nombre) =>
            root.Descendants().FirstOrDefault(e => e.Name.LocalName == nombre);

        /// <summary>XML del documento electrónico (SIFEN, Manual Técnico v150): lectura completa con desglose exacto.</summary>
        public static List<ComprobanteLeido> LeerXml(string contenido, out List<string> errores)
        {
            errores = new List<string>();
            var lista = new List<ComprobanteLeido>();
            XDocument doc;
            try { doc = XDocument.Parse(contenido); }
            catch (System.Xml.XmlException) { errores.Add("El archivo no es un XML válido"); return lista; }

            var des = doc.Descendants().Where(e => e.Name.LocalName == "DE").ToList();
            if (des.Count == 0) { errores.Add("El XML no contiene un documento electrónico (DE) del SIFEN"); return lista; }

            foreach (var de in des)
            {
                var cdc = de.Attribute("Id")?.Value;
                var c = cdc != null ? LeerCdc(cdc, out var err) : null;
                if (c == null) { errores.Add($"Documento con CDC inválido{(cdc != null ? $" ({cdc})" : "")}"); continue; }

                var timb = E(de, "gTimb");
                if (timb != null)
                {
                    c.Timbrado = V(timb, "dNumTim") ?? "";
                    var est = V(timb, "dEst"); var pun = V(timb, "dPunExp"); var num = V(timb, "dNumDoc");
                    if (est != null && pun != null && num != null) c.Numero = $"{est.PadLeft(3, '0')}-{pun.PadLeft(3, '0')}-{num.PadLeft(7, '0')}";
                    if (int.TryParse(V(timb, "iTiDE"), out var tide)) c.TipoComprobante = TipoRg90(tide);
                }
                if (DateTime.TryParse(V(de, "dFeEmiDE"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var fe)) c.Fecha = fe.Date;

                var emis = E(de, "gEmis");
                if (emis != null)
                {
                    c.EmisorRuc = V(emis, "dRucEm") ?? c.EmisorRuc;
                    c.EmisorDv = V(emis, "dDVEmi") ?? c.EmisorDv;
                    c.EmisorNombre = V(emis, "dNomEmi");
                }
                var rec = E(de, "gDatRec");
                if (rec != null)
                {
                    c.ReceptorRuc = V(rec, "dRucRec") ?? V(rec, "dNumIDRec");
                    c.ReceptorDv = V(rec, "dDVRec");
                    c.ReceptorNombre = V(rec, "dNomRec");
                    c.ReceptorTipoId = V(rec, "dRucRec") != null ? 11 : V(rec, "dNumIDRec") != null ? 12 : 15;
                }
                c.Credito = V(de, "iCondOpe") == "2";

                var tot = E(de, "gTotSub");
                if (tot != null)
                {
                    decimal sub10 = Num(V(tot, "dSub10")), sub5 = Num(V(tot, "dSub5"));
                    decimal exe = Num(V(tot, "dSubExe")) + Num(V(tot, "dSubExo"));
                    c.Gravado10 = Math.Round(sub10);
                    c.Gravado5 = Math.Round(sub5);
                    c.Exento = Math.Round(exe);
                    c.Iva10 = Math.Round(Num(V(tot, "dIVA10")));
                    c.Iva5 = Math.Round(Num(V(tot, "dIVA5")));
                    c.Total = Math.Round(Num(V(tot, "dTotGralOpe")));
                    if (c.Total == 0) c.Total = c.Gravado10 + c.Gravado5 + c.Exento;
                }
                var moneda = V(de, "cMoneOpe");
                if (moneda != null && moneda != "PYG")
                    c.Avisos.Add($"Operación en moneda extranjera ({moneda}): revisá los montos en guaraníes.");
                lista.Add(c);
            }
            return lista;
        }

        /// <summary>Validaciones aritméticas y de identificación. Devuelve las observaciones encontradas.</summary>
        public static List<string> Validar(decimal g10, decimal g5, decimal exento, decimal total, decimal iva10, decimal iva5,
            DateTime fecha, string contraparteDoc, string? contraparteDv, int contraparteTipoId, string numero, string timbrado, bool electronico)
        {
            var obs = new List<string>();
            if (total <= 0) obs.Add("El total debe ser mayor a cero.");
            if (Math.Abs(g10 + g5 + exento - total) > 1) obs.Add($"El total ({total:N0}) no coincide con gravado 10% + gravado 5% + exento ({g10 + g5 + exento:N0}).");
            if (g10 > 0 && iva10 > 0 && Math.Abs(Math.Round(g10 / 11m) - iva10) > 2) obs.Add($"El IVA 10% ({iva10:N0}) no corresponde al gravado ({Math.Round(g10 / 11m):N0} esperado).");
            if (g5 > 0 && iva5 > 0 && Math.Abs(Math.Round(g5 / 21m) - iva5) > 2) obs.Add($"El IVA 5% ({iva5:N0}) no corresponde al gravado ({Math.Round(g5 / 21m):N0} esperado).");
            if (fecha > DateTime.UtcNow.Date.AddDays(1)) obs.Add("La fecha es futura.");
            if (fecha < new DateTime(2021, 1, 1)) obs.Add("La RG90 no admite fechas anteriores al 01/01/2021 (salvo crédito).");
            if (contraparteTipoId == 11 && !string.IsNullOrWhiteSpace(contraparteDv) && !RucValido(contraparteDoc, contraparteDv))
                obs.Add($"El RUC {contraparteDoc}-{contraparteDv} no es válido (dígito verificador).");
            if (!electronico)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(numero ?? "", @"^\d{3}-\d{3}-\d{7}$")) obs.Add("El número debe tener el formato 001-001-0000001.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(timbrado ?? "", @"^\d{8}$")) obs.Add("El timbrado debe tener 8 dígitos.");
            }
            return obs;
        }
    }
}
