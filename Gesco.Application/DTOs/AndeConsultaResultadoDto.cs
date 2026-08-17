using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class AndeFacturaItemDto
    {
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty; // Pendiente de Pago | Pagado
        public string Emision { get; set; } = string.Empty;
        public string Vencimiento { get; set; } = string.Empty;
        /// <summary>Fecha de cobro/pago si ANDE la informa; si no, puede quedar vacío.</summary>
        public string FechaPago { get; set; } = string.Empty;
        public int? ConsumoKw { get; set; }
        /// <summary>Parámetros para facturaElectronicaPdf / facturaPdf del portal ANDE.</summary>
        public string? SecNis { get; set; }
        public string? SecRec { get; set; }
        public string? FechaFacturacion { get; set; }
        public bool FacturaElectronica { get; set; } = true;
    }

    public class AndeConsultaResultadoDto
    {
        public string Nis { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public decimal SaldoDeuda { get; set; }
        public int ConsumoKw { get; set; }
        public int FacturasPendientes { get; set; }
        public bool TieneDesconexion { get; set; }
        public string EstadoConexion { get; set; } = string.Empty;
        public string EstadoPago { get; set; } = string.Empty;
        public DateTime FechaConsulta { get; set; } = DateTime.Now;
        public string Vencimiento { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool EsSimulado { get; set; } = false;
        public List<AndeFacturaItemDto> Facturas { get; set; } = new();
    }
}
