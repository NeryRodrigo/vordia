using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    public class AgroDashboardDto
    {
        public int EstablecimientoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? ClienteNombre { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? Hectareas { get; set; }
        public int AnimalesActivos { get; set; }
        public int AnimalesFueraZona { get; set; }
        public int AnimalesTratamiento { get; set; }
        public int Lotes { get; set; }
        public int AlertasAbiertas { get; set; }
        public int DispositivosOnline { get; set; }
        public int DispositivosAlerta { get; set; }
        public decimal AlimentoKg7d { get; set; }
        public decimal? GpdPromedioKg { get; set; }
        public decimal? ConversionEstimada { get; set; }
        public List<AgroAlertaDto> UltimasAlertas { get; set; } = new();
    }

    public class AgroAnimalDto
    {
        public int AnimalId { get; set; }
        public int EstablecimientoId { get; set; }
        public int? LoteId { get; set; }
        public string? LoteCodigo { get; set; }
        public string CodigoInterno { get; set; } = "";
        public string? RfidTag { get; set; }
        public string? CollarId { get; set; }
        public string Categoria { get; set; } = "";
        public string? Sexo { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public decimal? PesoIngresoKg { get; set; }
        public decimal? PesoActualKg { get; set; }
        public decimal? GananciaKg { get; set; }
        public decimal? GpdKg { get; set; }
        public int? DiasEnEstablecimiento { get; set; }
        public string Estado { get; set; } = "";
        public decimal? UltimaLat { get; set; }
        public decimal? UltimaLng { get; set; }
        public DateTime? UltimaUbicacionAt { get; set; }
        public int? BateriaCollarPct { get; set; }
        public string? Notas { get; set; }
    }

    public class AgroAnimalUpsertRequest
    {
        public int EmpresaId { get; set; }
        public int EstablecimientoId { get; set; }
        public int? LoteId { get; set; }
        public string CodigoInterno { get; set; } = "";
        public string? RfidTag { get; set; }
        public string? CollarId { get; set; }
        public string Categoria { get; set; } = "Novillo";
        public string? Sexo { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public decimal? PesoIngresoKg { get; set; }
        public decimal? PesoActualKg { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? Notas { get; set; }
    }

    public class AgroPesajeDto
    {
        public int PesajeId { get; set; }
        public int AnimalId { get; set; }
        public string? AnimalCodigo { get; set; }
        public decimal PesoKg { get; set; }
        public DateTime FechaPesaje { get; set; }
        public string Origen { get; set; } = "";
        public string? Operario { get; set; }
    }

    public class AgroPesajeCreateRequest
    {
        public int EmpresaId { get; set; }
        public int AnimalId { get; set; }
        public decimal PesoKg { get; set; }
        public DateTime? FechaPesaje { get; set; }
        public string Origen { get; set; } = "Manual";
        public string? Operario { get; set; }
        public string? Notas { get; set; }
    }

    public class AgroRacionDto
    {
        public int RacionId { get; set; }
        public int LoteId { get; set; }
        public string? LoteCodigo { get; set; }
        public string TipoAlimento { get; set; } = "";
        public decimal CantidadKg { get; set; }
        public DateTime FechaEntrega { get; set; }
        public string? Operario { get; set; }
        public string? Vehiculo { get; set; }
        public decimal? CostoGs { get; set; }
    }

    public class AgroRacionCreateRequest
    {
        public int EmpresaId { get; set; }
        public int LoteId { get; set; }
        public string TipoAlimento { get; set; } = "";
        public decimal CantidadKg { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? Operario { get; set; }
        public string? Vehiculo { get; set; }
        public decimal? CostoGs { get; set; }
        public string? Notas { get; set; }
    }

    public class AgroLoteDto
    {
        public int LoteId { get; set; }
        public int EstablecimientoId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int Capacidad { get; set; }
        public string? CamaraCodigo { get; set; }
        public int Animales { get; set; }
    }

    public class AgroDispositivoDto
    {
        public int DispositivoId { get; set; }
        public string Codigo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int? AnimalId { get; set; }
        public string? AnimalCodigo { get; set; }
        public string Estado { get; set; } = "";
        public int? BateriaPct { get; set; }
        public DateTime? UltimoPingAt { get; set; }
    }

    public class AgroAlertaDto
    {
        public int AlertaId { get; set; }
        public int? AnimalId { get; set; }
        public string? AnimalCodigo { get; set; }
        public string Tipo { get; set; } = "";
        public string Severidad { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string? Detalle { get; set; }
        public bool Resuelta { get; set; }
        public DateTime FechaAlerta { get; set; }
    }

    public class AgroZonaDto
    {
        public int ZonaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public decimal? LatCentro { get; set; }
        public decimal? LngCentro { get; set; }
        public int RadioMetros { get; set; }
    }

    public class AgroEstablecimientoDto
    {
        public int EstablecimientoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? ClienteNombre { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? Hectareas { get; set; }
    }

    /// <summary>Simula evento de hardware (collar/GPS) para el prototipo.</summary>
    public class AgroSimularUbicacionRequest
    {
        public int EmpresaId { get; set; }
        public int AnimalId { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public int? BateriaPct { get; set; }
        public bool GenerarAlertaFueraZona { get; set; }
    }
}
