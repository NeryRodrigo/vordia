using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    /* VORDIA HQ — Necesidades del Mercado */

    public class MercadoNecesidadDto
    {
        public int NecesidadId { get; set; }
        public string Fuente { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        public string? Url { get; set; }
        public string? Solicitante { get; set; }
        public string? Pais { get; set; }
        public decimal? PresupuestoMin { get; set; }
        public decimal? PresupuestoMax { get; set; }
        public string? Moneda { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string? Habilidades { get; set; }
        public string? Vertical { get; set; }
        public string? Categoria { get; set; }
        public string? ProductoSugerido { get; set; }
        public int Puntaje { get; set; }
        public string? MotivoPuntaje { get; set; }
        public int Confianza { get; set; }
        public string? ConfianzaMotivo { get; set; }
        public int? Competidores { get; set; }
        public string? Alcance { get; set; }
        public string Estado { get; set; } = "";
        public string? ResponsableNombre { get; set; }
        public string? ContactoNombre { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoTelefono { get; set; }
        public string? ContactoOrigen { get; set; }
        /// <summary>Precio sugerido de la microapp para esta categoría (USD).</summary>
        public decimal PrecioSetupSugeridoUsd { get; set; }
        public decimal PrecioMensualSugeridoUsd { get; set; }
        public string? KitSugerido { get; set; }
        public string? Notas { get; set; }
        public int? ClienteId { get; set; }
        public int? RoadmapId { get; set; }
        public DateTime FechaCaptura { get; set; }
    }

    public class MercadoFuenteDto
    {
        public int FuenteId { get; set; }
        public string Tipo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Consulta { get; set; }
        public bool Activa { get; set; }
        public DateTime? UltimaSync { get; set; }
        public string? UltimoResultado { get; set; }
        public int Necesidades { get; set; }
    }

    public class MercadoFuenteRequest
    {
        public int EmpresaId { get; set; }
        public string Tipo { get; set; } = "Freelancer";
        public string Nombre { get; set; } = "";
        public string? Consulta { get; set; }
        public bool Activa { get; set; } = true;
    }

    public class MercadoManualRequest
    {
        public int EmpresaId { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }
        /// <summary>Facebook, WhatsApp, LinkedIn, referido, llamada…</summary>
        public string? Canal { get; set; }
        public string? Url { get; set; }
        public string? Solicitante { get; set; }
        public string? Pais { get; set; }
        public decimal? PresupuestoMax { get; set; }
        public string? Moneda { get; set; }
        public string? ResponsableNombre { get; set; }
        /// <summary>Señales de confianza marcadas por el vendedor (ver MercadoService.SenalesManual).</summary>
        public List<string>? Senales { get; set; }
    }

    public class MercadoActualizarRequest
    {
        public int EmpresaId { get; set; }
        public string? Estado { get; set; }
        public string? Notas { get; set; }
        public string? ResponsableNombre { get; set; }
    }

    public class MercadoSyncFuenteDto
    {
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public bool Ok { get; set; }
        public int Revisadas { get; set; }
        public int Nuevas { get; set; }
        public string? Mensaje { get; set; }
    }

    public class MercadoSyncResultDto
    {
        public int Nuevas { get; set; }
        public int Actualizadas { get; set; }
        public int Revisadas { get; set; }
        public List<MercadoSyncFuenteDto> Fuentes { get; set; } = new();
    }

    public class MercadoConteoDto
    {
        public string Clave { get; set; } = "";
        public int Cantidad { get; set; }
        public int PuntajePromedio { get; set; }
    }

    /// <summary>Patrón repetido de pedidos → producto rápido que conviene empaquetar.</summary>
    public class MercadoOportunidadDto
    {
        public string Categoria { get; set; } = "";
        public string Vertical { get; set; } = "";
        public int Cantidad { get; set; }
        public int CalientesAltas { get; set; }
        public decimal? PresupuestoMedianoUsd { get; set; }
        public string ProductoVordia { get; set; } = "";
        public string ProductoRapido { get; set; } = "";
        public List<string> Ejemplos { get; set; } = new();
        public int? RoadmapId { get; set; }
        /// <summary>Precio sugerido de la microapp: implementación única + mensualidad.</summary>
        public decimal PrecioSetupUsd { get; set; }
        public decimal PrecioMensualUsd { get; set; }
        /// <summary>Ingreso recurrente mensual con 10 clientes.</summary>
        public decimal Mrr10ClientesUsd { get; set; }
    }

    public class MercadoResumenDto
    {
        public int Total { get; set; }
        public int Nuevas7d { get; set; }
        public int Calientes { get; set; }
        public int EnGestion { get; set; }
        public int Ganadas { get; set; }
        public int ConvertidasCrm { get; set; }
        public int Confiables { get; set; }
        public DateTime? UltimaSync { get; set; }
        public List<MercadoConteoDto> PorFuente { get; set; } = new();
        public List<MercadoConteoDto> PorVertical { get; set; } = new();
        public List<MercadoConteoDto> PorAlcance { get; set; } = new();
        public List<MercadoOportunidadDto> Oportunidades { get; set; } = new();
    }

    public class MercadoOportunidadProductoRequest
    {
        public int EmpresaId { get; set; }
        public string Categoria { get; set; } = "";
        public string? UsuarioNombre { get; set; }
    }

    public class MercadoConversionRequest
    {
        public int EmpresaId { get; set; }
        public string? UsuarioNombre { get; set; }
    }

    public class MercadoConversionResultDto
    {
        public int? ClienteId { get; set; }
        public int? RoadmapId { get; set; }
        public string Mensaje { get; set; } = "";
    }
}
