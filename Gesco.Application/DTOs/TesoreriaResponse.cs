using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class FlujoCajaResponse
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal BalanceNeto => TotalIngresos - TotalEgresos;
        public List<MovimientoFinanciero> Movimientos { get; set; } = new List<MovimientoFinanciero>();
    }

    public class MovimientoFinanciero
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty; // "INGRESO" o "EGRESO"
        public string Categoria { get; set; } = string.Empty; // "Venta POS", "Compra Proveedor"
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int IdOrigen { get; set; }
    }
}
