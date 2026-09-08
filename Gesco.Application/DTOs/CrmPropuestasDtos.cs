using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    public class CrmPropuestaDto
    {
        public int PropuestaId { get; set; }
        public int EmpresaId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal MontoGs { get; set; }
        public string Estado { get; set; } = "Enviada";
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? Notas { get; set; }
        public string? UsuarioNombre { get; set; }
        /// <summary>Estado del cliente en el pipeline al momento de la consulta.</summary>
        public string? EstadoPipelineCliente { get; set; }
    }

    public class CrmPropuestaUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required, StringLength(300)]
        public string Descripcion { get; set; } = "";

        [Required]
        public decimal MontoGs { get; set; }

        /// <summary>Enviada | Ganada | Perdida</summary>
        public string Estado { get; set; } = "Enviada";

        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? Notas { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class CrmPropuestasResumenDto
    {
        public decimal MontoEnviado { get; set; }
        public int CantidadEnviadas { get; set; }
        public decimal MontoGanado { get; set; }
        public int CantidadGanadas { get; set; }
        public decimal MontoPerdido { get; set; }
        public int CantidadPerdidas { get; set; }
        /// <summary>Ganadas sobre el total ya cerrado, en porcentaje.</summary>
        public int TasaCierrePct { get; set; }
        public List<CrmPropuestaDto> Propuestas { get; set; } = new();
    }
}
