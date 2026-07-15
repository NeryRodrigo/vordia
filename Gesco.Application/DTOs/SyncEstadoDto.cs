using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class SyncEstadoDto
    {
        public int Total { get; set; }
        public string? UltimoMLSID { get; set; }
        public string Estado { get; set; }
    }
}
