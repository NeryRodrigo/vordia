using Microsoft.AspNetCore.Mvc;
using Gesco.Application.DTOs;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IMigrationService
    {
        Task<int> MigrarInteligente(Stream archivoExcel, string nombreArchivo, int empresaId);
        Task<int> MigrarDatosDesdeExcel(Stream archivoExcel, string nombreTabla, int empresaId);
        Task<List<Persona>> ObtenerPersonas(int empresaId);
        Task<object> ObtenerAgentes(int empresaId);
        Task<int> MigrarFacturasDesdeExcel(Stream archivoExcel, int empresaId);
        Task<VentaCabecera?> ObtenerFacturaCompleta(int ventaId);
        Task<CatalogosDto> ObtenerCatalogos(int empresaId);
        Task<int> GuardarFacturaManual(VentaCabecera factura);

        Task<List<VentaCabecera>> ObtenerVentas(int empresaId);

        Task<int> GuardarPropiedad(Propiedad propiedad);
        //Task<List<Propiedad>> ObtenerPropiedades(int empresaId);

        Task<List<object>> ObtenerPropiedades(int empresaId);

        Task<SyncEstadoDto> ScrappingMasivoRemax(int empresaId, string? urlManual);

        Task<List<AnalisisMercado>> ObtenerAnalisisMercado(int empresaId);

        Task<List<InmuebleReferencia>> ObtenerReferenciasPorAnalisis(int analisisId);

        Task<string> ObtenerSiguienteNroFactura(int empresaId);

        Task<int> MigrarInmobiliario(Stream archivoExcel, int empresaId);
        Task<int> MigrarCondominioExcel(Stream archivoExcel, int empresaId);
        Task<int> MigrarContratosPdf(List<Microsoft.AspNetCore.Http.IFormFile> pdfs, int empresaId);
        Task PausarSync(int empresaId);
        Task<SyncEstadoDto> ObtenerSyncStatus(int empresaId);
        Task<int> CrearAnalisisManual(AnalisisMercado input);

        Task<bool> AprobarPropiedad(int propiedadId);
        Task<Propiedad?> CrearCaptacionManual(Propiedad propInput);
        Task<bool> ActualizarAnalisis(int id, AnalisisMercado input);
        Task<bool> ActualizarPropiedad(Propiedad prop);
        Task<bool> EliminarPropiedad(int id);
        Task<bool> EliminarAnalisis(int id);
    }
}
