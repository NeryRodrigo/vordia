using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class CrmProductoDto
    {
        public int ProductoId { get; set; }
        public int EmpresaId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Significado { get; set; }
        public string? QueSera { get; set; }
        public string? QueIncluye { get; set; }
        public string? IdeaCentral { get; set; }
        public string Estado { get; set; } = "Planificado";
        public string Prioridad { get; set; } = "P2";
        public decimal FeeEstimadoGs { get; set; }
        public int Orden { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmProductoUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Significado { get; set; }
        public string? QueSera { get; set; }
        public string? QueIncluye { get; set; }
        public string? IdeaCentral { get; set; }
        public string Estado { get; set; } = "Planificado";
        public string Prioridad { get; set; } = "P2";
        public decimal FeeEstimadoGs { get; set; }
        public int Orden { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmProveedorDto
    {
        public int ProveedorId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "General";
        public string? ContactoNombre { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Ruc { get; set; }
        public string? ProductoCodigo { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
    }

    public class CrmProveedorUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "General";
        public string? ContactoNombre { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Ruc { get; set; }
        public string? ProductoCodigo { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
    }

    public class CrmProyectoDto
    {
        public int ProyectoId { get; set; }
        public int EmpresaId { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? ProductoCodigo { get; set; }
        public string Estado { get; set; } = "Planificado";
        public string Prioridad { get; set; } = "P1";
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? ResponsableNombre { get; set; }
        public int AvancePct { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmProyectoUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? ProductoCodigo { get; set; }
        public string Estado { get; set; } = "Planificado";
        public string Prioridad { get; set; } = "P1";
        public int? ClienteId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? ResponsableNombre { get; set; }
        public int AvancePct { get; set; }
        public string? Notas { get; set; }
    }
}
