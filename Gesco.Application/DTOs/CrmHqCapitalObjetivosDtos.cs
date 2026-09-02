using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class CrmCapitalDto
    {
        public int CapitalId { get; set; }
        public int EmpresaId { get; set; }
        public string Tipo { get; set; } = "";
        public decimal MontoGs { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = "";
        public int? SocioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmCapitalUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Tipo { get; set; } = "Aporte";
        public decimal MontoGs { get; set; }
        public DateTime? Fecha { get; set; }
        public string Concepto { get; set; } = "";
        public int? SocioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? Notas { get; set; }
    }

    public class CrmCapitalResumenDto
    {
        public decimal SaldoCapitalGs { get; set; }
        public decimal TotalAportes { get; set; }
        public decimal TotalRetiros { get; set; }
        public List<CrmCapitalDto> Movimientos { get; set; } = new();
    }

    public class CrmEmpleadoHqDto
    {
        public int EmpleadoHqId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string RolCargo { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public decimal SalarioMensualGs { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
    }

    public class CrmEmpleadoHqUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string RolCargo { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public decimal SalarioMensualGs { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
    }

    public class CrmEmpleadosResumenDto
    {
        public int CantidadActivos { get; set; }
        public decimal NominaMensualGs { get; set; }
        public List<CrmEmpleadoHqDto> Empleados { get; set; } = new();
    }

    public class CrmObjetivoDto
    {
        public int ObjetivoId { get; set; }
        public int EmpresaId { get; set; }
        public string Ambito { get; set; } = "Empresa";
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? NotasReunion { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string Prioridad { get; set; } = "Media";
        public DateTime? FechaObjetivo { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public int? TareaId { get; set; }
        public string? TareaTitulo { get; set; }
        public string? CreadoPor { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class CrmObjetivoUpsertRequest
    {
        public int EmpresaId { get; set; }
        public string Ambito { get; set; } = "Empresa";
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? NotasReunion { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string Prioridad { get; set; } = "Media";
        public DateTime? FechaObjetivo { get; set; }
        public int? ClienteId { get; set; }
        public int? TareaId { get; set; }
        public string? CreadoPor { get; set; }
    }
}
