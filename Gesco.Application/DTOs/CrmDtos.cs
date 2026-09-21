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
        public string? Origen { get; set; }
        public string? OrigenDetalle { get; set; }
        public string? MensajeInicial { get; set; }
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
        public string? Origen { get; set; }
        public string? OrigenDetalle { get; set; }
        public string? MensajeInicial { get; set; }
        public int? UsuarioResponsableId { get; set; }
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    /// <summary>Solicitud del formulario público de vordia.com.py — endpoint anónimo.</summary>
    public class CrmLeadWebRequest
    {
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Empresa { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Cargo { get; set; }

        [StringLength(80)]
        public string? Ciudad { get; set; }

        /// <summary>Código de solución: HABITAT, STAY, DINE, RETAIL, BOUTIQUE, OPS, AGRO o "hablar".</summary>
        [StringLength(120)]
        public string? Sector { get; set; }

        /// <summary>Escala aproximada: 1-10, 11-50, 51-200, 200+.</summary>
        [StringLength(40)]
        public string? Tamano { get; set; }

        /// <summary>planillas | otro_sistema | mixto | nada</summary>
        [StringLength(40)]
        public string? GestionActual { get; set; }

        [StringLength(40)]
        public string? HorarioContacto { get; set; }

        [StringLength(2000)]
        public string? Necesidad { get; set; }

        /// <summary>Campo trampa: los bots lo completan, las personas no lo ven.</summary>
        public string? Sitio { get; set; }
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
