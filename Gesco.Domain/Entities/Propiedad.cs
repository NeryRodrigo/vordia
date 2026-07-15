using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Propiedad
    {
        public int PropiedadId { get; set; }
        public int EmpresaId { get; set; }
        public int AgenteId { get; set; } // FK a Persona (Agente)
        public string? Titulo { get; set; } 
        public string? MLSID { get; set; }
        public string? Ciudad { get; set; }
        public decimal PrecioVentaGs { get; set; }
        public decimal SuperficieLoteM2 { get; set; }
        public decimal AreaConstruccionM2 { get; set; }

        // Campos Técnicos
        public int NroAmbientes { get; set; }
        public int NroDormitorios { get; set; }
        public int NroBaños { get; set; }
        public bool TieneJardin { get; set; }
        public bool AptoOficina { get; set; }
        public string? DescripcionRemax { get; set; }

        public bool Sync { get; set; }

        // Geolocalización
        public double Latitud { get; set; }
        public double Longitud { get; set; }

        // Fechas Críticas
        public DateTime? FechaFirmaContrato { get; set; }
        public DateTime? FechaCaducidad { get; set; }

        // Propiedades de navegación
        public Persona? Agente { get; set; }

        public bool EsAprobada { get; set; }
        public string? AgenteCreadorNombre { get; set; }
        public DateTime? FechaCreacion { get; set; }

        // Nuevos campos para UI y Requisitos
        public string? ImagenUrl { get; set; } // Foto de la propiedad
        public string? FotoAgenteUrl { get; set; } // Foto del agente
        
        public ICollection<DocumentoPropiedad> Documentos { get; set; } = new List<DocumentoPropiedad>();
    }
}
