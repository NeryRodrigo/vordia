namespace Gesco.Application.DTOs
{
    public class GastoServicioMantenimientoDto
    {
        public int GastoId { get; set; }
        public int UnidadId { get; set; }
        /// <summary>Luz, Agua o Basura.</summary>
        public string TipoServicio { get; set; } = "";
        public string? IdentificadorServicio { get; set; }
        public string? NroComprobante { get; set; }
        public int? MesReferencia { get; set; }
        public int? AnioReferencia { get; set; }
        public string? MetodoPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Proveedor { get; set; }
    }

    public class GastoServicioMantenimientoRequest
    {
        public int UnidadId { get; set; }
        /// <summary>Valores permitidos: Luz, Agua, Basura.</summary>
        public string TipoServicio { get; set; } = "";
        public string? IdentificadorServicio { get; set; }
        public string? NroComprobante { get; set; }
        public int? MesReferencia { get; set; }
        public int? AnioReferencia { get; set; }
        public string? MetodoPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Proveedor { get; set; }
    }
}
