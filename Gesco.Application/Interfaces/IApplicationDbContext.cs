using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DatabaseFacade Database { get; }

        DbSet<Persona> Personas { get; set; }
        DbSet<Producto> Productos { get; set; }
        DbSet<Categoria> Categorias { get; set; }
        DbSet<Marca> Marcas { get; set; }
        DbSet<Almacen> Almacenes { get; set; }
        DbSet<StockAlmacen> StockAlmacenes { get; set; }
        DbSet<Proveedor> Proveedores { get; set; }
        DbSet<CompraCabecera> CompraCabeceras { get; set; }
        DbSet<CompraDetalle> CompraDetalles { get; set; }
        DbSet<Empresa> Empresas { get; set; }
        DbSet<Sucursal> Sucursales { get; set; }
        // Agregá aquí todos los DbSet que necesites usar en tus servicios
        DbSet<VentaCabecera> VentaCabeceras { get; set; }
        DbSet<VentaDetalle> VentaDetalles { get; set; }
        DbSet<MapeoPerfil> MapeoPerfiles { get; set; }
        DbSet<MapeoDetalle> MapeoDetalles { get; set; }
        DbSet<MapeoRegistro> MapeoRegistros { get; set; }

        DbSet<Propiedad> Propiedades { get; set; }

        DbSet<SyncEstado> SyncEstado { get; set; }

        DbSet<AnalisisMercado> AnalisisMercados { get; set; }
        DbSet<InmuebleReferencia> InmuebleReferencia { get; set; }

        DbSet<Usuario> Usuarios { get; set; }
        DbSet<Modulo> Modulos { get; set; }
        DbSet<UsuarioModulo> UsuarioModulos { get; set; }
        DbSet<LicenciaModulo> LicenciasModulos { get; set; }
        DbSet<Empleado> Empleados { get; set; }

        DbSet<Habitacion> Habitaciones { get; set; }
        DbSet<Reserva> Reservas { get; set; }

        DbSet<UnidadCondominio> UnidadesCondominio { get; set; }
        DbSet<ContratoInquilino> ContratosInquilinos { get; set; }
        DbSet<PagoRecibo> PagosRecibos { get; set; }
        DbSet<GastoMantenimiento> GastosMantenimiento { get; set; }
        DbSet<DocumentoInquilino> DocumentosInquilino { get; set; }
        DbSet<ConfiguracionTimbrado> ConfiguracionesTimbrado { get; set; }
        DbSet<MorosidadInquilino> MorosidadesInquilino { get; set; }
        DbSet<CuotaMorosidad> CuotasMorosidad { get; set; }
        DbSet<SeguimientoMorosidad> SeguimientosMorosidad { get; set; }
        DbSet<Actividad> Actividades { get; set; }
        DbSet<ConfiguracionMulta> ConfiguracionesMultas { get; set; }
        DbSet<PlantillaDocumento> PlantillasDocumento { get; set; }

        Task BulkInsertAsync(List<Propiedad> batch);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
