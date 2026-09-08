using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmGastoDto
    {
        public int GastoId { get; set; }
        public int EmpresaId { get; set; }
        public string Concepto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal MontoGs { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int? ProveedorId { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? MetodoPago { get; set; }
        public string? NroComprobante { get; set; }
        public string? Notas { get; set; }
        public string? UsuarioNombre { get; set; }
        /// <summary>True si está pendiente y su vencimiento ya pasó.</summary>
        public bool Vencido { get; set; }
        /// <summary>Días hasta el vencimiento. Negativo si ya venció.</summary>
        public int? DiasParaVencer { get; set; }
    }

    public class CrmGastoUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required, StringLength(200)]
        public string Concepto { get; set; } = "";

        public string Categoria { get; set; } = "Operativo";

        [Required]
        public decimal MontoGs { get; set; }

        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int? ProveedorId { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? MetodoPago { get; set; }
        public string? NroComprobante { get; set; }
        public string? Notas { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmGastoPagoRequest
    {
        [Required]
        public int EmpresaId { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? MetodoPago { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmGastosResumenDto
    {
        /// <summary>Gastos con FechaPago dentro del mes en curso.</summary>
        public decimal PagadoMesActual { get; set; }
        public decimal PagadoTotal { get; set; }
        /// <summary>Todo lo que sigue en estado Pendiente.</summary>
        public decimal PendienteTotal { get; set; }
        public int CantidadPendientes { get; set; }
        public decimal VenceProximos7Dias { get; set; }
        public int CantidadVenceProximos7Dias { get; set; }
        public decimal VencidoTotal { get; set; }
        public int CantidadVencidos { get; set; }
        public List<CrmGastoDto> Gastos { get; set; } = new();
    }
}
