using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class CatalogosDto
    {
        public List<Persona> Clientes { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Sucursal> Sucursales { get; set; } = new();
    }
}
