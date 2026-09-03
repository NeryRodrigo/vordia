using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gesco.Domain.Entities
{
    [Table("AgroEstablecimientos")]
    public class AgroEstablecimiento
    {
        [Key] public int EstablecimientoId { get; set; }
        public int EmpresaId { get; set; }
        [Required, StringLength(150)] public string Nombre { get; set; } = "";
        [StringLength(150)] public string? ClienteNombre { get; set; }
        [StringLength(200)] public string? Ubicacion { get; set; }
        public decimal? Hectareas { get; set; }
        [StringLength(500)] public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("AgroLotes")]
    public class AgroLote
    {
        [Key] public int LoteId { get; set; }
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        [Required, StringLength(40)] public string Codigo { get; set; } = "";
        [Required, StringLength(120)] public string Nombre { get; set; } = "";
        [Required, StringLength(40)] public string Tipo { get; set; } = "Corral";
        public int Capacidad { get; set; }
        [StringLength(40)] public string? CamaraCodigo { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("AgroZonas")]
    public class AgroZona
    {
        [Key] public int ZonaId { get; set; }
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        [Required, StringLength(120)] public string Nombre { get; set; } = "";
        [Required, StringLength(30)] public string Tipo { get; set; } = "Permitida";
        public decimal? LatCentro { get; set; }
        public decimal? LngCentro { get; set; }
        public int RadioMetros { get; set; } = 200;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("AgroAnimales")]
    public class AgroAnimal
    {
        [Key] public int AnimalId { get; set; }
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        public int? LoteId { get; set; }
        [Required, StringLength(40)] public string CodigoInterno { get; set; } = "";
        [StringLength(60)] public string? RfidTag { get; set; }
        [StringLength(60)] public string? CollarId { get; set; }
        [Required, StringLength(40)] public string Categoria { get; set; } = "Novillo";
        [StringLength(10)] public string? Sexo { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public decimal? PesoIngresoKg { get; set; }
        public decimal? PesoActualKg { get; set; }
        [Required, StringLength(30)] public string Estado { get; set; } = "Activo";
        public decimal? UltimaLat { get; set; }
        public decimal? UltimaLng { get; set; }
        public DateTime? UltimaUbicacionAt { get; set; }
        public int? BateriaCollarPct { get; set; }
        [StringLength(500)] public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }

    [Table("AgroPesajes")]
    public class AgroPesaje
    {
        [Key] public int PesajeId { get; set; }
        public int EmpresaId { get; set; }
        public int AnimalId { get; set; }
        public decimal PesoKg { get; set; }
        public DateTime FechaPesaje { get; set; } = DateTime.UtcNow;
        [Required, StringLength(40)] public string Origen { get; set; } = "Manual";
        [StringLength(120)] public string? Operario { get; set; }
        [StringLength(300)] public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("AgroRaciones")]
    public class AgroRacion
    {
        [Key] public int RacionId { get; set; }
        public int EmpresaId { get; set; }
        public int LoteId { get; set; }
        [Required, StringLength(80)] public string TipoAlimento { get; set; } = "";
        public decimal CantidadKg { get; set; }
        public DateTime FechaEntrega { get; set; } = DateTime.UtcNow;
        [StringLength(120)] public string? Operario { get; set; }
        [StringLength(80)] public string? Vehiculo { get; set; }
        public decimal? CostoGs { get; set; }
        [StringLength(300)] public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("AgroDispositivos")]
    public class AgroDispositivo
    {
        [Key] public int DispositivoId { get; set; }
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        [Required, StringLength(60)] public string Codigo { get; set; } = "";
        [Required, StringLength(40)] public string Tipo { get; set; } = "Collar";
        public int? AnimalId { get; set; }
        [Required, StringLength(30)] public string Estado { get; set; } = "Online";
        public int? BateriaPct { get; set; }
        public DateTime? UltimoPingAt { get; set; }
        [StringLength(300)] public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("AgroAlertas")]
    public class AgroAlerta
    {
        [Key] public int AlertaId { get; set; }
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        public int? AnimalId { get; set; }
        public int? DispositivoId { get; set; }
        [Required, StringLength(40)] public string Tipo { get; set; } = "";
        [Required, StringLength(20)] public string Severidad { get; set; } = "Media";
        [Required, StringLength(200)] public string Titulo { get; set; } = "";
        [StringLength(500)] public string? Detalle { get; set; }
        public bool Resuelta { get; set; }
        public DateTime FechaAlerta { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
    }
}
