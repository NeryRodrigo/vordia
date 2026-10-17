using System;
using System.Collections.Generic;

namespace Gesco.Application.DTOs
{
    /* VORDIA AGRO — dashboard principal, inventario de insumos y lotes */

    public class AgroInsumoDto
    {
        public int InsumoId { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Unidad { get; set; } = "";
        public decimal Stock { get; set; }
        /// <summary>Consumo diario promedio de los últimos 7 días.</summary>
        public decimal ConsumoDiario { get; set; }
        public decimal? DiasRestantes { get; set; }
        public decimal StockMinimo { get; set; }
        public bool BajoMinimo { get; set; }
        public decimal? CostoPromedioGs { get; set; }
        public DateTime? UltimoIngreso { get; set; }
    }

    public class AgroInsumoRequest
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "Alimento";
        public string Unidad { get; set; } = "kg";
        public decimal StockMinimo { get; set; }
        public decimal? StockInicial { get; set; }
        public decimal? CostoUnitarioGs { get; set; }
    }

    public class AgroMovimientoRequest
    {
        public int EmpresaId { get; set; }
        /// <summary>Ingreso | Consumo | Ajuste (en ajuste, cantidad con signo).</summary>
        public string Tipo { get; set; } = "Ingreso";
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitarioGs { get; set; }
        public string? Referencia { get; set; }
        public string? Operario { get; set; }
    }

    public class AgroMovimientoDto
    {
        public int MovimientoId { get; set; }
        public string Tipo { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitarioGs { get; set; }
        public DateTime Fecha { get; set; }
        public string? Referencia { get; set; }
        public string? Operario { get; set; }
    }

    public class AgroLoteRequest
    {
        public int EmpresaId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "Corral";
        public int Capacidad { get; set; }
        public string? Etapa { get; set; }
        public decimal? PesoObjetivoKg { get; set; }
    }

    public class AgroEstadoSanitarioDto
    {
        public int Total { get; set; }
        public int SinIncidencias { get; set; }
        public int EnObservacion { get; set; }
        public int EnTratamiento { get; set; }
        public int EnRetiro { get; set; }
        /// <summary>Animales alcanzados por vacunas programadas en los próximos 7 días.</summary>
        public int VacunasProximas { get; set; }
        /// <summary>Animales alcanzados por vacunas vencidas.</summary>
        public int VacunasVencidas { get; set; }
        public int Muertes30d { get; set; }
        public decimal MortalidadPct30d { get; set; }
    }

    public class AgroTrazaResumenDto
    {
        public decimal PctIdentificacionCompleta { get; set; }
        public int InfoIncompleta { get; set; }
        public int SinRfid { get; set; }
        public int SinLote { get; set; }
        public int SinRazaOFecha { get; set; }
        public DateTime? UltimoPesaje { get; set; }
        public int Pesajes7d { get; set; }
    }

    public class AgroAlimentoResumenDto
    {
        public decimal ConsumoHoyKg { get; set; }
        public decimal ConsumoAyerKg { get; set; }
        public decimal? VariacionPct { get; set; }
        public decimal? ConsumoCabezaKg { get; set; }
        /// <summary>Referencia de consumo: ≈ 2,8 % del peso vivo promedio (ración tal cual).</summary>
        public decimal? ConsumoObjetivoCabKg { get; set; }
        public decimal StockKg { get; set; }
        public decimal? StockDias { get; set; }
        public decimal? Conversion { get; set; }
        public decimal ConversionObjetivo { get; set; }
        public List<AgroPuntoAlimentoDto> ConsumoDiario { get; set; } = new();
    }

    public class AgroRentabilidadDto
    {
        /// <summary>Alimento acumulado por cabeza desde el ingreso (estimado).</summary>
        public decimal CostoCabezaGs { get; set; }
        public decimal? CostoKgGanadoGs { get; set; }
        public decimal ValorCabezaGs { get; set; }
        /// <summary>Valor del animal al ingresar (peso de ingreso × precio kg).</summary>
        public decimal CostoIngresoCabezaGs { get; set; }
        public decimal MargenCabezaGs { get; set; }
        public decimal? MargenPct { get; set; }
    }

    public class AgroDispositivoTipoDto
    {
        public string Tipo { get; set; } = "";
        public int Online { get; set; }
        public int Total { get; set; }
    }

    /// <summary>Todo lo que muestra el dashboard principal de VORDIA Agro en una sola llamada.</summary>
    public class AgroPrincipalDto
    {
        public AgroEjecutivoDto Ejecutivo { get; set; } = new();
        public AgroFeedDto? Feed { get; set; }
        public string SistemaProductivo { get; set; } = "";
        public string Paquete { get; set; } = "";
        public DateTime? UltimaSincronizacion { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int IngresosMes { get; set; }
        public int SalidasMes { get; set; }
        public decimal? VariacionMesPct { get; set; }
        public int AlertasTotal { get; set; }
        public int AlertasSanitarias { get; set; }
        public int AlertasOperativas { get; set; }
        public int AlertasDispositivos { get; set; }
        public AgroEstadoSanitarioDto Sanitario { get; set; } = new();
        public AgroTrazaResumenDto Trazabilidad { get; set; } = new();
        public AgroAlimentoResumenDto Alimento { get; set; } = new();
        public AgroRentabilidadDto Rentabilidad { get; set; } = new();
        public int DispositivosOnline { get; set; }
        public int DispositivosTotal { get; set; }
        public List<AgroDispositivoTipoDto> DispositivosPorTipo { get; set; } = new();
        public List<AgroInsumoDto> Inventario { get; set; } = new();
        public List<AgroActividadDto> Actividad { get; set; } = new();
    }
}
