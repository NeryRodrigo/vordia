using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Domain.Entities
{
    public class MapeoPerfil
    {
        public int MapeoPerfilId { get; set; }
        public string NombrePerfil { get; set; } = string.Empty; // Ej: "Migración Contasys"
        public string TablaDestino { get; set; } = string.Empty; // Ej: "Persona"

        public ICollection<MapeoDetalle> Detalles { get; set; } = new List<MapeoDetalle>();
    }

    public class MapeoDetalle
    {
        public int MapeoDetalleId { get; set; }
        public int MapeoPerfilId { get; set; }
        public string ColumnaOrigen { get; set; } = string.Empty; // El nombre en el Excel
        public string ColumnaDestino { get; set; } = string.Empty; // El campo en Gesco

        public MapeoPerfil MapeoPerfil { get; set; } = null!;
    }

    public class MapeoRegistro // Representa una fila del Excel migrado
    {
        public int MapeoRegistroId { get; set; }
        public int MapeoPerfilId { get; set; } // Link a tu clase existente
        public string? DatosJson { get; set; }  // El contenido de la fila
        public string? FilaHash { get; set; }   // La huella para NO DUPLICAR

        public MapeoPerfil MapeoPerfil { get; set; } = null!;
    }
}
