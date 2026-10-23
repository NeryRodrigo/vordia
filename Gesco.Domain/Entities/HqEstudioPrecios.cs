using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Respuesta a la encuesta de precios de VORDIA (método Van Westendorp + Gabor-Granger):
    /// cuatro umbrales de precio del prospecto y su intención de compra ante un precio mostrado al azar.
    /// </summary>
    [Table("HqEstudioPreciosRespuestas")]
    public class HqEstudioPrecioRespuesta
    {
        [Key] public int RespuestaId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(20)] public string Producto { get; set; } = "";
        [StringLength(60)] public string? Origen { get; set; }

        /// <summary>Tamaño en la métrica del producto (unidades, habitaciones, cajas, vehículos, cabezas).</summary>
        public int? Tamano { get; set; }
        [StringLength(80)] public string? Ciudad { get; set; }
        [StringLength(40)] public string? HerramientaActual { get; set; }
        public decimal? GastoActualGs { get; set; }
        [StringLength(80)] public string? ProblemaPrincipal { get; set; }
        [StringLength(20)] public string? Decisor { get; set; }

        // Van Westendorp (Gs por mes)
        public decimal MuyBaratoGs { get; set; }
        public decimal BaratoGs { get; set; }
        public decimal CaroGs { get; set; }
        public decimal MuyCaroGs { get; set; }

        // Gabor-Granger: precio del plan recomendado mostrado al azar y probabilidad de compra (1 a 5)
        public decimal PrecioMostradoGs { get; set; }
        public int IntencionCompra { get; set; }

        [StringLength(1000)] public string? Comentario { get; set; }
        [StringLength(150)] public string? ContactoNombre { get; set; }
        [StringLength(150)] public string? ContactoEmpresa { get; set; }
        [StringLength(150)] public string? ContactoDato { get; set; }
        public bool AceptaContacto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
