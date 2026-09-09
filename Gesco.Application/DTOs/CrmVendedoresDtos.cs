using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmVendedorDto
    {
        public int VendedorId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public decimal ComisionPct { get; set; }
        public decimal MetaMensualGs { get; set; }
        public string Estado { get; set; } = "Activo";
        public DateTime? FechaIngreso { get; set; }
        public string? Notas { get; set; }

        // --- Condición de socio ---
        public bool EsSocio { get; set; }
        public int? SocioId { get; set; }
        public DateTime? FechaSocioDesde { get; set; }
        /// <summary>Participación como socio. Solo tiene valor si EsSocio.</summary>
        public decimal? PorcentajeSocio { get; set; }

        // --- Ventas reales, calculadas desde las propuestas que cerró ---
        /// <summary>Suma de propuestas ganadas, de todos los tiempos.</summary>
        public decimal VentasTotalGs { get; set; }
        /// <summary>Propuestas ganadas con cierre dentro del mes en curso.</summary>
        public decimal VentasMesGs { get; set; }
        /// <summary>Propuestas que siguen sin respuesta.</summary>
        public decimal EnNegociacionGs { get; set; }
        public int PropuestasAbiertas { get; set; }
        public int PropuestasGanadas { get; set; }
        public int PropuestasPerdidas { get; set; }
        /// <summary>Comisión sobre lo vendido en el mes.</summary>
        public decimal ComisionMesGs { get; set; }
        /// <summary>Avance sobre la meta del mes, 0..100+. Null si no tiene meta.</summary>
        public int? CumplimientoMetaPct { get; set; }
    }

    public class CrmVendedorUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";

        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public decimal ComisionPct { get; set; }
        public decimal MetaMensualGs { get; set; }
        public string Estado { get; set; } = "Activo";
        public DateTime? FechaIngreso { get; set; }
        public string? Notas { get; set; }
    }

    /// <summary>Decisión de los dueños: este vendedor pasa a ser socio.</summary>
    public class CrmVendedorPromoverRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        /// <summary>Participación que se le asigna como socio.</summary>
        [Required]
        public decimal Porcentaje { get; set; }

        public string? UsuarioNombre { get; set; }
    }

    public class CrmVendedoresResumenDto
    {
        public int CantidadActivos { get; set; }
        public int CantidadSocios { get; set; }
        public decimal VentasMesGs { get; set; }
        public decimal VentasTotalGs { get; set; }
        public decimal ComisionesMesGs { get; set; }
        public decimal MetaMensualTotalGs { get; set; }
        public List<CrmVendedorDto> Vendedores { get; set; } = new();
    }
}
