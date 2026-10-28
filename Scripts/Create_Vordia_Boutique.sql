/*
  VORDIA BOUTIQUE · Gestión de boutiques (prendas con talle y color, stock, caja con apartados,
  alquileres por fecha, pruebas y clientas) y VORDIA CLOSET, el marketplace que junta el stock
  de todas las boutiques de Asunción y Gran Asunción. Se puede correr más de una vez.
*/
USE GESCO_DB;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.BoutiquePerfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiquePerfiles (
        PerfilId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiquePerfiles PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        NombreComercial  NVARCHAR(120) NOT NULL,
        Slug             NVARCHAR(80) NOT NULL,
        Descripcion      NVARCHAR(500) NULL,
        Ciudad           NVARCHAR(60) NULL,
        Barrio           NVARCHAR(80) NULL,
        Direccion        NVARCHAR(250) NULL,
        Whatsapp         NVARCHAR(40) NULL,
        Instagram        NVARCHAR(80) NULL,
        Horario          NVARCHAR(200) NULL,
        Estilo           NVARCHAR(60) NULL,
        Verificada       BIT NOT NULL CONSTRAINT DF_BtqPerfil_Verif DEFAULT (0),
        Publicada        BIT NOT NULL CONSTRAINT DF_BtqPerfil_Pub DEFAULT (1),
        Demo             BIT NOT NULL CONSTRAINT DF_BtqPerfil_Demo DEFAULT (0),
        FechaAlta        DATETIME2 NOT NULL CONSTRAINT DF_BtqPerfil_Alta DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_BoutiquePerfiles_Empresa ON dbo.BoutiquePerfiles (EmpresaId);
    CREATE UNIQUE INDEX UX_BoutiquePerfiles_Slug ON dbo.BoutiquePerfiles (Slug);
    PRINT 'Tabla BoutiquePerfiles creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiquePrendas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiquePrendas (
        PrendaId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiquePrendas PRIMARY KEY,
        EmpresaId       INT NOT NULL,
        Nombre          NVARCHAR(150) NOT NULL,
        Categoria       NVARCHAR(40) NOT NULL,
        Ocasiones       NVARCHAR(200) NULL,
        Descripcion     NVARCHAR(1500) NULL,
        Material        NVARCHAR(120) NULL,
        Modalidad       NVARCHAR(10) NOT NULL CONSTRAINT DF_BtqPrenda_Modo DEFAULT (N'Venta'),
        PrecioVenta     DECIMAL(18,2) NOT NULL CONSTRAINT DF_BtqPrenda_PV DEFAULT (0),
        PrecioAlquiler  DECIMAL(18,2) NOT NULL CONSTRAINT DF_BtqPrenda_PA DEFAULT (0),
        DiasAlquiler    INT NOT NULL CONSTRAINT DF_BtqPrenda_Dias DEFAULT (4),
        Garantia        DECIMAL(18,2) NOT NULL CONSTRAINT DF_BtqPrenda_Gar DEFAULT (0),
        PrecioAnterior  DECIMAL(18,2) NULL,
        ColorHex        NVARCHAR(9) NULL,
        Publicada       BIT NOT NULL CONSTRAINT DF_BtqPrenda_Pub DEFAULT (1),
        Activo          BIT NOT NULL CONSTRAINT DF_BtqPrenda_Activo DEFAULT (1),
        Vistas          INT NOT NULL CONSTRAINT DF_BtqPrenda_Vistas DEFAULT (0),
        FechaAlta       DATETIME2 NOT NULL CONSTRAINT DF_BtqPrenda_Alta DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_BtqPrenda_Modo CHECK (Modalidad IN (N'Venta', N'Alquiler', N'Ambos'))
    );
    CREATE INDEX IX_BoutiquePrendas_Empresa ON dbo.BoutiquePrendas (EmpresaId, Activo, Publicada);
    PRINT 'Tabla BoutiquePrendas creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueVariantes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueVariantes (
        VarianteId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueVariantes PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        PrendaId      INT NOT NULL CONSTRAINT FK_BtqVar_Prenda REFERENCES dbo.BoutiquePrendas(PrendaId),
        Talle         NVARCHAR(10) NOT NULL,
        Color         NVARCHAR(40) NOT NULL,
        ColorHex      NVARCHAR(9) NULL,
        Stock         INT NOT NULL CONSTRAINT DF_BtqVar_Stock DEFAULT (0),
        BustoCm       INT NULL,
        CinturaCm     INT NULL,
        CaderaCm      INT NULL,
        LargoCm       INT NULL,
        CodigoBarras  NVARCHAR(40) NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_BtqVar_Activo DEFAULT (1)
    );
    CREATE INDEX IX_BoutiqueVariantes_Prenda ON dbo.BoutiqueVariantes (PrendaId, Activo);
    PRINT 'Tabla BoutiqueVariantes creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueFotos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueFotos (
        FotoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueFotos PRIMARY KEY,
        EmpresaId    INT NOT NULL,
        PrendaId     INT NOT NULL CONSTRAINT FK_BtqFoto_Prenda REFERENCES dbo.BoutiquePrendas(PrendaId),
        Archivo      NVARCHAR(80) NOT NULL,
        ContentType  NVARCHAR(40) NOT NULL,
        Orden        INT NOT NULL CONSTRAINT DF_BtqFoto_Orden DEFAULT (0),
        Fecha        DATETIME2 NOT NULL CONSTRAINT DF_BtqFoto_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_BoutiqueFotos_Prenda ON dbo.BoutiqueFotos (PrendaId, Orden);
    PRINT 'Tabla BoutiqueFotos creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueClientas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueClientas (
        ClientaId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueClientas PRIMARY KEY,
        EmpresaId   INT NOT NULL,
        Nombre      NVARCHAR(150) NOT NULL,
        Whatsapp    NVARCHAR(40) NULL,
        Email       NVARCHAR(150) NULL,
        Talle       NVARCHAR(10) NULL,
        Cumpleanos  DATETIME2 NULL,
        Notas       NVARCHAR(500) NULL,
        Origen      NVARCHAR(20) NOT NULL CONSTRAINT DF_BtqCli_Origen DEFAULT (N'Caja'),
        FechaAlta   DATETIME2 NOT NULL CONSTRAINT DF_BtqCli_Alta DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_BoutiqueClientas_Empresa ON dbo.BoutiqueClientas (EmpresaId, Nombre);
    PRINT 'Tabla BoutiqueClientas creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueVentas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueVentas (
        VentaId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueVentas PRIMARY KEY,
        EmpresaId      INT NOT NULL,
        ClientaId      INT NULL,
        Estado         NVARCHAR(12) NOT NULL CONSTRAINT DF_BtqVenta_Estado DEFAULT (N'Pagada'),
        Total          DECIMAL(18,2) NOT NULL,
        Pagado         DECIMAL(18,2) NOT NULL,
        MedioPago      NVARCHAR(20) NULL,
        Canal          NVARCHAR(20) NOT NULL CONSTRAINT DF_BtqVenta_Canal DEFAULT (N'Tienda'),
        VenceApartado  DATETIME2 NULL,
        Notas          NVARCHAR(300) NULL,
        Vendedora      NVARCHAR(150) NULL,
        Fecha          DATETIME2 NOT NULL CONSTRAINT DF_BtqVenta_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_BtqVenta_Estado CHECK (Estado IN (N'Pagada', N'Apartada', N'Anulada'))
    );
    CREATE INDEX IX_BoutiqueVentas_Empresa ON dbo.BoutiqueVentas (EmpresaId, Fecha);
    PRINT 'Tabla BoutiqueVentas creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueVentaItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueVentaItems (
        ItemId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueVentaItems PRIMARY KEY,
        VentaId         INT NOT NULL CONSTRAINT FK_BtqItem_Venta REFERENCES dbo.BoutiqueVentas(VentaId),
        VarianteId      INT NOT NULL CONSTRAINT FK_BtqItem_Variante REFERENCES dbo.BoutiqueVariantes(VarianteId),
        Cantidad        INT NOT NULL CONSTRAINT DF_BtqItem_Cant DEFAULT (1),
        PrecioUnitario  DECIMAL(18,2) NOT NULL,
        Descripcion     NVARCHAR(220) NOT NULL
    );
    CREATE INDEX IX_BoutiqueVentaItems_Venta ON dbo.BoutiqueVentaItems (VentaId);
    PRINT 'Tabla BoutiqueVentaItems creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiqueAlquileres', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiqueAlquileres (
        AlquilerId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiqueAlquileres PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        VarianteId       INT NOT NULL CONSTRAINT FK_BtqAlq_Variante REFERENCES dbo.BoutiqueVariantes(VarianteId),
        ClientaId        INT NULL,
        ClientaNombre    NVARCHAR(150) NOT NULL,
        ClientaWhatsapp  NVARCHAR(40) NULL,
        Desde            DATETIME2 NOT NULL,
        Hasta            DATETIME2 NOT NULL,
        Estado           NVARCHAR(12) NOT NULL CONSTRAINT DF_BtqAlq_Estado DEFAULT (N'Reservado'),
        Precio           DECIMAL(18,2) NOT NULL,
        Sena             DECIMAL(18,2) NOT NULL CONSTRAINT DF_BtqAlq_Sena DEFAULT (0),
        Garantia         DECIMAL(18,2) NOT NULL CONSTRAINT DF_BtqAlq_Gar DEFAULT (0),
        Notas            NVARCHAR(300) NULL,
        Fecha            DATETIME2 NOT NULL CONSTRAINT DF_BtqAlq_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_BtqAlq_Estado CHECK (Estado IN (N'Reservado', N'Retirado', N'Devuelto', N'Cancelado'))
    );
    CREATE INDEX IX_BoutiqueAlquileres_Variante ON dbo.BoutiqueAlquileres (VarianteId, Desde, Hasta);
    CREATE INDEX IX_BoutiqueAlquileres_Empresa ON dbo.BoutiqueAlquileres (EmpresaId, Desde);
    PRINT 'Tabla BoutiqueAlquileres creada';
END
GO

IF OBJECT_ID(N'dbo.BoutiquePruebas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BoutiquePruebas (
        PruebaId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BoutiquePruebas PRIMARY KEY,
        EmpresaId    INT NOT NULL,
        PrendaId     INT NOT NULL CONSTRAINT FK_BtqPrueba_Prenda REFERENCES dbo.BoutiquePrendas(PrendaId),
        VarianteId   INT NULL,
        Nombre       NVARCHAR(150) NOT NULL,
        Whatsapp     NVARCHAR(40) NOT NULL,
        FechaHora    DATETIME2 NOT NULL,
        FechaEvento  DATETIME2 NULL,
        Estado       NVARCHAR(12) NOT NULL CONSTRAINT DF_BtqPrueba_Estado DEFAULT (N'Pedida'),
        Mensaje      NVARCHAR(500) NULL,
        Fecha        DATETIME2 NOT NULL CONSTRAINT DF_BtqPrueba_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_BtqPrueba_Estado CHECK (Estado IN (N'Pedida', N'Confirmada', N'Vino', N'NoVino', N'Cancelada'))
    );
    CREATE INDEX IX_BoutiquePruebas_Empresa ON dbo.BoutiquePruebas (EmpresaId, FechaHora);
    PRINT 'Tabla BoutiquePruebas creada';
END
GO

/* Boutique de demostración (el usuario se crea desde HQ → Usuarios con el entorno "Boutique Demo")
   y cuatro boutiques de ejemplo para que el marketplace VORDIA CLOSET se vea lleno en la demo.
   Las prendas se cargan desde la app con el botón "Cargar demo". */
IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Rubro = N'BOUTIQUE' AND RazonSocial = N'Boutique Demo')
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Boutique Demo', N'80000001-9', N'Villa Morra, Asunción', N'BOUTIQUE');
    PRINT 'Empresa demo BOUTIQUE creada';
END
GO

DECLARE @n INT = (SELECT COUNT(*) FROM dbo.Empresas WHERE Rubro = N'CLOSET_DEMO');
WHILE @n < 4
BEGIN
    SET @n += 1;
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (CONCAT(N'Closet Demo ', @n), N'80000002-7', N'Gran Asunción', N'CLOSET_DEMO');
END
PRINT 'Boutiques de ejemplo del marketplace listas';
GO

PRINT '=== VORDIA BOUTIQUE listo ===';
GO
