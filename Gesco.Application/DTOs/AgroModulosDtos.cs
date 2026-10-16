using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    /* VORDIA AGRO — rediseño por módulos: Base + Cría + Recría + Feed + Health + Trace + Finance + Connect */

    public class AgroConfiguracionDto
    {
        public bool Configurada { get; set; }
        public string SistemaProductivo { get; set; } = "CicloCompleto";
        public string Paquete { get; set; } = "Professional";
        public List<string> ModulosActivos { get; set; } = new();
        public decimal PesoObjetivoKg { get; set; }
        public decimal PesoObjetivoRecriaKg { get; set; }
        public decimal GpdMinimoKg { get; set; }
        public decimal PrecioKgGs { get; set; }
        public decimal CostoRacionKgGs { get; set; }
        public decimal GpdObjetivoKg { get; set; }
        public decimal ConversionObjetivo { get; set; }
    }

    public class AgroConfiguracionRequest
    {
        public int EmpresaId { get; set; }
        public string SistemaProductivo { get; set; } = "CicloCompleto";
        public string? Paquete { get; set; }
        public List<string> ModulosActivos { get; set; } = new();
        public decimal? PesoObjetivoKg { get; set; }
        public decimal? PesoObjetivoRecriaKg { get; set; }
        public decimal? GpdMinimoKg { get; set; }
        public decimal? PrecioKgGs { get; set; }
        public decimal? CostoRacionKgGs { get; set; }
        public decimal? GpdObjetivoKg { get; set; }
        public decimal? ConversionObjetivo { get; set; }
    }

    /// <summary>Ítem de "Qué requiere atención hoy".</summary>
    public class AgroAtencionDto
    {
        public string Prioridad { get; set; } = "Media"; // Alta | Media | Baja
        public string Modulo { get; set; } = "";         // FEED | HEALTH | CRIA | RECRIA | CONNECT | TRACE
        public string Titulo { get; set; } = "";
        public string? Detalle { get; set; }
        public int? AlertaId { get; set; }
        public int? SanidadId { get; set; }
        public int? LoteId { get; set; }
        public int? AnimalId { get; set; }
    }

    public class AgroEtapaResumenDto
    {
        public string Etapa { get; set; } = "";
        public int Animales { get; set; }
        public decimal? PesoPromedioKg { get; set; }
        public decimal? GpdPromedioKg { get; set; }
        public decimal ValorEstimadoGs { get; set; }
    }

    /// <summary>Dashboard ejecutivo: panorama de toda la empresa ganadera.</summary>
    public class AgroEjecutivoDto
    {
        public int EstablecimientoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? ClienteNombre { get; set; }
        public string? Ubicacion { get; set; }
        public decimal? Hectareas { get; set; }
        public int TotalAnimales { get; set; }
        public int ListosParaVenta { get; set; }
        public int AlertasAbiertas { get; set; }
        public int SanidadPendiente { get; set; }
        public int SanidadVencida { get; set; }
        public int AnimalesEnRetiro { get; set; }
        public decimal KgEnPie { get; set; }
        public decimal ValorRodeoGs { get; set; }
        public decimal PrecioKgGs { get; set; }
        public int Lotes { get; set; }
        public List<AgroEtapaResumenDto> Etapas { get; set; } = new();
        public List<AgroAtencionDto> Atencion { get; set; } = new();
    }

    public class AgroPuntoPesoDto
    {
        public DateTime Fecha { get; set; }
        public decimal? PesoPromedioKg { get; set; }
        /// <summary>Trayectoria objetivo: arranca en el primer punto real y sube con la GPD objetivo.</summary>
        public decimal? PesoObjetivoKg { get; set; }
        public int Animales { get; set; }
    }

    public class AgroPuntoAlimentoDto
    {
        public DateTime Fecha { get; set; }
        public decimal Kg { get; set; }
        public decimal CostoGs { get; set; }
    }

    public class AgroFeedLoteDto
    {
        public int LoteId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int Capacidad { get; set; }
        public int Animales { get; set; }
        public decimal? PesoPromedioKg { get; set; }
        public decimal? GpdPromedioKg { get; set; }
        public decimal PesoObjetivoKg { get; set; }
        public int? DiasPromedio { get; set; }
        public decimal AlimentoKg7d { get; set; }
        public decimal? KgCabDia { get; set; }
        public decimal? Conversion { get; set; }
        public decimal KgProducidos30d { get; set; }
        public decimal CostoAlimento30dGs { get; set; }
        public decimal? CostoKgProducidoGs { get; set; }
        public int ListosParaVenta { get; set; }
        public DateTime? UltimaRacion { get; set; }
        public string Rendimiento { get; set; } = "Optimo"; // Optimo | Atencion | Bajo
    }

    public class AgroSanidadResumenDto
    {
        public int Programadas { get; set; }
        public int Vencidas { get; set; }
        public int AplicadasMes { get; set; }
        public int AnimalesEnRetiro { get; set; }
        public int AnimalesEnTratamiento { get; set; }
        public List<AgroSanidadDto> Proximas { get; set; } = new();
    }

    public class AgroEconomiaDto
    {
        public decimal KgProducidos30d { get; set; }
        public decimal CostoAlimento30dGs { get; set; }
        public decimal? CostoKgProducidoGs { get; set; }
        public decimal CostoDiarioGs { get; set; }
        public decimal? CostoCabDiaGs { get; set; }
        public decimal PrecioKgGs { get; set; }
        public decimal? MargenKgGs { get; set; }
        public decimal ValorStockGs { get; set; }
        public decimal ValorListosGs { get; set; }
        public decimal KgListos { get; set; }
    }

    public class AgroDispositivosResumenDto
    {
        public int Total { get; set; }
        public int Online { get; set; }
        public int Alerta { get; set; }
        public int BateriaBaja { get; set; }
    }

    public class AgroActividadDto
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = ""; // Pesaje | Racion | Sanidad | Alerta | Etapa
        public string Titulo { get; set; } = "";
        public string? Detalle { get; set; }
    }

    /// <summary>Dashboard VORDIA Feed — Engorde / Confinamiento.</summary>
    public class AgroFeedDto
    {
        public int EstablecimientoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? ClienteNombre { get; set; }
        public string? Ubicacion { get; set; }
        public int CabezasActivas { get; set; }
        public int CapacidadTotal { get; set; }
        public decimal? PesoPromedioKg { get; set; }
        public decimal? PesoIngresoPromedioKg { get; set; }
        public decimal? GpdPromedioKg { get; set; }
        public decimal? ConversionEstimada { get; set; }
        public int ListosParaVenta { get; set; }
        public int AlertasAbiertas { get; set; }
        public int? DiasPromedioEnCorral { get; set; }
        public decimal PesoObjetivoKg { get; set; }
        public decimal GpdMinimoKg { get; set; }
        public decimal GpdObjetivoKg { get; set; }
        public decimal ConversionObjetivo { get; set; }
        /// <summary>Animales que llegan al peso objetivo en ≤ 15 / ≤ 30 días (sin contar los ya listos).</summary>
        public int Listos15d { get; set; }
        public int Listos30d { get; set; }
        public int? DiasEstimadosTerminacion { get; set; }
        public decimal AlimentoKg7d { get; set; }
        public decimal? KgCabDia { get; set; }
        public List<AgroAtencionDto> Atencion { get; set; } = new();
        public List<AgroPuntoPesoDto> EvolucionPeso { get; set; } = new();
        public List<AgroFeedLoteDto> Lotes { get; set; } = new();
        public List<AgroPuntoAlimentoDto> AlimentacionDiaria { get; set; } = new();
        public AgroSanidadResumenDto Sanidad { get; set; } = new();
        public AgroEconomiaDto Economia { get; set; } = new();
        public AgroDispositivosResumenDto Dispositivos { get; set; } = new();
        public List<AgroActividadDto> Actividad { get; set; } = new();
    }

    public class AgroSanidadDto
    {
        public int SanidadId { get; set; }
        public int? AnimalId { get; set; }
        public string? AnimalCodigo { get; set; }
        public int? LoteId { get; set; }
        public string? LoteCodigo { get; set; }
        public string Tipo { get; set; } = "";
        public string Producto { get; set; } = "";
        public string? Dosis { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public DateTime? FechaAplicacion { get; set; }
        public int DiasRetiro { get; set; }
        public DateTime? RetiroHasta { get; set; }
        public string Estado { get; set; } = "";
        public bool Vencida { get; set; }
        public string? Responsable { get; set; }
        public string? Notas { get; set; }
        public int AnimalesAlcanzados { get; set; }
    }

    public class AgroSanidadCreateRequest
    {
        public int EmpresaId { get; set; }
        public int? AnimalId { get; set; }
        public int? LoteId { get; set; }
        public string Tipo { get; set; } = "Vacuna";
        public string Producto { get; set; } = "";
        public string? Dosis { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public bool Aplicada { get; set; }
        public int DiasRetiro { get; set; }
        public string? Responsable { get; set; }
        public string? Notas { get; set; }
    }

    public class AgroCambioEtapaRequest
    {
        public int EmpresaId { get; set; }
        public string Etapa { get; set; } = "";
        public int? LoteId { get; set; }
        public decimal? PesoKg { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>Ficha / trazabilidad individual del animal.</summary>
    public class AgroFichaAnimalDto
    {
        public AgroAnimalDto Animal { get; set; } = new();
        public string? LoteNombre { get; set; }
        public List<AgroPesajeDto> Pesajes { get; set; } = new();
        public List<AgroSanidadDto> Sanidad { get; set; } = new();
        public List<AgroAlertaDto> Alertas { get; set; } = new();
        public List<AgroActividadDto> Eventos { get; set; } = new();
        public List<string> Crias { get; set; } = new();
    }
}
