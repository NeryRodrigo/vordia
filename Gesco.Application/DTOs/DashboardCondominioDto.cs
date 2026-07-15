using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class DashboardCondominioDto
    {
        public DashboardSummaryDto Summary { get; set; }
        public List<DashboardDetailDto> Ocupados { get; set; } = new();
        public List<DashboardDetailDto> Disponibles { get; set; } = new();
        public List<DashboardDetailDto> ObjetivosMes { get; set; } = new();
        public List<DashboardDetailDto> Cobrados { get; set; } = new();
        public List<DashboardDetailDto> FaltaCobrar { get; set; } = new();
        public List<DashboardDetailDto> Vencimientos { get; set; } = new();
        public List<DashboardDetailDto> Morosos { get; set; } = new();
    }

    public class DashboardSummaryDto
    {
        public int TotalOcupados { get; set; }
        public int TotalDisponibles { get; set; }
        public decimal ObjetivoMes { get; set; }
        public decimal CobradoMes { get; set; }
        public decimal FaltaCobrar { get; set; }
        public decimal TotalVencimientos { get; set; }
        public decimal TotalMorosos { get; set; }
        public int TotalCuotasPendientes { get; set; }
    }

    public class DashboardDetailDto
    {
        public int UnidadId { get; set; }

        private string _duplex = string.Empty;
        public string Duplex
        {
            get => _duplex;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) _duplex = value ?? "S/D";
                else _duplex = System.Text.RegularExpressions.Regex.Replace(value, @"\bD\s*-?\s*(\d+)\b", m => $"Dúplex {int.Parse(m.Groups[1].Value):D2}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }
        public string Condominio { get; set; }
        public string Inquilino { get; set; }
        public string Cedula { get; set; }
        public string NisAnde { get; set; }
        public int? Ocupantes { get; set; }
        public decimal Monto { get; set; }
        public decimal? InteresAcumulado { get; set; }
        public decimal? MontoRestante { get; set; }
        public bool FacturaEmitida { get; set; }
        public string EstadoFactura => FacturaEmitida ? "Emitida" : "Pendiente";
        public string TipoPago { get; set; }
        public DateTime? Fecha { get; set; }
        
        public bool AndePagadoEsteMes { get; set; }
        public decimal AndeMontoDeuda { get; set; }
        public DateTime? AndeFechaPago { get; set; }
        public bool AndeFacturaEmitida { get; set; }
        public DateTime? AndeFechaVencimiento { get; set; }
        public int AndeConsumoKw { get; set; }
    }
}
