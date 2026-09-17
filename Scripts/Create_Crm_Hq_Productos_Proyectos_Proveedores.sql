/*
  Vordia HQ — Productos (línea VORDIA), Proveedores y Proyectos
  Idempotente. Compatible sqlcmd (QUOTED_IDENTIFIER ON).
  No toca Habitat/Stay/Fontana.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
GO

/* ========== PRODUCTOS ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProductos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmProductos (
        ProductoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmProductos PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        Codigo           NVARCHAR(30) NOT NULL,  -- HQ | HABITAT | STAY | DINE | RETAIL | AGRO
        Nombre           NVARCHAR(120) NOT NULL,
        Significado      NVARCHAR(500) NULL,
        QueSera          NVARCHAR(1000) NULL,
        QueIncluye       NVARCHAR(MAX) NULL,
        IdeaCentral      NVARCHAR(1000) NULL,
        Estado           NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmProd_Estado DEFAULT (N'Planificado'),
        Prioridad        NVARCHAR(10) NOT NULL CONSTRAINT DF_CrmProd_Pri DEFAULT (N'P2'),
        FeeEstimadoGs    DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmProd_Fee DEFAULT (0),
        Orden            INT NOT NULL CONSTRAINT DF_CrmProd_Orden DEFAULT (0),
        Notas            NVARCHAR(MAX) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_CrmProd_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmProd_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmProd_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmProd_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT UQ_CrmProd_EmpresaCodigo UNIQUE (EmpresaId, Codigo),
        CONSTRAINT CK_CrmProd_Estado CHECK (Estado IN (N'Activo', N'En desarrollo', N'Planificado', N'Pausado')),
        CONSTRAINT CK_CrmProd_Pri CHECK (Prioridad IN (N'P0', N'P1', N'P2'))
    );
    PRINT 'CrmProductos creada';
END
ELSE PRINT 'CrmProductos ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProductos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmProd_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmProductos'))
BEGIN
    CREATE INDEX IX_CrmProd_Empresa ON dbo.CrmProductos (EmpresaId, Activo, Orden, Estado);
    PRINT 'IX_CrmProd_Empresa creado';
END
GO

/* ========== PROVEEDORES ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProveedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmProveedores (
        ProveedorId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmProveedores PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        Nombre           NVARCHAR(200) NOT NULL,
        Categoria        NVARCHAR(80) NOT NULL CONSTRAINT DF_CrmProv_Cat DEFAULT (N'General'),
        ContactoNombre   NVARCHAR(150) NULL,
        Email            NVARCHAR(150) NULL,
        Telefono         NVARCHAR(50) NULL,
        Ruc              NVARCHAR(30) NULL,
        ProductoCodigo   NVARCHAR(30) NULL, -- vínculo opcional a línea VORDIA
        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmProv_Estado DEFAULT (N'Activo'),
        Notas            NVARCHAR(MAX) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_CrmProv_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmProv_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmProv_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmProv_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmProv_Estado CHECK (Estado IN (N'Activo', N'Inactivo', N'Prospecto'))
    );
    PRINT 'CrmProveedores creada';
END
ELSE PRINT 'CrmProveedores ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProveedores', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmProv_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmProveedores'))
BEGIN
    CREATE INDEX IX_CrmProv_Empresa ON dbo.CrmProveedores (EmpresaId, Activo, Estado, Categoria);
    PRINT 'IX_CrmProv_Empresa creado';
END
GO

/* ========== PROYECTOS ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProyectos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmProyectos (
        ProyectoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmProyectos PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        Titulo           NVARCHAR(200) NOT NULL,
        Descripcion      NVARCHAR(MAX) NULL,
        ProductoCodigo   NVARCHAR(30) NULL, -- HQ | HABITAT | STAY | ...
        Estado           NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmPy_Estado DEFAULT (N'Planificado'),
        Prioridad        NVARCHAR(10) NOT NULL CONSTRAINT DF_CrmPy_Pri DEFAULT (N'P1'),
        ClienteId        INT NULL,
        FechaInicio      DATE NULL,
        FechaFin         DATE NULL,
        ResponsableNombre NVARCHAR(150) NULL,
        AvancePct        INT NOT NULL CONSTRAINT DF_CrmPy_Avance DEFAULT (0),
        Notas            NVARCHAR(MAX) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_CrmPy_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmPy_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmPy_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmPy_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmPy_Estado CHECK (Estado IN (N'Planificado', N'En curso', N'Pausado', N'Hecho', N'Cancelado')),
        CONSTRAINT CK_CrmPy_Pri CHECK (Prioridad IN (N'P0', N'P1', N'P2')),
        CONSTRAINT CK_CrmPy_Avance CHECK (AvancePct BETWEEN 0 AND 100)
    );
    PRINT 'CrmProyectos creada';
END
ELSE PRINT 'CrmProyectos ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.CrmProyectos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmPy_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmProyectos'))
BEGIN
    CREATE INDEX IX_CrmPy_Empresa ON dbo.CrmProyectos (EmpresaId, Activo, Estado, ProductoCodigo);
    PRINT 'IX_CrmPy_Empresa creado';
END
GO

/* ========== SEED PRODUCTOS VORDIA ========== */
SET QUOTED_IDENTIFIER ON;
DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA');

IF @EmpresaId IS NOT NULL AND OBJECT_ID(N'dbo.CrmProductos', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'HQ')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'HQ', N'VORDIA HQ',
            N'HQ viene de Headquarters: sede central, cuartel general o centro de mando.',
            N'El sistema interno de administración y dirección de VORDIA.',
            N'Clientes, CRM, ingresos, egresos, CxC, CxP, contratos, productos, proyectos, tareas, reportes, automatizaciones, roadmap y toma de decisiones.',
            N'Es el lugar desde donde se controla y dirige toda la empresa VORDIA.',
            N'Activo', N'P0', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'HABITAT')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'HABITAT', N'VORDIA HABITAT',
            N'Habitat significa entorno o lugar donde vive una persona o una comunidad.',
            N'Plataforma para administración de condominios, edificios y comunidades residenciales.',
            N'Unidades, propietarios, residentes, expensas, cobranzas, proveedores, mantenimiento, comunicaciones y administración general.',
            N'No se limita a condominios; crece hacia edificios, complejos, barrios cerrados y otras comunidades.',
            N'Activo', N'P0', 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'STAY')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'STAY', N'VORDIA STAY',
            N'Stay significa estadía, permanencia o alojamiento.',
            N'Plataforma para hoteles, hostels y otros tipos de alojamiento.',
            N'Habitaciones, reservas, huéspedes, disponibilidad, tarifas, check-in, check-out, pagos y reportes.',
            N'Corto, internacional y amplio para distintos modelos de hospedaje.',
            N'En desarrollo', N'P0', 3);

    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'DINE')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'DINE', N'VORDIA DINE',
            N'Dine viene de to dine: comer o cenar, con connotación gastronómica y elegante.',
            N'Plataforma para restaurantes, cafeterías, bares y negocios gastronómicos.',
            N'Mesas, pedidos, cocina, caja, inventario, clientes, proveedores, delivery y reportes.',
            N'Representa la experiencia gastronómica completa con imagen premium.',
            N'Planificado', N'P1', 4);

    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'RETAIL')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'RETAIL', N'VORDIA RETAIL',
            N'Retail es el término internacional para comercio minorista (venta al consumidor).',
            N'Plataforma para comercios: boutiques, librerías, ropa, zapaterías, cosmética y más.',
            N'Productos, inventario, ventas, caja, clientes, proveedores y reportes.',
            N'Una sola plataforma adaptable a muchos sectores comerciales.',
            N'Planificado', N'P1', 5);

    IF NOT EXISTS (SELECT 1 FROM dbo.CrmProductos WHERE EmpresaId = @EmpresaId AND Codigo = N'AGRO')
        INSERT INTO dbo.CrmProductos (EmpresaId, Codigo, Nombre, Significado, QueSera, QueIncluye, IdeaCentral, Estado, Prioridad, Orden)
        VALUES (@EmpresaId, N'AGRO', N'VORDIA AGRO',
            N'Agro: línea futura orientada al sector agropecuario y rural.',
            N'Plataforma para productores, cooperativas y operaciones del agro.',
            N'Lotes, campañas, costos, inventario, comercializadores y reportes (definición en roadmap).',
            N'Expande VORDIA más allá de urbano/servicios hacia el agro paraguayo.',
            N'Planificado', N'P2', 6);

    PRINT 'Seed productos VORDIA OK';
END
GO

/* Seed proyecto ejemplo */
SET QUOTED_IDENTIFIER ON;
DECLARE @Emp INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA');
IF @Emp IS NOT NULL AND OBJECT_ID(N'dbo.CrmProyectos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.CrmProyectos WHERE EmpresaId = @Emp)
BEGIN
    INSERT INTO dbo.CrmProyectos (EmpresaId, Titulo, Descripcion, ProductoCodigo, Estado, Prioridad, ResponsableNombre, AvancePct, FechaInicio)
    VALUES
    (@Emp, N'Operación diaria HQ', N'Mantener CRM, finanzas, capital, objetivos y tareas vivos para Eli/Nery.', N'HQ', N'En curso', N'P0', N'Eli Ortigoza', 60, CAST(GETDATE() AS DATE)),
    (@Emp, N'STAY MVP tarifas/pagos', N'Base operativa de tarifas, bloqueos y pagos para hostels.', N'STAY', N'En curso', N'P0', N'Nery Chavez', 35, CAST(GETDATE() AS DATE)),
    (@Emp, N'DINE discovery', N'Definir alcance MVP gastronómico con socios.', N'DINE', N'Planificado', N'P1', N'Eli Ortigoza', 0, NULL);
    PRINT 'Seed proyectos OK';
END
GO

PRINT '=== HQ Productos / Proveedores / Proyectos listo ===';
GO
