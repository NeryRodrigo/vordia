using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    /// <summary>
    /// Secuencia de correos comerciales de VORDIA HQ: primer correo redactado con IA y aprobado por una persona,
    /// más seguimientos automáticos espaciados, con límite diario y respeto de las bajas.
    /// </summary>
    [Table("HqSecuencias")]
    public class HqSecuencia
    {
        [Key] public int SecuenciaId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(120)] public string Nombre { get; set; } = "";
        /// <summary>Código de producto: HABITAT, STAY, DINE, RETAIL, BOUTIQUE, OPS, AGRO.</summary>
        [Required, StringLength(20)] public string Producto { get; set; } = "";
        /// <summary>Qué se ofrece (ej. precio fundador, demo de 15 minutos). La IA lo usa en todos los correos.</summary>
        [Required, StringLength(1000)] public string Oferta { get; set; } = "";
        [Required, StringLength(120)] public string Firma { get; set; } = "";
        [StringLength(150)] public string? ResponderA { get; set; }
        public int Pasos { get; set; } = 3;
        public int DiasEntrePasos { get; set; } = 3;
        public int LimiteDiario { get; set; } = 30;
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Cada persona dentro de una secuencia y en qué paso va.</summary>
    [Table("HqSecuenciaContactos")]
    public class HqSecuenciaContacto
    {
        [Key] public int ContactoId { get; set; }
        public int EmpresaId { get; set; }
        public int SecuenciaId { get; set; }
        public int? ClienteId { get; set; }
        public int? NecesidadId { get; set; }
        [Required, StringLength(150)] public string Email { get; set; } = "";
        [StringLength(150)] public string? Nombre { get; set; }
        [StringLength(200)] public string? Empresa { get; set; }
        /// <summary>Datos de negocio para personalizar (qué pidió, tamaño, resultado de una calculadora).</summary>
        [StringLength(2000)] public string? Contexto { get; set; }
        /// <summary>Borrador | Redactado | Aprobado | EnCurso | Respondio | Baja | Terminado | Descartado | Error</summary>
        [Required, StringLength(20)] public string Estado { get; set; } = "Borrador";
        public int PasoActual { get; set; }
        [StringLength(200)] public string? Asunto { get; set; }
        public string? Cuerpo { get; set; }
        public DateTime? ProximoEnvio { get; set; }
        public DateTime? UltimoEnvio { get; set; }
        [StringLength(300)] public string? Detalle { get; set; }
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Registro de cada correo enviado (o simulado) por una secuencia.</summary>
    [Table("HqSecuenciaEnvios")]
    public class HqSecuenciaEnvio
    {
        [Key] public int EnvioId { get; set; }
        public int EmpresaId { get; set; }
        public int SecuenciaId { get; set; }
        public int ContactoId { get; set; }
        public int Paso { get; set; }
        [StringLength(200)] public string Asunto { get; set; } = "";
        public string Cuerpo { get; set; } = "";
        /// <summary>Enviado | Simulado | Error</summary>
        [StringLength(20)] public string Resultado { get; set; } = "Enviado";
        [StringLength(300)] public string? Detalle { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
