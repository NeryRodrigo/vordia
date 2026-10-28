using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>VORDIA CONTA · Cada contribuyente (RUC) que lleva el estudio contable.</summary>
    [Table("ContaClientes")]
    public class ContaCliente
    {
        [Key] public int ClienteId { get; set; }
        /// <summary>Empresa del estudio contable (tenant).</summary>
        public int EmpresaId { get; set; }
        [Required, StringLength(250)] public string RazonSocial { get; set; } = "";
        /// <summary>RUC sin dígito verificador.</summary>
        [Required, StringLength(20)] public string Ruc { get; set; } = "";
        [Required, StringLength(1)] public string Dv { get; set; } = "";
        /// <summary>IVA General | IRE General | IRE Simple | IRE Resimple | IRP</summary>
        [StringLength(30)] public string? Regimen { get; set; }
        public bool ObligacionIva { get; set; } = true;
        public bool ObligacionIre { get; set; } = true;
        public bool ObligacionIrp { get; set; }
        [StringLength(150)] public string? ContactoNombre { get; set; }
        [StringLength(40)] public string? ContactoWhatsapp { get; set; }
        [StringLength(150)] public string? ContactoEmail { get; set; }
        /// <summary>Link privado para que el cliente suba sus facturas desde el celular.</summary>
        [Required, StringLength(40)] public string TokenPortal { get; set; } = "";
        public bool Activo { get; set; } = true;
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Comprobante de compra o venta de un cliente del estudio (factura, nota de crédito, etc.).</summary>
    [Table("ContaComprobantes")]
    public class ContaComprobante
    {
        [Key] public int ComprobanteId { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        /// <summary>Compra | Venta</summary>
        [Required, StringLength(10)] public string Tipo { get; set; } = "Compra";
        /// <summary>XML | QR | Portal | Manual | Demo</summary>
        [Required, StringLength(20)] public string Origen { get; set; } = "Manual";
        /// <summary>Electrónico (e-Kuatia): no va en el archivo RG90 porque Marangatu ya lo tiene.</summary>
        public bool Electronico { get; set; }
        [StringLength(44)] public string? Cdc { get; set; }
        /// <summary>Código RG90 (Tabla 4): 109 factura, 110 nota de crédito, 111 nota de débito, 101 autofactura…</summary>
        public int TipoComprobante { get; set; } = 109;
        [StringLength(8)] public string Timbrado { get; set; } = "";
        /// <summary>Formato ###-###-#######.</summary>
        [StringLength(20)] public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Credito { get; set; }
        /// <summary>Código RG90 (Tabla 3): 11 RUC, 12 cédula, 15 sin nombre…</summary>
        public int ContraparteTipoId { get; set; } = 11;
        [StringLength(20)] public string ContraparteDoc { get; set; } = "";
        [StringLength(1)] public string? ContraparteDv { get; set; }
        [StringLength(250)] public string? ContraparteNombre { get; set; }
        /// <summary>Montos en guaraníes, IVA incluido en los gravados (como pide la RG90).</summary>
        public decimal Gravado10 { get; set; }
        public decimal Gravado5 { get; set; }
        public decimal Exento { get; set; }
        public decimal Total { get; set; }
        public decimal Iva10 { get; set; }
        public decimal Iva5 { get; set; }
        /// <summary>Para notas de crédito y débito: comprobante de venta asociado (###-###-#######) y su timbrado.</summary>
        [StringLength(20)] public string? NumeroAsociado { get; set; }
        [StringLength(8)] public string? TimbradoAsociado { get; set; }
        public bool ImputaIva { get; set; } = true;
        public bool ImputaIre { get; set; }
        public bool ImputaIrp { get; set; }
        /// <summary>Pendiente | Aprobado | Observado</summary>
        [Required, StringLength(20)] public string Estado { get; set; } = "Pendiente";
        [StringLength(1000)] public string? Observaciones { get; set; }
        /// <summary>Período fiscal AAAAMM.</summary>
        public int Periodo { get; set; }
        [StringLength(150)] public string? ArchivoNombre { get; set; }
        [StringLength(150)] public string? CargadoPor { get; set; }
        public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Archivos RG90 generados (trazabilidad y numeración del identificador del archivo).</summary>
    [Table("ContaExportaciones")]
    public class ContaExportacion
    {
        [Key] public int ExportacionId { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        public int Periodo { get; set; }
        [Required, StringLength(60)] public string Archivo { get; set; } = "";
        public int Filas { get; set; }
        [StringLength(150)] public string? GeneradoPor { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
