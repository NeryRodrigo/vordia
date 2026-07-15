using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class PropiedadGescoDto
    {
        // Datos identificadores
        public string? Titulo { get; set; }
        public string? ReferenciaMls { get; set; } // El ID de Remax (ej: 143084038-25)

        // Precios (los recibimos como string para limpiarlos después)
        public string? PrecioTexto { get; set; }
        public decimal PrecioFinal { get; set; }
        public decimal Precio { get; set; }
        public string MLSID { get; set; }

        public int AgentId { get; set; }
        public string NombreAgente { get; set; }
        public string? LinkAgente { get; set; } // De acá sacaremos el ID único del agente

        // Ubicación y Mapa
        public string? Ciudad { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public double OfficeLat { get; set; }
        public double OfficeLng { get; set; }
        public string? MapUrl { get; set; }

        // Datos Técnicos
        public string? M2TerrenoTexto { get; set; }
        public decimal M2TerrenoFinal { get; set; }

        public string? M2ConstruccionTexto { get; set; }
        public decimal M2ConstruccionFinal { get; set; }

        public int Dormitorios { get; set; }
        public int Baños { get; set; }

        // Descripción completa
        public string? Descripcion { get; set; }
        public string? UrlOrigen { get; set; }
    }
}
