using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Regla del motor de automatizaciones de Vordia HQ: un disparador que el motor
    /// evalúa periódicamente y una acción que ejecuta cuando encuentra coincidencias.
    /// </summary>
    [Table("CrmAutomatizaciones")]
    public class CrmAutomatizacion
    {
        [Key]
        public int AutomatizacionId { get; set; }
        public int EmpresaId { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";

        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// documento_por_vencer | contrato_por_cobrar | contrato_vencido |
        /// tarea_por_vencer | propuesta_sin_respuesta | lead_web_sin_contactar
        /// </summary>
        [Required, StringLength(50)]
        public string Disparador { get; set; } = "documento_por_vencer";

        /// <summary>Días de anticipación del aviso, o de espera para los disparadores de inactividad.</summary>
        public int DiasAnticipacion { get; set; } = 5;

        /// <summary>email | tarea | email_y_tarea</summary>
        [Required, StringLength(30)]
        public string Accion { get; set; } = "email";

        /// <summary>Destinatario del aviso. Si queda vacío se usa la cuenta remitente configurada.</summary>
        [StringLength(250)]
        public string? EmailDestino { get; set; }

        /// <summary>Admite {referencia}, {fecha}, {dias} y {monto}.</summary>
        [StringLength(200)]
        public string? AsuntoPlantilla { get; set; }

        public string? MensajePlantilla { get; set; }

        /// <summary>Encendida o pausada por el usuario.</summary>
        public bool Activa { get; set; }

        /// <summary>Baja lógica: false es una regla eliminada.</summary>
        public bool Activo { get; set; } = true;

        public DateTime? UltimaEjecucion { get; set; }

        [StringLength(400)]
        public string? UltimoResultado { get; set; }

        public int VecesDisparada { get; set; }

        [StringLength(150)]
        public string? CreadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
    }

    /// <summary>
    /// Bitácora de disparos. La combinación AutomatizacionId + ClaveUnica tiene índice
    /// único para los resultados exitosos, y es lo que impide avisar dos veces por el
    /// mismo hecho aunque el motor corra varias veces al día.
    /// </summary>
    [Table("CrmAutomatizacionEjecuciones")]
    public class CrmAutomatizacionEjecucion
    {
        [Key]
        public int EjecucionId { get; set; }
        public int AutomatizacionId { get; set; }
        public int EmpresaId { get; set; }

        /// <summary>Identifica el hecho concreto, por ejemplo "documento:41:2026-10-15".</summary>
        [Required, StringLength(200)]
        public string ClaveUnica { get; set; } = "";

        /// <summary>email | tarea | email_y_tarea | error | omitido</summary>
        [Required, StringLength(20)]
        public string Resultado { get; set; } = "email";

        [StringLength(200)]
        public string? Referencia { get; set; }

        [StringLength(1000)]
        public string? Detalle { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AutomatizacionId))]
        public virtual CrmAutomatizacion? Automatizacion { get; set; }
    }
}
