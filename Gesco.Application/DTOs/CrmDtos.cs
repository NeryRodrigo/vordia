using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmClienteDto
    {
        public int ClienteId { get; set; }
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? Rubro { get; set; }
        public string? Documento { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoCargo { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoTelefono { get; set; }
        public string EstadoPipeline { get; set; } = "Prospecto";
        public string? SistemaServicio { get; set; }
        public string? ProximosPasos { get; set; }
        public DateTime? FechaEstimadaCierre { get; set; }
        public decimal? FeeEstimadoMensual { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public int? UsuarioResponsableId { get; set; }
        public int CantidadHistorial { get; set; }
    }

    public class CrmClienteUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(200)]
        public string RazonSocial { get; set; } = string.Empty;

        public string? NombreComercial { get; set; }
        public string? Rubro { get; set; }
        public string? Documento { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoCargo { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoTelefono { get; set; }
        public string? EstadoPipeline { get; set; }
        public string? SistemaServicio { get; set; }
        public string? ProximosPasos { get; set; }
        public DateTime? FechaEstimadaCierre { get; set; }
        public decimal? FeeEstimadoMensual { get; set; }
        public string? Notas { get; set; }
        public int? UsuarioResponsableId { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmCambiarEstadoRequest
    {
        [Required]
        public string EstadoPipeline { get; set; } = string.Empty;
        public string? Nota { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmHistorialDto
    {
        public int HistorialId { get; set; }
        public int ClienteId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? EstadoAnterior { get; set; }
        public string? EstadoNuevo { get; set; }
        public DateTime Fecha { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmHistorialCreateRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public string Tipo { get; set; } = "nota";

        [Required]
        public string Titulo { get; set; } = string.Empty;

        public string? Detalle { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmPipelineResumenDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public List<CrmClienteDto> Clientes { get; set; } = new();
    }
}
