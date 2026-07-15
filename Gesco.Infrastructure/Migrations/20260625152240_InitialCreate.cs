using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gesco.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    ActividadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResponsableId = table.Column<int>(type: "int", nullable: true),
                    ResponsableNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.ActividadId);
                });

            migrationBuilder.CreateTable(
                name: "AnalisisMercados",
                columns: table => new
                {
                    AnalisisMercadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    AgenteNombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombrePropiedad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    M2Terreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioM2Terreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioM2TerrenoUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    M2Construccion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioM2Construccion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioM2ConstruccionUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoCambio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioM2Sugerido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComisionPorcentaje = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetencionRentaPorcentaje = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResumenVentaGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenVentaUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenComisionGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenComisionUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenEscribaniaGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenEscribaniaUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenRetencionGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenRetencionUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenNetoGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResumenNetoUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisisMercados", x => x.AnalisisMercadoId);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesMultas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    MontoMulta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoAplicacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiasGracia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesMultas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    EmpleadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.EmpleadoId);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RUC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rubro = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.EmpresaId);
                });

            migrationBuilder.CreateTable(
                name: "MapeoPerfiles",
                columns: table => new
                {
                    MapeoPerfilId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombrePerfil = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TablaDestino = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapeoPerfiles", x => x.MapeoPerfilId);
                });

            migrationBuilder.CreateTable(
                name: "Modulos",
                columns: table => new
                {
                    ModuloId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreModulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RubroAsociado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RutaFrontend = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulos", x => x.ModuloId);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    PersonaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DV = table.Column<int>(type: "int", nullable: true),
                    EsCliente = table.Column<bool>(type: "bit", nullable: false),
                    EsProveedor = table.Column<bool>(type: "bit", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.PersonaId);
                });

            migrationBuilder.CreateTable(
                name: "SyncEstado",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSyncEstado = table.Column<int>(type: "int", nullable: false),
                    TotalInsertados = table.Column<int>(type: "int", nullable: false),
                    UltimoMLSIDProcesado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEstado", x => x.EmpresaId);
                });

            migrationBuilder.CreateTable(
                name: "InmuebleReferencia",
                columns: table => new
                {
                    InmuebleReferenciaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalisisMercadoId = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioVentaGs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    M2Terreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InmuebleReferencia", x => x.InmuebleReferenciaId);
                    table.ForeignKey(
                        name: "FK_InmuebleReferencia_AnalisisMercados_AnalisisMercadoId",
                        column: x => x.AnalisisMercadoId,
                        principalTable: "AnalisisMercados",
                        principalColumn: "AnalisisMercadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    AlmacenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.AlmacenId);
                    table.ForeignKey(
                        name: "FK_Almacenes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.CategoriaId);
                    table.ForeignKey(
                        name: "FK_Categorias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesTimbrado",
                columns: table => new
                {
                    TimbradoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Ruc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroTimbrado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValidezDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidezHasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumeroDesde = table.Column<int>(type: "int", nullable: false),
                    NumeroHasta = table.Column<int>(type: "int", nullable: false),
                    UltimoNumeroUsado = table.Column<int>(type: "int", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstaActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesTimbrado", x => x.TimbradoId);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesTimbrado_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Habitaciones",
                columns: table => new
                {
                    HabitacionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CapacidadCamas = table.Column<int>(type: "int", nullable: false),
                    PrecioBasePorNocheGs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DetallesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habitaciones", x => x.HabitacionId);
                    table.ForeignKey(
                        name: "FK_Habitaciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    MarcaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.MarcaId);
                    table.ForeignKey(
                        name: "FK_Marcas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    ProveedorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ruc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.ProveedorId);
                    table.ForeignKey(
                        name: "FK_Proveedores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    SucursalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PuntoExpedicion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.SucursalId);
                    table.ForeignKey(
                        name: "FK_Sucursales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesCondominio",
                columns: table => new
                {
                    UnidadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NombreCondominio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoUnidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NisAnde = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedidorAgua = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesCondominio", x => x.UnidadId);
                    table.ForeignKey(
                        name: "FK_UnidadesCondominio_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    RequiereCambioPass = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaConexion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapeoDetalles",
                columns: table => new
                {
                    MapeoDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapeoPerfilId = table.Column<int>(type: "int", nullable: false),
                    ColumnaOrigen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnaDestino = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapeoDetalles", x => x.MapeoDetalleId);
                    table.ForeignKey(
                        name: "FK_MapeoDetalles_MapeoPerfiles_MapeoPerfilId",
                        column: x => x.MapeoPerfilId,
                        principalTable: "MapeoPerfiles",
                        principalColumn: "MapeoPerfilId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapeoRegistros",
                columns: table => new
                {
                    MapeoRegistroId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapeoPerfilId = table.Column<int>(type: "int", nullable: false),
                    DatosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilaHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapeoRegistros", x => x.MapeoRegistroId);
                    table.ForeignKey(
                        name: "FK_MapeoRegistros_MapeoPerfiles_MapeoPerfilId",
                        column: x => x.MapeoPerfilId,
                        principalTable: "MapeoPerfiles",
                        principalColumn: "MapeoPerfilId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicenciasModulos",
                columns: table => new
                {
                    LicenciaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ModuloId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    PruebaGratisActiva = table.Column<bool>(type: "bit", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenciasModulos", x => x.LicenciaId);
                    table.ForeignKey(
                        name: "FK_LicenciasModulos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LicenciasModulos_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "ModuloId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Propiedades",
                columns: table => new
                {
                    PropiedadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    AgenteId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MLSID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrecioVentaGs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SuperficieLoteM2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AreaConstruccionM2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NroAmbientes = table.Column<int>(type: "int", nullable: false),
                    NroDormitorios = table.Column<int>(type: "int", nullable: false),
                    NroBaños = table.Column<int>(type: "int", nullable: false),
                    TieneJardin = table.Column<bool>(type: "bit", nullable: false),
                    AptoOficina = table.Column<bool>(type: "bit", nullable: false),
                    DescripcionRemax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sync = table.Column<bool>(type: "bit", nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    FechaFirmaContrato = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCaducidad = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EsAprobada = table.Column<bool>(type: "bit", nullable: false),
                    AgenteCreadorNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImagenUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoAgenteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propiedades", x => x.PropiedadId);
                    table.ForeignKey(
                        name: "FK_Propiedades_Personas_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentaCabeceras",
                columns: table => new
                {
                    VentaCabeceraId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    PersonaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NroFactura = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaCabeceras", x => x.VentaCabeceraId);
                    table.ForeignKey(
                        name: "FK_VentaCabeceras_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    MarcaId = table.Column<int>(type: "int", nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoBarra = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IvaPorcentaje = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.ProductoId);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "CategoriaId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Productos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "MarcaId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompraCabeceras",
                columns: table => new
                {
                    CompraCabeceraId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    NroFactura = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaFactura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraCabeceras", x => x.CompraCabeceraId);
                    table.ForeignKey(
                        name: "FK_CompraCabeceras_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompraCabeceras_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "ProveedorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosInquilino",
                columns: table => new
                {
                    ContratoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicioContrato = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinContrato = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoMensualAlquiler = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositoGarantia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CantidadOcupantes = table.Column<int>(type: "int", nullable: false),
                    VehiculoInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiciosIncluidos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstaActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosInquilino", x => x.ContratoId);
                    table.ForeignKey(
                        name: "FK_ContratosInquilino_Personas_InquilinoId",
                        column: x => x.InquilinoId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContratosInquilino_UnidadesCondominio_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "UnidadesCondominio",
                        principalColumn: "UnidadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GastosMantenimiento",
                columns: table => new
                {
                    GastoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentificadorServicio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NroComprobante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MesReferencia = table.Column<int>(type: "int", nullable: true),
                    AnioReferencia = table.Column<int>(type: "int", nullable: true),
                    MetodoPago = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastosMantenimiento", x => x.GastoId);
                    table.ForeignKey(
                        name: "FK_GastosMantenimiento_UnidadesCondominio_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "UnidadesCondominio",
                        principalColumn: "UnidadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MorosidadesInquilino",
                columns: table => new
                {
                    MorosidadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CantidadCuotas = table.Column<int>(type: "int", nullable: false),
                    MontoInteresDiario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InteresAcumuladoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UltimaFechaCalculoInteres = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservacionesGenerales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorosidadesInquilino", x => x.MorosidadId);
                    table.ForeignKey(
                        name: "FK_MorosidadesInquilino_Personas_InquilinoId",
                        column: x => x.InquilinoId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MorosidadesInquilino_UnidadesCondominio_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "UnidadesCondominio",
                        principalColumn: "UnidadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    ReservaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    HabitacionId = table.Column<int>(type: "int", nullable: false),
                    HuespedId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCheckOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MontoTotalGs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoPagadoGs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrigenReserva = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasAdicionales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.ReservaId);
                    table.ForeignKey(
                        name: "FK_Reservas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Habitaciones_HabitacionId",
                        column: x => x.HabitacionId,
                        principalTable: "Habitaciones",
                        principalColumn: "HabitacionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Personas_HuespedId",
                        column: x => x.HuespedId,
                        principalTable: "Personas",
                        principalColumn: "PersonaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateTable(
                name: "UsuarioModulos",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ModuloId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioModulos", x => new { x.UsuarioId, x.ModuloId });
                    table.ForeignKey(
                        name: "FK_UsuarioModulos_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "ModuloId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioModulos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoPropiedades",
                columns: table => new
                {
                    DocumentoPropiedadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoPropiedades", x => x.DocumentoPropiedadId);
                    table.ForeignKey(
                        name: "FK_DocumentoPropiedades_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "PropiedadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAlmacenes",
                columns: table => new
                {
                    StockAlmacenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlmacenes", x => x.StockAlmacenId);
                    table.ForeignKey(
                        name: "FK_StockAlmacenes_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "AlmacenId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAlmacenes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaDetalles",
                columns: table => new
                {
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaCabeceraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IvaPorcentaje = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaDetalles", x => x.VentaDetalleId);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VentaDetalles_VentaCabeceras_VentaCabeceraId",
                        column: x => x.VentaCabeceraId,
                        principalTable: "VentaCabeceras",
                        principalColumn: "VentaCabeceraId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompraDetalles",
                columns: table => new
                {
                    CompraDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompraCabeceraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraDetalles", x => x.CompraDetalleId);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_CompraCabeceras_CompraCabeceraId",
                        column: x => x.CompraCabeceraId,
                        principalTable: "CompraCabeceras",
                        principalColumn: "CompraCabeceraId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompraDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosInquilino",
                columns: table => new
                {
                    DocumentoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsEntregado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosInquilino", x => x.DocumentoId);
                    table.ForeignKey(
                        name: "FK_DocumentosInquilino_ContratosInquilino_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "ContratosInquilino",
                        principalColumn: "ContratoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CuotasMorosidad",
                columns: table => new
                {
                    CuotaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MorosidadId = table.Column<int>(type: "int", nullable: false),
                    NumeroCuota = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstaPagado = table.Column<bool>(type: "bit", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuotasMorosidad", x => x.CuotaId);
                    table.ForeignKey(
                        name: "FK_CuotasMorosidad_MorosidadesInquilino_MorosidadId",
                        column: x => x.MorosidadId,
                        principalTable: "MorosidadesInquilino",
                        principalColumn: "MorosidadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagosRecibo",
                columns: table => new
                {
                    PagoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    CuotaMorosidadId = table.Column<int>(type: "int", nullable: true),
                    TipoPago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MesPagado = table.Column<int>(type: "int", nullable: false),
                    AnioPagado = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Mora = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FacturaEmitida = table.Column<bool>(type: "bit", nullable: false),
                    NumeroFactura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComprobanteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NroComprobante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Banco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsableCobro = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosRecibo", x => x.PagoId);
                    table.ForeignKey(
                        name: "FK_PagosRecibo_ContratosInquilino_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "ContratosInquilino",
                        principalColumn: "ContratoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosRecibo_CuotasMorosidad_CuotaMorosidadId",
                        column: x => x.CuotaMorosidadId,
                        principalTable: "CuotasMorosidad",
                        principalColumn: "CuotaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeguimientosMorosidad",
                columns: table => new
                {
                    SeguimientoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuotaId = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguimientosMorosidad", x => x.SeguimientoId);
                    table.ForeignKey(
                        name: "FK_SeguimientosMorosidad_CuotasMorosidad_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "CuotasMorosidad",
                        principalColumn: "CuotaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_EmpresaId",
                table: "Almacenes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_EmpresaId",
                table: "Categorias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraCabeceras_EmpresaId",
                table: "CompraCabeceras",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraCabeceras_ProveedorId",
                table: "CompraCabeceras",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_CompraCabeceraId",
                table: "CompraDetalles",
                column: "CompraCabeceraId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_ProductoId",
                table: "CompraDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesTimbrado_EmpresaId",
                table: "ConfiguracionesTimbrado",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosInquilino_InquilinoId",
                table: "ContratosInquilino",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosInquilino_UnidadId",
                table: "ContratosInquilino",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_CuotasMorosidad_MorosidadId",
                table: "CuotasMorosidad",
                column: "MorosidadId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoPropiedades_PropiedadId",
                table: "DocumentoPropiedades",
                column: "PropiedadId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosInquilino_ContratoId",
                table: "DocumentosInquilino",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_GastosMantenimiento_UnidadId",
                table: "GastosMantenimiento",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Habitaciones_EmpresaId",
                table: "Habitaciones",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_InmuebleReferencia_AnalisisMercadoId",
                table: "InmuebleReferencia",
                column: "AnalisisMercadoId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenciasModulos_EmpresaId",
                table: "LicenciasModulos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenciasModulos_ModuloId",
                table: "LicenciasModulos",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_MapeoDetalles_MapeoPerfilId",
                table: "MapeoDetalles",
                column: "MapeoPerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_MapeoRegistros_MapeoPerfilId",
                table: "MapeoRegistros",
                column: "MapeoPerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_EmpresaId",
                table: "Marcas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MorosidadesInquilino_InquilinoId",
                table: "MorosidadesInquilino",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MorosidadesInquilino_UnidadId",
                table: "MorosidadesInquilino",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosRecibo_ContratoId",
                table: "PagosRecibo",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosRecibo_CuotaMorosidadId",
                table: "PagosRecibo",
                column: "CuotaMorosidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId",
                table: "Productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_EmpresaId",
                table: "Productos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_AgenteId",
                table: "Propiedades",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId",
                table: "Proveedores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EmpresaId",
                table: "Reservas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_HabitacionId",
                table: "Reservas",
                column: "HabitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_HuespedId",
                table: "Reservas",
                column: "HuespedId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SeguimientosMorosidad_CuotaId",
                table: "SeguimientosMorosidad",
                column: "CuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacenes_AlmacenId",
                table: "StockAlmacenes",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlmacenes_ProductoId",
                table: "StockAlmacenes",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_EmpresaId",
                table: "Sucursales",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesCondominio_EmpresaId",
                table: "UnidadesCondominio",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioModulos_ModuloId",
                table: "UsuarioModulos",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId",
                table: "Usuarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaCabeceras_EmpresaId_NroFactura",
                table: "VentaCabeceras",
                columns: new[] { "EmpresaId", "NroFactura" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaCabeceras_PersonaId",
                table: "VentaCabeceras",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_ProductoId",
                table: "VentaDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_VentaCabeceraId",
                table: "VentaDetalles",
                column: "VentaCabeceraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "CompraDetalles");

            migrationBuilder.DropTable(
                name: "ConfiguracionesMultas");

            migrationBuilder.DropTable(
                name: "ConfiguracionesTimbrado");

            migrationBuilder.DropTable(
                name: "DocumentoPropiedades");

            migrationBuilder.DropTable(
                name: "DocumentosInquilino");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "GastosMantenimiento");

            migrationBuilder.DropTable(
                name: "InmuebleReferencia");

            migrationBuilder.DropTable(
                name: "LicenciasModulos");

            migrationBuilder.DropTable(
                name: "MapeoDetalles");

            migrationBuilder.DropTable(
                name: "MapeoRegistros");

            migrationBuilder.DropTable(
                name: "PagosRecibo");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "SeguimientosMorosidad");

            migrationBuilder.DropTable(
                name: "StockAlmacenes");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "SyncEstado");

            migrationBuilder.DropTable(
                name: "UsuarioModulos");

            migrationBuilder.DropTable(
                name: "VentaDetalles");

            migrationBuilder.DropTable(
                name: "CompraCabeceras");

            migrationBuilder.DropTable(
                name: "Propiedades");

            migrationBuilder.DropTable(
                name: "AnalisisMercados");

            migrationBuilder.DropTable(
                name: "MapeoPerfiles");

            migrationBuilder.DropTable(
                name: "ContratosInquilino");

            migrationBuilder.DropTable(
                name: "Habitaciones");

            migrationBuilder.DropTable(
                name: "CuotasMorosidad");

            migrationBuilder.DropTable(
                name: "Almacenes");

            migrationBuilder.DropTable(
                name: "Modulos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "VentaCabeceras");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "MorosidadesInquilino");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Personas");

            migrationBuilder.DropTable(
                name: "UnidadesCondominio");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
