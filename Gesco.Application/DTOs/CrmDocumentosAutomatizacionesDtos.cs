using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gesco.Application.DTOs
{
    // ─────────────────────────────────────────────────────────────
    // Documentos
    // ─────────────────────────────────────────────────────────────

    public class CrmDocumentoDto
    {
        public int DocumentoId { get; set; }
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Otro";
        public string? NombreArchivo { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool EsEnlaceExterno { get; set; }
        public string? Extension { get; set; }
        public long TamanoBytes { get; set; }
        /// <summary>Tamaño ya formateado para mostrar, por ejemplo "1,4 MB".</summary>
        public string TamanoTexto { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        /// <summary>Días que faltan para vencer. Negativo si ya venció, null si no caduca.</summary>
        public int? DiasParaVencer { get; set; }
        /// <summary>sin_vencimiento | vigente | por_vencer | vencido</summary>
        public string EstadoVencimiento { get; set; } = "sin_vencimiento";
        public string? Notas { get; set; }
        public string? SubidoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>Alta de un documento que vive en otra plataforma (Drive, Dropbox, un sitio).</summary>
    public class CrmDocumentoEnlaceRequest
    {
        [Required]
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }

        [Required, StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(40)]
        public string? Categoria { get; set; }

        [Required, StringLength(500)]
        public string Url { get; set; } = string.Empty;

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        [StringLength(150)]
        public string? SubidoPor { get; set; }
    }

    /// <summary>
    /// Alta de un documento con archivo. El controlador ya guardó el binario en disco
    /// y acá viaja solo lo que se persiste en la tabla.
    /// </summary>
    public class CrmDocumentoArchivoRequest
    {
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Extension { get; set; }
        public long TamanoBytes { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? Notas { get; set; }
        public string? SubidoPor { get; set; }
    }

    /// <summary>Edición de la ficha: no cambia el archivo, solo sus datos.</summary>
    public class CrmDocumentoMetadatosRequest
    {
        [Required]
        public int EmpresaId { get; set; }
        public int? ClienteId { get; set; }

        [Required, StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(40)]
        public string? Categoria { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }
    }

    public class CrmDocumentosResumenDto
    {
        public int Total { get; set; }
        public int PorVencer { get; set; }
        public int Vencidos { get; set; }
        public int SinVencimiento { get; set; }
        public double TamanoTotalMb { get; set; }
        public List<CrmDocumentoCategoriaDto> PorCategoria { get; set; } = new();
    }

    public class CrmDocumentoCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // Automatizaciones
    // ─────────────────────────────────────────────────────────────

    public class CrmAutomatizacionDto
    {
        public int AutomatizacionId { get; set; }
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Disparador { get; set; } = string.Empty;
        /// <summary>Nombre legible del disparador, para no repetir el diccionario en el front.</summary>
        public string DisparadorNombre { get; set; } = string.Empty;
        public int DiasAnticipacion { get; set; }
        public string Accion { get; set; } = "email";
        public string AccionNombre { get; set; } = string.Empty;
        public string? EmailDestino { get; set; }
        public string? AsuntoPlantilla { get; set; }
        public string? MensajePlantilla { get; set; }
        public bool Activa { get; set; }
        public DateTime? UltimaEjecucion { get; set; }
        public string? UltimoResultado { get; set; }
        public int VecesDisparada { get; set; }
        public string? CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class CrmAutomatizacionUpsertRequest
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required, StringLength(50)]
        public string Disparador { get; set; } = string.Empty;

        [Range(0, 365)]
        public int DiasAnticipacion { get; set; } = 5;

        [StringLength(30)]
        public string? Accion { get; set; }

        [StringLength(250)]
        public string? EmailDestino { get; set; }

        [StringLength(200)]
        public string? AsuntoPlantilla { get; set; }

        public string? MensajePlantilla { get; set; }

        public bool Activa { get; set; }

        [StringLength(150)]
        public string? CreadoPor { get; set; }
    }

    public class CrmAutomatizacionEstadoRequest
    {
        [Required]
        public int EmpresaId { get; set; }
        public bool Activa { get; set; }
    }

    public class CrmAutomatizacionEjecucionDto
    {
        public int EjecucionId { get; set; }
        public int AutomatizacionId { get; set; }
        public string AutomatizacionNombre { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public string? Detalle { get; set; }
        public DateTime Fecha { get; set; }
    }

    /// <summary>Resultado de una corrida del motor, sea manual o automática.</summary>
    public class CrmAutomatizacionCorridaDto
    {
        public int ReglasEvaluadas { get; set; }
        public int Coincidencias { get; set; }
        public int Disparos { get; set; }
        public int YaAvisados { get; set; }
        public int Errores { get; set; }
        public List<string> Detalle { get; set; } = new();
        public string Resumen { get; set; } = string.Empty;
    }

    public class CrmAutomatizacionCatalogoDto
    {
        public List<CrmAutomatizacionOpcionDto> Disparadores { get; set; } = new();
        public List<CrmAutomatizacionOpcionDto> Acciones { get; set; } = new();
        public List<string> CategoriasDocumento { get; set; } = new();
    }

    public class CrmAutomatizacionOpcionDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        /// <summary>Texto que explica qué significan los días en este disparador.</summary>
        public string? EtiquetaDias { get; set; }
    }
}
