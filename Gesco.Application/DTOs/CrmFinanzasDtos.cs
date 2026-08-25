using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmContratoDto
    {
        public int ContratoId { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string SistemaServicio { get; set; } = string.Empty;
        public decimal FeeAcordado { get; set; }
        public decimal FeeMensual { get; set; }
        public decimal FeeAnual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? ProximoVencimiento { get; set; }
        public string EstadoPago { get; set; } = "Pendiente";
        public string EstadoContrato { get; set; } = "Activo";
        public bool EsRecurrente { get; set; }
        public string Periodicidad { get; set; } = "Mensual";
        public string? Notas { get; set; }
        public bool Activo { get; set; }
        public int CantidadPagos { get; set; }
        public decimal TotalCobrado { get; set; }
    }

    public class CrmContratoUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public string SistemaServicio { get; set; } = string.Empty;

        public decimal FeeAcordado { get; set; }
        public decimal FeeMensual { get; set; }
        public decimal FeeAnual { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        public DateTime? ProximoVencimiento { get; set; }
        public string? EstadoPago { get; set; }
        public string? EstadoContrato { get; set; }
        public bool EsRecurrente { get; set; } = true;
        public string? Periodicidad { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmPagoDto
    {
        public int PagoId { get; set; }
        public int EmpresaId { get; set; }
        public int ContratoId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string SistemaServicio { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string? Periodo { get; set; }
        public string? MetodoPago { get; set; }
        public string? Concepto { get; set; }
        public string Estado { get; set; } = "Registrado";
        public string? Notas { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmPagoCreateRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ContratoId { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaPago { get; set; }

        public string? Periodo { get; set; }
        public string? MetodoPago { get; set; }
        public string? Concepto { get; set; }
        public string? Notas { get; set; }
        public bool MarcarContratoAlDia { get; set; } = true;
        public DateTime? NuevoProximoVencimiento { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmIngresosResumenDto
    {
        public decimal MrrActivo { get; set; }
        public decimal ArrProyectado { get; set; }
        public decimal CobradoMesActual { get; set; }
        public decimal CobradoTotal { get; set; }
        public int ContratosActivos { get; set; }
        public int ContratosPendientesPago { get; set; }
        public int ContratosVencidos { get; set; }
        public List<CrmContratoDto> Recurrentes { get; set; } = new();
        public List<CrmPagoDto> UltimosPagos { get; set; } = new();
    }
}
