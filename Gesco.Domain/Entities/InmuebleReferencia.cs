using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class InmuebleReferencia
    {
        public int AnalisisMercadoId { get; set; }
        public int InmuebleReferenciaId { get; set; }
        public int EmpresaId { get; set; }
        public string LinkUrl { get; set; }
        public decimal PrecioVentaGs { get; set; }
        public decimal M2Terreno { get; set; }
        public DateTime FechaCarga { get; set; }
        public decimal PrecioM2Gs => PrecioVentaGs / M2Terreno; // Lógica calculada

        [System.Text.Json.Serialization.JsonIgnore]
        public virtual AnalisisMercado AnalisisMercado { get; set; } = null!;
    }
}
