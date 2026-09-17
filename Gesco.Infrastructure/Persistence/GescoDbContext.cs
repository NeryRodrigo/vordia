using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Infrastructure.Persistence
{
    public class GescoDbContext : DbContext, IApplicationDbContext
    {
        public GescoDbContext(DbContextOptions<GescoDbContext> options) : base(options) { }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<StockAlmacen> StockAlmacenes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<CompraCabecera> CompraCabeceras { get; set; }
        public DbSet<CompraDetalle> CompraDetalles { get; set; }
        public DbSet<MapeoPerfil> MapeoPerfiles { get; set; }
        public DbSet<MapeoDetalle> MapeoDetalles { get; set; }
        public DbSet<MapeoRegistro> MapeoRegistros { get; set; }
        public DbSet<VentaCabecera> VentaCabeceras { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }

        public DbSet<Propiedad> Propiedades { get; set; }
        public DbSet<DocumentoPropiedad> DocumentoPropiedades { get; set; }

        public DbSet<AnalisisMercado> AnalisisMercados { get; set; }

        public DbSet<InmuebleReferencia> InmuebleReferencia { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<SyncEstado> SyncEstado { get; set; }

        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<UsuarioModulo> UsuarioModulos { get; set; }
        public DbSet<LicenciaModulo> LicenciasModulos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }

        public DbSet<Habitacion> Habitaciones { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<UnidadCondominio> UnidadesCondominio { get; set; }
        public DbSet<ContratoInquilino> ContratosInquilinos { get; set; }
        public DbSet<PagoRecibo> PagosRecibos { get; set; }
        public DbSet<GastoMantenimiento> GastosMantenimiento { get; set; }
        public DbSet<DocumentoInquilino> DocumentosInquilino { get; set; }
        public DbSet<ConfiguracionTimbrado> ConfiguracionesTimbrado { get; set; }
        public DbSet<MorosidadInquilino> MorosidadesInquilino { get; set; }
        public DbSet<CuotaMorosidad> CuotasMorosidad { get; set; }
        public DbSet<SeguimientoMorosidad> SeguimientosMorosidad { get; set; }
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<ConfiguracionMulta> ConfiguracionesMultas { get; set; }
        public DbSet<PlantillaDocumento> PlantillasDocumento { get; set; }

        public DbSet<CrmCliente> CrmClientes { get; set; }
        public DbSet<CrmHistorial> CrmHistorial { get; set; }
        public DbSet<CrmContrato> CrmContratos { get; set; }
        public DbSet<CrmPago> CrmPagos { get; set; }
        public DbSet<CrmTarea> CrmTareas { get; set; }
        public DbSet<CrmSocio> CrmSocios { get; set; }
        public DbSet<CrmRoadmap> CrmRoadmap { get; set; }
        public DbSet<CrmRoadmapHistorial> CrmRoadmapHistorial { get; set; }
        public DbSet<FichaIncompleta> FichasIncompletas { get; set; }
        public DbSet<FontanaMovimiento> FontanaMovimientos { get; set; }
        public DbSet<FontanaPersonal> FontanaPersonal { get; set; }

        // VORDIA STAY
        public DbSet<StayTarifa> StayTarifas { get; set; }
        public DbSet<StayPago> StayPagos { get; set; }
        public DbSet<StayBloqueo> StayBloqueos { get; set; }
        public DbSet<StayHuesped> StayHuespedes { get; set; }

        // VORDIA AGRO
        public DbSet<AgroEstablecimiento> AgroEstablecimientos { get; set; }
        public DbSet<AgroLote> AgroLotes { get; set; }
        public DbSet<AgroZona> AgroZonas { get; set; }
        public DbSet<AgroAnimal> AgroAnimales { get; set; }
        public DbSet<AgroPesaje> AgroPesajes { get; set; }
        public DbSet<AgroRacion> AgroRaciones { get; set; }
        public DbSet<AgroDispositivo> AgroDispositivos { get; set; }
        public DbSet<AgroAlerta> AgroAlertas { get; set; }

        public DbSet<CrmCapitalMovimiento> CrmCapitalMovimientos { get; set; }
        public DbSet<CrmEmpleadoHq> CrmEmpleadosHq { get; set; }
        public DbSet<CrmGasto> CrmGastos { get; set; }
        public DbSet<CrmPropuesta> CrmPropuestas { get; set; }
        public DbSet<CrmVendedor> CrmVendedores { get; set; }
        public DbSet<CrmObjetivo> CrmObjetivos { get; set; }
        public DbSet<CrmProducto> CrmProductos { get; set; }
        public DbSet<CrmProveedor> CrmProveedores { get; set; }
        public DbSet<CrmProyecto> CrmProyectos { get; set; }
        public DbSet<CrmDocumento> CrmDocumentos { get; set; }
        public DbSet<CrmAutomatizacion> CrmAutomatizaciones { get; set; }
        public DbSet<CrmAutomatizacionEjecucion> CrmAutomatizacionEjecuciones { get; set; }

        public async Task BulkInsertAsync(List<Propiedad> batch)
        {
            /*if (batch == null || batch.Count == 0)
                return;

            await this.BulkInsertAsync(batch, new BulkConfig
            {
                BatchSize = 500,
                PreserveInsertOrder = true,
                SetOutputIdentity = true
            });*/
            await Propiedades.AddRangeAsync(batch);
            await SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Mapeo de tablas para el módulo de condominios (ajustado a nombres reales en DB)
            modelBuilder.Entity<UnidadCondominio>().ToTable("UnidadesCondominio");
            modelBuilder.Entity<ContratoInquilino>().ToTable("ContratosInquilino");
            modelBuilder.Entity<PagoRecibo>().ToTable("PagosRecibo");
            modelBuilder.Entity<DocumentoInquilino>().ToTable("DocumentosInquilino");
            modelBuilder.Entity<MorosidadInquilino>().ToTable("MorosidadesInquilino");
            modelBuilder.Entity<CuotaMorosidad>().ToTable("CuotasMorosidad");
            modelBuilder.Entity<SeguimientoMorosidad>().ToTable("SeguimientosMorosidad");
            modelBuilder.Entity<Actividad>().ToTable("Actividades");
            modelBuilder.Entity<ConfiguracionMulta>().ToTable("ConfiguracionesMultas");
            modelBuilder.Entity<PlantillaDocumento>().ToTable("PlantillasDocumento");
            modelBuilder.Entity<CrmCliente>().ToTable("CrmClientes");
            modelBuilder.Entity<CrmHistorial>().ToTable("CrmHistorial");
            modelBuilder.Entity<CrmContrato>().ToTable("CrmContratos");
            modelBuilder.Entity<CrmPago>().ToTable("CrmPagos");
            modelBuilder.Entity<CrmTarea>().ToTable("CrmTareas");
            modelBuilder.Entity<CrmSocio>().ToTable("CrmSocios");
            modelBuilder.Entity<CrmRoadmap>().ToTable("CrmRoadmap");
            modelBuilder.Entity<CrmRoadmapHistorial>().ToTable("CrmRoadmapHistorial");
            modelBuilder.Entity<FichaIncompleta>().ToTable("FichasIncompletas");
            modelBuilder.Entity<FontanaMovimiento>().ToTable("FontanaMovimientos");
            modelBuilder.Entity<FontanaPersonal>().ToTable("FontanaPersonal");
            modelBuilder.Entity<FontanaMovimiento>().Property(m => m.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<FontanaPersonal>().Property(p => p.Salario).HasPrecision(18, 2);

            modelBuilder.Entity<StayTarifa>().ToTable("StayTarifas");
            modelBuilder.Entity<StayPago>().ToTable("StayPagos");
            modelBuilder.Entity<StayBloqueo>().ToTable("StayBloqueos");
            modelBuilder.Entity<StayHuesped>().ToTable("StayHuespedes");
            modelBuilder.Entity<StayTarifa>().Property(t => t.PrecioPorNocheGs).HasPrecision(18, 2);
            modelBuilder.Entity<StayTarifa>().Property(t => t.PrecioPorSemanaGs).HasPrecision(18, 2);
            modelBuilder.Entity<StayTarifa>().Property(t => t.PrecioPorMesGs).HasPrecision(18, 2);
            modelBuilder.Entity<StayPago>().Property(p => p.MontoGs).HasPrecision(18, 2);

            modelBuilder.Entity<AgroEstablecimiento>().ToTable("AgroEstablecimientos");
            modelBuilder.Entity<AgroLote>().ToTable("AgroLotes");
            modelBuilder.Entity<AgroZona>().ToTable("AgroZonas");
            modelBuilder.Entity<AgroAnimal>().ToTable("AgroAnimales");
            modelBuilder.Entity<AgroPesaje>().ToTable("AgroPesajes");
            modelBuilder.Entity<AgroRacion>().ToTable("AgroRaciones");
            modelBuilder.Entity<AgroDispositivo>().ToTable("AgroDispositivos");
            modelBuilder.Entity<AgroAlerta>().ToTable("AgroAlertas");
            modelBuilder.Entity<AgroEstablecimiento>().Property(e => e.Hectareas).HasPrecision(18, 2);
            modelBuilder.Entity<AgroZona>().Property(z => z.LatCentro).HasPrecision(10, 6);
            modelBuilder.Entity<AgroZona>().Property(z => z.LngCentro).HasPrecision(10, 6);
            modelBuilder.Entity<AgroAnimal>().Property(a => a.PesoIngresoKg).HasPrecision(10, 2);
            modelBuilder.Entity<AgroAnimal>().Property(a => a.PesoActualKg).HasPrecision(10, 2);
            modelBuilder.Entity<AgroAnimal>().Property(a => a.UltimaLat).HasPrecision(10, 6);
            modelBuilder.Entity<AgroAnimal>().Property(a => a.UltimaLng).HasPrecision(10, 6);
            modelBuilder.Entity<AgroPesaje>().Property(p => p.PesoKg).HasPrecision(10, 2);
            modelBuilder.Entity<AgroRacion>().Property(r => r.CantidadKg).HasPrecision(12, 2);
            modelBuilder.Entity<AgroRacion>().Property(r => r.CostoGs).HasPrecision(18, 2);

            modelBuilder.Entity<CrmCapitalMovimiento>().ToTable("CrmCapitalMovimientos");
            modelBuilder.Entity<CrmEmpleadoHq>().ToTable("CrmEmpleadosHq");
            modelBuilder.Entity<CrmGasto>().ToTable("CrmGastos");
            modelBuilder.Entity<CrmGasto>().Property(g => g.MontoGs).HasPrecision(18, 2);
            modelBuilder.Entity<CrmPropuesta>().ToTable("CrmPropuestas");
            modelBuilder.Entity<CrmPropuesta>().Property(p => p.MontoGs).HasPrecision(18, 2);
            modelBuilder.Entity<CrmVendedor>().ToTable("CrmVendedores");
            modelBuilder.Entity<CrmVendedor>().Property(v => v.ComisionPct).HasPrecision(5, 2);
            modelBuilder.Entity<CrmVendedor>().Property(v => v.MetaMensualGs).HasPrecision(18, 2);
            modelBuilder.Entity<CrmObjetivo>().ToTable("CrmObjetivos");
            modelBuilder.Entity<CrmProducto>().ToTable("CrmProductos");
            modelBuilder.Entity<CrmProveedor>().ToTable("CrmProveedores");
            modelBuilder.Entity<CrmProyecto>().ToTable("CrmProyectos");
            modelBuilder.Entity<CrmCapitalMovimiento>().Property(c => c.MontoGs).HasPrecision(18, 2);
            modelBuilder.Entity<CrmEmpleadoHq>().Property(e => e.SalarioMensualGs).HasPrecision(18, 2);
            modelBuilder.Entity<CrmProducto>().Property(p => p.FeeEstimadoGs).HasPrecision(18, 2);

            modelBuilder.Entity<CrmCliente>()
                .HasMany(c => c.Historial)
                .WithOne(h => h.Cliente)
                .HasForeignKey(h => h.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CrmCliente>().Property(c => c.FeeEstimadoMensual).HasPrecision(18, 2);
            modelBuilder.Entity<CrmContrato>().Property(c => c.FeeAcordado).HasPrecision(18, 2);
            modelBuilder.Entity<CrmContrato>().Property(c => c.FeeMensual).HasPrecision(18, 2);
            modelBuilder.Entity<CrmContrato>().Property(c => c.FeeAnual).HasPrecision(18, 2);
            modelBuilder.Entity<CrmPago>().Property(p => p.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<CrmSocio>().Property(s => s.Porcentaje).HasPrecision(5, 2);

            modelBuilder.Entity<CrmContrato>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CrmPago>()
                .HasOne(p => p.Contrato)
                .WithMany()
                .HasForeignKey(p => p.ContratoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CrmPago>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CrmTarea>()
                .HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrmDocumento>().ToTable("CrmDocumentos");
            modelBuilder.Entity<CrmAutomatizacion>().ToTable("CrmAutomatizaciones");
            modelBuilder.Entity<CrmAutomatizacionEjecucion>().ToTable("CrmAutomatizacionEjecuciones");

            modelBuilder.Entity<CrmDocumento>()
                .HasOne(d => d.Cliente)
                .WithMany()
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CrmAutomatizacionEjecucion>()
                .HasOne(e => e.Automatizacion)
                .WithMany()
                .HasForeignKey(e => e.AutomatizacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Aquí puedes configurar precisión de decimales para Guaraníes
            modelBuilder.Entity<Producto>().Property(p => p.PrecioVenta).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Producto>().Property(p => p.Costo).HasPrecision(18, 2);
            modelBuilder.Entity<Producto>().Property(p => p.StockActual).HasPrecision(18, 2);

            // Configuraciones de WMS y Relaciones de Producto
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Marca)
                .WithMany()
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StockAlmacen>().Property(s => s.Cantidad).HasPrecision(18, 2);
            
            modelBuilder.Entity<StockAlmacen>()
                .HasOne(s => s.Almacen)
                .WithMany(a => a.Stocks)
                .HasForeignKey(s => s.AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAlmacen>()
                .HasOne(s => s.Producto)
                .WithMany(p => p.Stocks)
                .HasForeignKey(s => s.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.Empresa)
                .WithMany()
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Marca>()
                .HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Almacen>()
                .HasOne(a => a.Empresa)
                .WithMany()
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Compras
            modelBuilder.Entity<Proveedor>()
                .HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompraCabecera>()
                .HasOne(c => c.Empresa)
                .WithMany()
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompraCabecera>()
                .HasOne(c => c.Proveedor)
                .WithMany()
                .HasForeignKey(c => c.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompraCabecera>().Property(c => c.Total).HasPrecision(18, 2);
            modelBuilder.Entity<CompraDetalle>().Property(d => d.Cantidad).HasPrecision(18, 2);
            modelBuilder.Entity<CompraDetalle>().Property(d => d.CostoUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<CompraDetalle>().Property(d => d.Subtotal).HasPrecision(18, 2);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(d => d.CompraCabecera)
                .WithMany(c => c.Detalles)
                .HasForeignKey(d => d.CompraCabeceraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VentaCabecera>()
                        .HasIndex(v => new { v.EmpresaId, v.NroFactura })
                        .IsUnique();
            modelBuilder.Entity<VentaCabecera>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<VentaDetalle>().Property(v => v.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<VentaDetalle>().Property(v => v.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<SyncEstado>().HasKey(e => e.EmpresaId);

            // Hostal / PMS
            modelBuilder.Entity<Habitacion>().Property(h => h.PrecioBasePorNocheGs).HasPrecision(18, 2);
            modelBuilder.Entity<Reserva>().Property(r => r.MontoTotalGs).HasPrecision(18, 2);
            modelBuilder.Entity<Reserva>().Property(r => r.MontoPagadoGs).HasPrecision(18, 2);

            // Condominio / Alquileres
            modelBuilder.Entity<ContratoInquilino>().Property(c => c.MontoMensualAlquiler).HasPrecision(18, 2);
            modelBuilder.Entity<ContratoInquilino>().Property(c => c.DepositoGarantia).HasPrecision(18, 2);
            modelBuilder.Entity<PagoRecibo>().Property(p => p.MontoPagado).HasPrecision(18, 2);
            modelBuilder.Entity<PagoRecibo>().Property(p => p.SaldoPendiente).HasPrecision(18, 2);
            modelBuilder.Entity<PagoRecibo>().Property(p => p.Mora).HasPrecision(18, 2);
            modelBuilder.Entity<GastoMantenimiento>().Property(g => g.Monto).HasPrecision(18, 2);

            modelBuilder.Entity<ContratoInquilino>()
                .HasOne(c => c.Unidad)
                .WithMany(u => u.Contratos)
                .HasForeignKey(c => c.UnidadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoRecibo>()
                .HasOne(p => p.Contrato)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.ContratoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoRecibo>()
                .HasOne(p => p.CuotaMorosidad)
                .WithMany()
                .HasForeignKey(p => p.CuotaMorosidadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentoInquilino>()
                .HasOne(d => d.Contrato)
                .WithMany()
                .HasForeignKey(d => d.ContratoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MorosidadInquilino>()
                .HasOne(m => m.Inquilino)
                .WithMany()
                .HasForeignKey(m => m.InquilinoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MorosidadInquilino>()
                .HasOne(m => m.Unidad)
                .WithMany()
                .HasForeignKey(m => m.UnidadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CuotaMorosidad>()
                .HasOne(c => c.Morosidad)
                .WithMany(m => m.Cuotas)
                .HasForeignKey(c => c.MorosidadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SeguimientoMorosidad>()
                .HasOne(s => s.Cuota)
                .WithMany(c => c.Seguimientos)
                .HasForeignKey(s => s.CuotaId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configuración para la nueva tabla Propiedades
            modelBuilder.Entity<Propiedad>().Property(p => p.PrecioVentaGs).HasPrecision(18, 2);
            modelBuilder.Entity<Propiedad>().Property(p => p.SuperficieLoteM2).HasPrecision(18, 2);
            modelBuilder.Entity<Propiedad>().Property(p => p.AreaConstruccionM2).HasPrecision(18, 2);

            // Configuración Hostal / PMS - Evitar ciclos de cascada
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Empresa)
                .WithMany()
                .HasForeignKey(r => r.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(v => v.Producto)
                .WithMany()
                .HasForeignKey(v => v.ProductoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Habitacion)
                .WithMany()
                .HasForeignKey(r => r.HabitacionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Huesped)
                .WithMany()
                .HasForeignKey(r => r.HuespedId)
                .OnDelete(DeleteBehavior.Restrict);

            // Si querés que el título sea obligatorio y tenga un límite
            modelBuilder.Entity<Propiedad>().Property(p => p.Titulo).IsRequired().HasMaxLength(250);
            
            // Relación Propiedad -> Documentos
            modelBuilder.Entity<Propiedad>()
                .HasMany(p => p.Documentos)
                .WithOne(d => d.Propiedad)
                .HasForeignKey(d => d.PropiedadId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Usuario>()
                        .HasIndex(u => u.Email)
                        .IsUnique();
            modelBuilder.Entity<UsuarioModulo>()
                .HasKey(um => new { um.UsuarioId, um.ModuloId });

            modelBuilder.Entity<UsuarioModulo>()
                .HasOne(um => um.Usuario)
                .WithMany(u => u.UsuarioModulos)
                .HasForeignKey(um => um.UsuarioId);

            modelBuilder.Entity<UsuarioModulo>()
                .HasOne(um => um.Modulo)
                .WithMany(m => m.UsuarioModulos)
                .HasForeignKey(um => um.ModuloId);

            modelBuilder.Entity<LicenciaModulo>()
                .HasOne(lm => lm.Empresa)
                .WithMany()
                .HasForeignKey(lm => lm.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LicenciaModulo>()
                .HasOne(lm => lm.Modulo)
                .WithMany()
                .HasForeignKey(lm => lm.ModuloId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
