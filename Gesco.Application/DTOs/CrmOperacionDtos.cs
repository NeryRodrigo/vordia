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

        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmRoadmapDto
    {
        public int RoadmapId { get; set; }
        public int EmpresaId { get; set; }
        public string Tipo { get; set; } = "Idea";
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = "Borrador";
        public string Prioridad { get; set; } = "P2";
        public int? ResponsableUsuarioId { get; set; }
        public string? ResponsableNombre { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaMeta { get; set; }
        public string? ProximoPaso { get; set; }
        public DateTime? FechaProximoPaso { get; set; }
        public string? ConvertidoA { get; set; }
        public string? Notas { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public bool VencidoProximoPaso { get; set; }
        public bool VencidaMeta { get; set; }
        public List<CrmRoadmapHistorialDto> Historial { get; set; } = new();
    }

    public class CrmRoadmapHistorialDto
    {
        public int HistorialId { get; set; }
        public string Tipo { get; set; } = "nota";
        public string Titulo { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? EstadoAnterior { get; set; }
        public string? EstadoNuevo { get; set; }
        public string? UsuarioNombre { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CrmRoadmapUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public string Tipo { get; set; } = "Idea";

        [Required]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
        public string? Estado { get; set; }
        public string? Prioridad { get; set; }
        public int? ResponsableUsuarioId { get; set; }
        public string? ResponsableNombre { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaMeta { get; set; }
        public string? ProximoPaso { get; set; }
        public DateTime? FechaProximoPaso { get; set; }
        public string? ConvertidoA { get; set; }
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmRoadmapNotaRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public string Titulo { get; set; } = string.Empty;

        public string? Detalle { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }
}
