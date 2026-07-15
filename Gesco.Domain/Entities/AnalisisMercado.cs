using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class AnalisisMercado
    {
        public int EmpresaId { get; set; }
        public string AgenteNombre { get; set; }
        public int AnalisisMercadoId { get; set; }
        public string NombrePropiedad { get; set; } // Ej: "Milena"
        public decimal M2Terreno { get; set; }
        public decimal PrecioM2Terreno { get; set; } // Nueva
        public decimal PrecioM2TerrenoUsd { get; set; }
        public decimal M2Construccion { get; set; }
        public decimal PrecioM2Construccion { get; set; } // Nueva
        public decimal PrecioM2ConstruccionUsd { get; set; }
        public decimal TipoCambio { get; set; } = 7070; // Nueva
        public decimal PrecioM2Sugerido { get; set; }
        public decimal ComisionPorcentaje { get; set; } = 5.5m; // Estándar Paraguay
        public decimal RetencionRentaPorcentaje { get; set; } = 2.4m; // Normativa DNIT
        public DateTime FechaCarga { get; set; }
        public decimal ResumenVentaGs { get; set; }
        public decimal ResumenVentaUsd { get; set; }
        public decimal ResumenComisionGs { get; set; }
        public decimal ResumenComisionUsd { get; set; }
        public decimal ResumenEscribaniaGs { get; set; }
        public decimal ResumenEscribaniaUsd { get; set; }
        public decimal ResumenRetencionGs { get; set; }
        public decimal ResumenRetencionUsd { get; set; }
        public decimal ResumenNetoGs { get; set; }
        public decimal ResumenNetoUsd { get; set; }

        public virtual ICollection<InmuebleReferencia> InmuebleReferencias { get; set; } = new List<InmuebleReferencia>();
    }
}
