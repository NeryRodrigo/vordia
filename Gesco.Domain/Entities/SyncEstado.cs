using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class SyncEstado
    {
        [Key]
        public int IdSyncEstado { get; set; }
        public int EmpresaId { get; set; }
        public int TotalInsertados { get; set; }
        public string? UltimoMLSIDProcesado { get; set; }
        public string Estado { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
