using System;

namespace Gesco.Application.DTOs
{
    public class AndeConsultaResultadoDto
    {
        public string Nis { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
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
    }
}
