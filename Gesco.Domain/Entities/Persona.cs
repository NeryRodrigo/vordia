using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class Persona
    {
        public string RUC;

        public int PersonaId { get; set; }
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; }
        public string Documento { get; set; } // RUC o CI
        public int? DV { get; set; } // Dígito Verificador
        public bool EsCliente { get; set; }
        public bool EsProveedor { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        [NotMapped]
        public bool TieneFacturas { get; set; }
    }
}
