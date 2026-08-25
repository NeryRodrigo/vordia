using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmTareaDto
    {
        public int TareaId { get; set; }
        public int EmpresaId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string Prioridad { get; set; } = "Media";
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public int? ContratoId { get; set; }
        public int? AsignadoUsuarioId { get; set; }
        public string? AsignadoNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCompletada { get; set; }
        public bool Vencida { get; set; }
    }

    public class CrmTareaUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? Estado { get; set; }
        public string? Prioridad { get; set; }
        public int? ClienteId { get; set; }
        public int? ContratoId { get; set; }
        public int? AsignadoUsuarioId { get; set; }
        public string? AsignadoNombre { get; set; }
    }

    public class CrmSocioDto
    {
        public int SocioId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal Porcentaje { get; set; }
        public int? UsuarioId { get; set; }
        public bool Activo { get; set; }
    }

    public class CrmSocioUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string? Email { get; set; }

        [Required]
        public decimal Porcentaje { get; set; }

        public int? UsuarioId { get; set; }
    }

    public class CrmFlujoMesDto
    {
        public string Periodo { get; set; } = string.Empty;
        public decimal Ingresos { get; set; }
        public int CantidadPagos { get; set; }
    }

    public class CrmRepartoSocioDto
    {
        public int SocioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
        public decimal MontoMes { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoMrrProyectado { get; set; }
    }

    public class CrmFlujoCajaDto
    {
        public decimal CobradoMesActual { get; set; }
        public decimal CobradoTotal { get; set; }
        public decimal MrrActivo { get; set; }
        public decimal ArrProyectado { get; set; }
        public decimal PendienteCobro { get; set; }
        public decimal SumaPorcentajesSocios { get; set; }
        public List<CrmFlujoMesDto> UltimosMeses { get; set; } = new();
        public List<CrmRepartoSocioDto> Reparto { get; set; } = new();
        public List<CrmSocioDto> Socios { get; set; } = new();
    }

    public class CrmEstadoSimpleRequest
    {
        [Required]
        public string Estado { get; set; } = string.Empty;
    }
}
