/*
  VORDIA · Actualización de base de datos — octubre 2026
  -------------------------------------------------------
  Crea lo que falte para Agro por módulos, Necesidades del Mercado (con bajas de correos),
  HQ (proyectos, productos, capital, objetivos con métricas, hoja de ruta, leads web),
  permisos granulares de usuarios, Stay, Fontana y VORDIA CONTA (estudios contables).

  - Se puede correr más de una vez: cada bloque verifica antes de crear o agregar.
  - No borra ni modifica datos existentes.
  - NO carga datos de demostración (animales, insumos, objetivos de ejemplo): solo estructura y
    configuración necesaria (catálogo de productos HQ, módulos de menú, fuentes del radar).

  Cómo correrlo en el servidor (después de un backup):
    sqlcmd -S localhost -U sa -P '<clave>' -d GESCO_DB -i 03_Actualizacion_Octubre_2026.sql
  Después correr 98_Verificar_Esquema_Completo.sql: no debería listar faltantes.
*/
USE GESCO_DB;
GO
SET NOCOUNT ON;
EXEC sp_set_session_context @key = N'vordia_sin_demo', @value = 1;
PRINT '=== Actualización VORDIA octubre 2026 — inicio ===';
GO

/* ======================================================================
   Fontana: finanzas y personal  (Create_Fontana_Finanzas_Personal.sql)
   ====================================================================== */
PRINT '--- Fontana: finanzas y personal ---';
GO
/*
  Fontana (Condominio Agus) — Finanzas + Personal
  Ejecutar en GESCO_DB (idempotente).
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.FontanaMovimientos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FontanaMovimientos (
        MovimientoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FontanaMovimientos PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        Tipo             NVARCHAR(20) NOT NULL,  -- ingreso | gasto
        Propiedad        NVARCHAR(50) NOT NULL,  -- Fontana 1 | Fontana 2 | Fontana 3
        Categoria        NVARCHAR(80) NOT NULL,
        Concepto         NVARCHAR(300) NOT NULL,
        Monto            DECIMAL(18,2) NOT NULL CONSTRAINT DF_FontanaMovimientos_Monto DEFAULT (0),
        Fecha            DATE NOT NULL,
        MetodoPago       NVARCHAR(50) NULL,
        Origen           NVARCHAR(120) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_FontanaMovimientos_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_FontanaMovimientos_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_FontanaMovimientos_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_FontanaMovimientos_Tipo CHECK (Tipo IN (N'ingreso', N'gasto'))
    );
    CREATE INDEX IX_FontanaMovimientos_EmpresaFecha
        ON dbo.FontanaMovimientos (EmpresaId, Activo, Fecha DESC);
    CREATE INDEX IX_FontanaMovimientos_Propiedad
        ON dbo.FontanaMovimientos (EmpresaId, Propiedad, Fecha DESC);
    PRINT 'Tabla FontanaMovimientos creada';
END
ELSE
    PRINT 'Tabla FontanaMovimientos ya existe';
GO

IF OBJECT_ID(N'dbo.FontanaPersonal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FontanaPersonal (
        PersonalId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FontanaPersonal PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        Nombre           NVARCHAR(200) NOT NULL,
        Funcion          NVARCHAR(150) NOT NULL,
        Telefono         NVARCHAR(50) NULL,
        PropiedadesJson  NVARCHAR(500) NOT NULL CONSTRAINT DF_FontanaPersonal_Props DEFAULT (N'[]'),
        Salario          DECIMAL(18,2) NOT NULL CONSTRAINT DF_FontanaPersonal_Salario DEFAULT (0),
        FechaPago        DATE NOT NULL,
        EstadoPago       NVARCHAR(20) NOT NULL CONSTRAINT DF_FontanaPersonal_Estado DEFAULT (N'PENDIENTE'),
        Activo           BIT NOT NULL CONSTRAINT DF_FontanaPersonal_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_FontanaPersonal_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_FontanaPersonal_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_FontanaPersonal_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_FontanaPersonal_Estado CHECK (EstadoPago IN (N'PAGADO', N'PENDIENTE'))
    );
    CREATE INDEX IX_FontanaPersonal_Empresa
        ON dbo.FontanaPersonal (EmpresaId, Activo, EstadoPago);
    PRINT 'Tabla FontanaPersonal creada';
END
ELSE
    PRINT 'Tabla FontanaPersonal ya existe';
GO

/* ======================================================================
   VORDIA STAY: huéspedes, tarifas, pagos y bloqueos  (Create_Vordia_Stay.sql)
   ====================================================================== */
PRINT '--- VORDIA STAY: huéspedes, tarifas, pagos y bloqueos ---';
GO
/*
  VORDIA STAY — esquema base (idempotente)
  Extiende el Core (Empresa / Persona) y reutiliza Habitaciones + Reservas existentes.
  NO modifica tablas de Habitat (UnidadesCondominio, ContratosInquilinos, etc.).
  NO altera columnas de Habitaciones/Reservas.

  Ejecutar en GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------- StayTarifas ---------- */
IF OBJECT_ID(N'dbo.StayTarifas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StayTarifas (
        StayTarifaId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StayTarifas PRIMARY KEY,
        EmpresaId          INT NOT NULL,
        Nombre             NVARCHAR(120) NOT NULL,
        TipoHabitacion     NVARCHAR(50) NOT NULL,
        HabitacionId       INT NULL,
        PrecioPorNocheGs   DECIMAL(18,2) NOT NULL CONSTRAINT DF_StayTarifas_Noche DEFAULT (0),
        PrecioPorSemanaGs  DECIMAL(18,2) NULL,
        PrecioPorMesGs     DECIMAL(18,2) NULL,
        VigenciaDesde      DATE NULL,
        VigenciaHasta      DATE NULL,
        Activo             BIT NOT NULL CONSTRAINT DF_StayTarifas_Activo DEFAULT (1),
        Notas              NVARCHAR(300) NULL,
        FechaCreacion      DATETIME2 NOT NULL CONSTRAINT DF_StayTarifas_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StayTarifas_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_StayTarifas_Habitacion FOREIGN KEY (HabitacionId) REFERENCES dbo.Habitaciones(HabitacionId)
    );
    CREATE INDEX IX_StayTarifas_EmpresaTipo ON dbo.StayTarifas (EmpresaId, Activo, TipoHabitacion);
    PRINT 'Tabla StayTarifas creada';
END
ELSE PRINT 'Tabla StayTarifas ya existe';
GO

/* ---------- StayPagos ---------- */
IF OBJECT_ID(N'dbo.StayPagos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StayPagos (
        StayPagoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StayPagos PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        ReservaId        INT NOT NULL,
        MontoGs          DECIMAL(18,2) NOT NULL CONSTRAINT DF_StayPagos_Monto DEFAULT (0),
        MetodoPago       NVARCHAR(50) NOT NULL,
        FechaPago        DATETIME2 NOT NULL CONSTRAINT DF_StayPagos_Fecha DEFAULT (SYSUTCDATETIME()),
        Concepto         NVARCHAR(120) NOT NULL,
        NroComprobante   NVARCHAR(80) NULL,
        UsuarioId        INT NULL,
        Notas            NVARCHAR(400) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_StayPagos_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_StayPagos_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StayPagos_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_StayPagos_Reserva FOREIGN KEY (ReservaId) REFERENCES dbo.Reservas(ReservaId)
    );
    CREATE INDEX IX_StayPagos_Reserva ON dbo.StayPagos (EmpresaId, ReservaId, Activo, FechaPago DESC);
    PRINT 'Tabla StayPagos creada';
END
ELSE PRINT 'Tabla StayPagos ya existe';
GO

/* ---------- StayBloqueos ---------- */
IF OBJECT_ID(N'dbo.StayBloqueos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StayBloqueos (
        StayBloqueoId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StayBloqueos PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        HabitacionId     INT NOT NULL,
        FechaDesde       DATETIME2 NOT NULL,
        FechaHasta       DATETIME2 NOT NULL,
        Motivo           NVARCHAR(40) NOT NULL,
        Notas            NVARCHAR(400) NULL,
        UsuarioId        INT NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_StayBloqueos_Activo DEFAULT (1),
        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_StayBloqueos_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StayBloqueos_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_StayBloqueos_Habitacion FOREIGN KEY (HabitacionId) REFERENCES dbo.Habitaciones(HabitacionId),
        CONSTRAINT CK_StayBloqueos_Rango CHECK (FechaHasta > FechaDesde)
    );
    CREATE INDEX IX_StayBloqueos_HabFecha ON dbo.StayBloqueos (EmpresaId, HabitacionId, Activo, FechaDesde, FechaHasta);
    PRINT 'Tabla StayBloqueos creada';
END
ELSE PRINT 'Tabla StayBloqueos ya existe';
GO

/* ---------- StayHuespedes ---------- */
IF OBJECT_ID(N'dbo.StayHuespedes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StayHuespedes (
        StayHuespedId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StayHuespedes PRIMARY KEY,
        EmpresaId          INT NOT NULL,
        PersonaId          INT NOT NULL,
        Nacionalidad       NVARCHAR(80) NULL,
        FechaNacimiento    DATE NULL,
        TipoDocumento      NVARCHAR(40) NULL,
        PaisResidencia     NVARCHAR(120) NULL,
        TelefonoEmergencia NVARCHAR(40) NULL,
        Preferencias       NVARCHAR(400) NULL,
        Notas              NVARCHAR(400) NULL,
        Activo             BIT NOT NULL CONSTRAINT DF_StayHuespedes_Activo DEFAULT (1),
        FechaCreacion      DATETIME2 NOT NULL CONSTRAINT DF_StayHuespedes_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_StayHuespedes_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StayHuespedes_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_StayHuespedes_Persona FOREIGN KEY (PersonaId) REFERENCES dbo.Personas(PersonaId),
        CONSTRAINT UQ_StayHuespedes_EmpresaPersona UNIQUE (EmpresaId, PersonaId)
    );
    CREATE INDEX IX_StayHuespedes_Empresa ON dbo.StayHuespedes (EmpresaId, Activo);
    PRINT 'Tabla StayHuespedes creada';
END
ELSE PRINT 'Tabla StayHuespedes ya existe';
GO

/* ---------- Módulo menú STAY (licencias multi-producto) ---------- */
DECLARE @Rubros TABLE (Rubro NVARCHAR(50));
INSERT INTO @Rubros (Rubro)
SELECT DISTINCT Rubro FROM dbo.Empresas WHERE Rubro IS NOT NULL AND LTRIM(RTRIM(Rubro)) <> N''
UNION SELECT N'CONDOMINIO'
UNION SELECT N'HOSTEL'
UNION SELECT N'VORDIA';

DECLARE @R NVARCHAR(50);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Rubro FROM @Rubros;
OPEN cur;
FETCH NEXT FROM cur INTO @R;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado = @R AND RutaFrontend = N'stay')
        INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
        VALUES (N'Vordia Stay', @R, N'Bed', N'stay');
    FETCH NEXT FROM cur INTO @R;
END
CLOSE cur; DEALLOCATE cur;
PRINT 'Módulo stay registrado en catálogo Modulos (por rubros existentes).';
GO

PRINT '=== VORDIA STAY listo ===';
SELECT 'StayTarifas' AS Tabla, COUNT(*) AS Filas FROM dbo.StayTarifas
UNION ALL SELECT 'StayPagos', COUNT(*) FROM dbo.StayPagos
UNION ALL SELECT 'StayBloqueos', COUNT(*) FROM dbo.StayBloqueos
UNION ALL SELECT 'StayHuespedes', COUNT(*) FROM dbo.StayHuespedes;
GO

/* ======================================================================
   HQ: hoja de ruta  (Create_Crm_Roadmap.sql)
   ====================================================================== */
PRINT '--- HQ: hoja de ruta ---';
GO
/*
  CRM Vordia HQ — Roadmap: Ideas de negocio y Alianzas
  Ejecutar en GESCO_DB (idempotente).
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.CrmRoadmap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmRoadmap (
        RoadmapId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmRoadmap PRIMARY KEY,
        EmpresaId            INT NOT NULL,
        Tipo                 NVARCHAR(30) NOT NULL,  -- Idea | Alianza
        Titulo               NVARCHAR(200) NOT NULL,
        Descripcion          NVARCHAR(MAX) NULL,
        Estado               NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmRoadmap_Estado DEFAULT (N'Borrador'),
        Prioridad            NVARCHAR(10) NOT NULL CONSTRAINT DF_CrmRoadmap_Prioridad DEFAULT (N'P2'),
        ResponsableUsuarioId INT NULL,
        ResponsableNombre    NVARCHAR(150) NULL,
        FechaRegistro        DATE NOT NULL CONSTRAINT DF_CrmRoadmap_FechaReg DEFAULT (CAST(SYSUTCDATETIME() AS DATE)),
        FechaMeta            DATE NULL,
        ProximoPaso          NVARCHAR(500) NULL,
        FechaProximoPaso     DATE NULL,
        ConvertidoA          NVARCHAR(30) NULL,  -- Producto | CRM | Proyecto | NULL
        Notas                NVARCHAR(MAX) NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_CrmRoadmap_Activo DEFAULT (1),
        FechaCreacion        DATETIME2 NOT NULL CONSTRAINT DF_CrmRoadmap_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion   DATETIME2 NULL,
        CONSTRAINT FK_CrmRoadmap_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmRoadmap_Tipo CHECK (Tipo IN (N'Idea', N'Alianza')),
        CONSTRAINT CK_CrmRoadmap_Estado CHECK (Estado IN (
            N'Borrador', N'En evaluacion', N'Priorizado', N'En curso', N'Convertido', N'Descartado'
        )),
        CONSTRAINT CK_CrmRoadmap_Prioridad CHECK (Prioridad IN (N'P0', N'P1', N'P2')),
        CONSTRAINT CK_CrmRoadmap_ConvertidoA CHECK (
            ConvertidoA IS NULL OR ConvertidoA IN (N'Producto', N'CRM', N'Proyecto')
        )
    );
    CREATE INDEX IX_CrmRoadmap_Empresa ON dbo.CrmRoadmap (EmpresaId, Activo, Tipo, Estado);
    PRINT 'Tabla CrmRoadmap creada';
END
GO

IF OBJECT_ID(N'dbo.CrmRoadmapHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmRoadmapHistorial (
        HistorialId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmRoadmapHistorial PRIMARY KEY,
        RoadmapId       INT NOT NULL,
        EmpresaId       INT NOT NULL,
        Tipo            NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmRoadmapHist_Tipo DEFAULT (N'nota'),
        Titulo          NVARCHAR(200) NOT NULL,
        Detalle         NVARCHAR(MAX) NULL,
        EstadoAnterior  NVARCHAR(30) NULL,
        EstadoNuevo     NVARCHAR(30) NULL,
        UsuarioId       INT NULL,
        UsuarioNombre   NVARCHAR(150) NULL,
        Fecha           DATETIME2 NOT NULL CONSTRAINT DF_CrmRoadmapHist_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmRoadmapHist_Item FOREIGN KEY (RoadmapId) REFERENCES dbo.CrmRoadmap(RoadmapId),
        CONSTRAINT FK_CrmRoadmapHist_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId)
    );
    CREATE INDEX IX_CrmRoadmapHist_Item ON dbo.CrmRoadmapHistorial (RoadmapId, Fecha DESC);
    PRINT 'Tabla CrmRoadmapHistorial creada';
END
GO

DECLARE @EmpresaId INT;
SELECT @EmpresaId = EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' AND RazonSocial = N'Vordia';
IF @EmpresaId IS NULL BEGIN PRINT 'ERROR: Empresa Vordia no encontrada'; RETURN; END

DECLARE @EliId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py');
DECLARE @NeryId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py');

IF NOT EXISTS (SELECT 1 FROM dbo.CrmRoadmap WHERE EmpresaId = @EmpresaId AND Titulo = N'Alianza con desarrolladoras inmobiliarias')
BEGIN
    INSERT INTO dbo.CrmRoadmap (
        EmpresaId, Tipo, Titulo, Descripcion, Estado, Prioridad,
        ResponsableUsuarioId, ResponsableNombre, FechaRegistro, FechaMeta,
        ProximoPaso, FechaProximoPaso, Notas
    )
    VALUES (
        @EmpresaId, N'Alianza',
        N'Alianza con desarrolladoras inmobiliarias',
        N'Proponer Vordia Condominios / Fontana como stack de administración a desarrolladoras que entregan edificios.',
        N'En evaluacion', N'P1',
        @EliId, N'Eli Ortigoza',
        CAST(SYSUTCDATETIME() AS DATE),
        DATEADD(MONTH, 2, CAST(SYSUTCDATETIME() AS DATE)),
        N'Armar one-pager y lista de 5 desarrolladoras objetivo',
        DATEADD(DAY, 14, CAST(SYSUTCDATETIME() AS DATE)),
        N'Seed inicial HQ — editable por Eli/Nery'
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.CrmRoadmap WHERE EmpresaId = @EmpresaId AND Titulo = N'Módulo Hostel como producto SaaS')
BEGIN
    INSERT INTO dbo.CrmRoadmap (
        EmpresaId, Tipo, Titulo, Descripcion, Estado, Prioridad,
        ResponsableUsuarioId, ResponsableNombre, FechaRegistro, FechaMeta,
        ProximoPaso, FechaProximoPaso, Notas
    )
    VALUES (
        @EmpresaId, N'Idea',
        N'Módulo Hostel como producto SaaS',
        N'Empaquetar la lógica Fontana/Hostel para venderla a otros hospedajes independientes.',
        N'Borrador', N'P2',
        @NeryId, N'Nery Chavez',
        CAST(SYSUTCDATETIME() AS DATE),
        DATEADD(MONTH, 3, CAST(SYSUTCDATETIME() AS DATE)),
        N'Definir alcance MVP y precio estimado mensual',
        DATEADD(DAY, 21, CAST(SYSUTCDATETIME() AS DATE)),
        N'Seed inicial HQ — idea de producto'
    );
END

-- Módulo menú (opcional licencia)
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado=N'VORDIA' AND RutaFrontend=N'vordia_roadmap')
BEGIN
    INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
    VALUES (N'Roadmap', N'VORDIA', N'Map', N'vordia_roadmap');
END

DECLARE @ModRoadmap INT = (SELECT TOP 1 ModuloId FROM dbo.Modulos WHERE RubroAsociado=N'VORDIA' AND RutaFrontend=N'vordia_roadmap');
IF @ModRoadmap IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.LicenciasModulos WHERE EmpresaId=@EmpresaId AND ModuloId=@ModRoadmap)
        INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
        VALUES (@EmpresaId, @ModRoadmap, 1, 0, NULL);

    IF @EliId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioModulos WHERE UsuarioId=@EliId AND ModuloId=@ModRoadmap)
        INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId) VALUES (@EliId, @ModRoadmap);
    IF @NeryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioModulos WHERE UsuarioId=@NeryId AND ModuloId=@ModRoadmap)
        INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId) VALUES (@NeryId, @ModRoadmap);
END

PRINT 'Roadmap Vordia: listo.';
GO

/* ======================================================================
   HQ: productos, proyectos y proveedores  (Create_Crm_Hq_Productos_Proyectos_Proveedores.sql)
   ====================================================================== */
PRINT '--- HQ: productos, proyectos y proveedores ---';
GO
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

/* ======================================================================
   HQ: capital, empleados y objetivos  (Create_Crm_Hq_Capital_Objetivos.sql)
   ====================================================================== */
PRINT '--- HQ: capital, empleados y objetivos ---';
GO
/*

  Vordia HQ — Capital empresa, Empleados (finanzas) y Objetivos (CRM/Tareas)

  Idempotente. No toca Habitat/Stay/Fontana.

  Compatible con sqlcmd (requiere QUOTED_IDENTIFIER ON por índices filtrados).

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



IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmCapitalMovimientos (

        CapitalId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmCapitalMovimientos PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Tipo             NVARCHAR(30) NOT NULL, -- Aporte | Retiro | Ajuste | Valuacion

        MontoGs          DECIMAL(18,2) NOT NULL,

        Fecha            DATE NOT NULL,

        Concepto         NVARCHAR(300) NOT NULL,

        SocioId          INT NULL,

        UsuarioNombre    NVARCHAR(150) NULL,

        Notas            NVARCHAR(500) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmCapital_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmCapital_Creacion DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmCapital_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmCapital_Tipo CHECK (Tipo IN (N'Aporte', N'Retiro', N'Ajuste', N'Valuacion'))

    );

    PRINT 'CrmCapitalMovimientos creada';

END

ELSE PRINT 'CrmCapitalMovimientos ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmCapital_EmpresaFecha' AND object_id = OBJECT_ID(N'dbo.CrmCapitalMovimientos'))

BEGIN

    CREATE INDEX IX_CrmCapital_EmpresaFecha ON dbo.CrmCapitalMovimientos (EmpresaId, Activo, Fecha DESC);

    PRINT 'IX_CrmCapital_EmpresaFecha creado';

END

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmEmpleadosHq (

        EmpleadoHqId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmEmpleadosHq PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Nombre           NVARCHAR(150) NOT NULL,

        RolCargo         NVARCHAR(120) NOT NULL,

        Email            NVARCHAR(150) NULL,

        Telefono         NVARCHAR(50) NULL,

        SalarioMensualGs DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmEmp_Salario DEFAULT (0),

        FechaIngreso     DATE NULL,

        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmEmp_Estado DEFAULT (N'Activo'),

        Notas            NVARCHAR(500) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmEmp_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmEmp_Creacion DEFAULT (SYSUTCDATETIME()),

        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmEmp_Update DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmEmp_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmEmp_Estado CHECK (Estado IN (N'Activo', N'Inactivo', N'Licencia'))

    );

    PRINT 'CrmEmpleadosHq creada';

END

ELSE PRINT 'CrmEmpleadosHq ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmEmp_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmEmpleadosHq'))

BEGIN

    CREATE INDEX IX_CrmEmp_Empresa ON dbo.CrmEmpleadosHq (EmpresaId, Activo, Estado);

    PRINT 'IX_CrmEmp_Empresa creado';

END

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmObjetivos (

        ObjetivoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmObjetivos PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Ambito           NVARCHAR(20) NOT NULL, -- Empresa | Crm | Tarea

        Titulo           NVARCHAR(200) NOT NULL,

        Descripcion      NVARCHAR(MAX) NULL,

        NotasReunion     NVARCHAR(MAX) NULL, -- para compartir con socia

        Estado           NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmObj_Estado DEFAULT (N'Pendiente'),

        Prioridad        NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmObj_Pri DEFAULT (N'Media'),

        FechaObjetivo    DATE NULL,

        ClienteId        INT NULL,

        TareaId          INT NULL,

        CreadoPor        NVARCHAR(150) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmObj_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmObj_Creacion DEFAULT (SYSUTCDATETIME()),

        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmObj_Update DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmObj_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmObj_Ambito CHECK (Ambito IN (N'Empresa', N'Crm', N'Tarea')),

        CONSTRAINT CK_CrmObj_Estado CHECK (Estado IN (N'Pendiente', N'En curso', N'Logrado', N'Pausado', N'Cancelado')),

        CONSTRAINT CK_CrmObj_Pri CHECK (Prioridad IN (N'Baja', N'Media', N'Alta'))

    );

    PRINT 'CrmObjetivos creada';

END

ELSE PRINT 'CrmObjetivos ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmObj_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmObjetivos'))

BEGIN

    CREATE INDEX IX_CrmObj_Empresa ON dbo.CrmObjetivos (EmpresaId, Activo, Ambito, Estado);

    PRINT 'IX_CrmObj_Empresa creado';

END

GO



-- Índice filtrado: requiere QUOTED_IDENTIFIER ON (sqlcmd suele venir OFF)

SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmObj_Tarea' AND object_id = OBJECT_ID(N'dbo.CrmObjetivos'))

BEGIN

    CREATE INDEX IX_CrmObj_Tarea ON dbo.CrmObjetivos (TareaId) WHERE TareaId IS NOT NULL;

    PRINT 'IX_CrmObj_Tarea creado';

END

GO



-- Seed objetivos generales si empresa Vordia existe y no hay ninguno

SET QUOTED_IDENTIFIER ON;

DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA');

IF @EmpresaId IS NOT NULL

   AND ISNULL(CAST(SESSION_CONTEXT(N'vordia_sin_demo') AS INT), 0) = 0

   AND OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM dbo.CrmObjetivos WHERE EmpresaId = @EmpresaId)

BEGIN

    INSERT INTO dbo.CrmObjetivos (EmpresaId, Ambito, Titulo, Descripcion, NotasReunion, Estado, Prioridad, FechaObjetivo, CreadoPor)

    VALUES

    (@EmpresaId, N'Empresa', N'Capital operativo inicial documentado', N'Registrar aportes de capital y mantener el libro al día.', N'Reunión socios: validar montos y %.', N'En curso', N'Alta', DATEADD(MONTH, 1, CAST(GETDATE() AS DATE)), N'Sistema'),

    (@EmpresaId, N'Crm', N'Cerrar 2 clientes Habitat/Stay este trimestre', N'Pipeline enfocado en condominios y alojamiento.', N'Hablar con socia: priorizar Fontana-like vs Stay.', N'Pendiente', N'Alta', DATEADD(MONTH, 3, CAST(GETDATE() AS DATE)), N'Sistema'),

    (@EmpresaId, N'Empresa', N'Equipo HQ con roles claros', N'Cargar empleados y costos mensuales en Finanzas.', NULL, N'Pendiente', N'Media', DATEADD(MONTH, 2, CAST(GETDATE() AS DATE)), N'Sistema');

    PRINT 'Objetivos seed insertados';

END

ELSE PRINT 'Seed objetivos omitido (ya hay datos o sin empresa VORDIA)';

GO



PRINT '=== HQ Capital / Empleados / Objetivos listo ===';

GO

/* ======================================================================
   Usuarios: permisos granulares  (Add_Usuario_PermisosJson.sql)
   ====================================================================== */
PRINT '--- Usuarios: permisos granulares ---';
GO
-- Permisos granulares por usuario (JSON array de ids, ej: ["fontana_ver","condo_registrar_pago"])
IF COL_LENGTH('dbo.Usuarios', 'PermisosJson') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD PermisosJson NVARCHAR(MAX) NULL;
    PRINT 'Columna Usuarios.PermisosJson agregada.';
END
ELSE
    PRINT 'Columna Usuarios.PermisosJson ya existe.';
GO

/* ======================================================================
   HQ: origen de leads web  (Add_Crm_Leads_Web.sql)
   ====================================================================== */
PRINT '--- HQ: origen de leads web ---';
GO
/*
  CRM Vordia — Origen de leads (formulario web pública)
  Base: GESCO_DB
  Idempotente.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.CrmClientes', N'U') IS NULL
BEGIN
    PRINT 'ERROR: CrmClientes no existe. Ejecutá primero Create_Crm_Vordia.sql';
    RETURN;
END

IF COL_LENGTH('dbo.CrmClientes', 'Origen') IS NULL
BEGIN
    ALTER TABLE dbo.CrmClientes ADD Origen NVARCHAR(40) NULL;
    PRINT 'Columna Origen agregada';
END
GO

IF COL_LENGTH('dbo.CrmClientes', 'OrigenDetalle') IS NULL
BEGIN
    ALTER TABLE dbo.CrmClientes ADD OrigenDetalle NVARCHAR(120) NULL;
    PRINT 'Columna OrigenDetalle agregada';
END
GO

IF COL_LENGTH('dbo.CrmClientes', 'MensajeInicial') IS NULL
BEGIN
    ALTER TABLE dbo.CrmClientes ADD MensajeInicial NVARCHAR(MAX) NULL;
    PRINT 'Columna MensajeInicial agregada';
END
GO

-- Los clientes cargados a mano antes de este cambio quedan como origen manual
UPDATE dbo.CrmClientes SET Origen = N'manual' WHERE Origen IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CrmClientes_Origen' AND object_id = OBJECT_ID('dbo.CrmClientes'))
BEGIN
    CREATE INDEX IX_CrmClientes_Origen ON dbo.CrmClientes (EmpresaId, Origen, FechaCreacion DESC);
    PRINT 'Indice IX_CrmClientes_Origen creado';
END
GO

SELECT Origen, COUNT(*) AS Cantidad
FROM dbo.CrmClientes
GROUP BY Origen
ORDER BY Origen;
GO

/* ======================================================================
   HQ: métricas de objetivos  (Add_Crm_Objetivos_Metricas.sql)
   ====================================================================== */
PRINT '--- HQ: métricas de objetivos ---';
GO
/*
  VORDIA HQ — Métricas de avance para Objetivos
  =================================================================
  Agrega a dbo.CrmObjetivos las columnas que el dashboard ejecutivo
  necesita para dibujar el progreso real ("7 de 10", 70%):

    MetaValor    -> el objetivo numérico (ej. 10 clientes nuevos)
    ValorActual  -> lo alcanzado hasta hoy (ej. 7)
    AvancePct    -> avance manual 0..100, para objetivos sin número

  Regla de cálculo en el dashboard:
    - Si MetaValor > 0        -> porcentaje = ValorActual / MetaValor
    - Si no, y hay AvancePct  -> se usa AvancePct
    - Si no                   -> 100% si está Logrado, 0% en otro caso

  Script idempotente: se puede correr varias veces sin efecto adverso.
  Requiere haber ejecutado antes Create_Crm_Hq_Capital_Objetivos.sql.

  Uso: SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NULL
BEGIN
    RAISERROR(N'No existe dbo.CrmObjetivos. Ejecutar antes Create_Crm_Hq_Capital_Objetivos.sql', 16, 1);
END
GO

-------------------------------------------------------------
-- 1) MetaValor
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'MetaValor') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD MetaValor DECIMAL(18, 2) NULL;
    PRINT 'CrmObjetivos.MetaValor agregada';
END
ELSE PRINT 'CrmObjetivos.MetaValor ya existe';
GO

-------------------------------------------------------------
-- 2) ValorActual
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'ValorActual') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD ValorActual DECIMAL(18, 2) NULL;
    PRINT 'CrmObjetivos.ValorActual agregada';
END
ELSE PRINT 'CrmObjetivos.ValorActual ya existe';
GO

-------------------------------------------------------------
-- 3) AvancePct
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'AvancePct') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD AvancePct INT NULL;
    PRINT 'CrmObjetivos.AvancePct agregada';
END
ELSE PRINT 'CrmObjetivos.AvancePct ya existe';
GO

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'AvancePct') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmObj_AvancePct')
BEGIN
    ALTER TABLE dbo.CrmObjetivos
        ADD CONSTRAINT CK_CrmObj_AvancePct CHECK (AvancePct IS NULL OR (AvancePct >= 0 AND AvancePct <= 100));
    PRINT 'CK_CrmObj_AvancePct creado';
END
ELSE PRINT 'CK_CrmObj_AvancePct ya existe u omitido';
GO

-------------------------------------------------------------
-- 4) Verificación
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
BEGIN
    SELECT
        Columna     = c.name,
        Tipo        = t.name,
        Permite_Null = c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CrmObjetivos')
      AND c.name IN (N'MetaValor', N'ValorActual', N'AvancePct')
    ORDER BY c.name;
END
GO

PRINT '=== Métricas de Objetivos listas ===';
GO

/* ======================================================================
   VORDIA AGRO: base  (Create_Vordia_Agro.sql)
   ====================================================================== */
PRINT '--- VORDIA AGRO: base ---';
GO
/*
  VORDIA AGRO — Plataforma inteligente de confinamiento agropecuario (prototipo)
  Cliente demo: Agrofield
  Idempotente + QUOTED_IDENTIFIER ON (sqlcmd).
  NO toca Habitat / Stay / Fontana / HQ CRM existente.
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

/* ========== ESTABLECIMIENTOS ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroEstablecimientos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroEstablecimientos (
        EstablecimientoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroEst PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Nombre            NVARCHAR(150) NOT NULL,
        ClienteNombre     NVARCHAR(150) NULL, -- ej. Agrofield
        Ubicacion         NVARCHAR(200) NULL,
        Hectareas         DECIMAL(18,2) NULL,
        Notas             NVARCHAR(500) NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_AgroEst_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroEst_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroEst_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId)
    );
    PRINT 'AgroEstablecimientos creada';
END
ELSE PRINT 'AgroEstablecimientos ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroEstablecimientos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroEst_Empresa' AND object_id=OBJECT_ID(N'dbo.AgroEstablecimientos'))
    CREATE INDEX IX_AgroEst_Empresa ON dbo.AgroEstablecimientos (EmpresaId, Activo);
GO

/* ========== LOTES / CORRALES ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroLotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroLotes (
        LoteId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroLote PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        Codigo            NVARCHAR(40) NOT NULL,
        Nombre            NVARCHAR(120) NOT NULL,
        Tipo              NVARCHAR(40) NOT NULL CONSTRAINT DF_AgroLote_Tipo DEFAULT (N'Corral'), -- Corral | Potrero | Manga
        Capacidad         INT NOT NULL CONSTRAINT DF_AgroLote_Cap DEFAULT (0),
        CamaraCodigo      NVARCHAR(40) NULL, -- futuro hardware / IA
        Activo            BIT NOT NULL CONSTRAINT DF_AgroLote_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroLote_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroLote_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroLote_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId)
    );
    PRINT 'AgroLotes creada';
END
ELSE PRINT 'AgroLotes ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroLotes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroLote_Est' AND object_id=OBJECT_ID(N'dbo.AgroLotes'))
    CREATE INDEX IX_AgroLote_Est ON dbo.AgroLotes (EmpresaId, EstablecimientoId, Activo);
GO

/* ========== ZONAS (cerca virtual — prototipo) ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroZonas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroZonas (
        ZonaId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroZona PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        Nombre            NVARCHAR(120) NOT NULL,
        Tipo              NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroZona_Tipo DEFAULT (N'Permitida'), -- Permitida | Prohibida
        LatCentro         DECIMAL(10,6) NULL,
        LngCentro         DECIMAL(10,6) NULL,
        RadioMetros       INT NOT NULL CONSTRAINT DF_AgroZona_Radio DEFAULT (200),
        Activo            BIT NOT NULL CONSTRAINT DF_AgroZona_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroZona_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroZona_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroZona_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId),
        CONSTRAINT CK_AgroZona_Tipo CHECK (Tipo IN (N'Permitida', N'Prohibida'))
    );
    PRINT 'AgroZonas creada';
END
ELSE PRINT 'AgroZonas ya existe';
GO

/* ========== ANIMALES ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroAnimales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroAnimales (
        AnimalId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroAnimal PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        LoteId            INT NULL,
        CodigoInterno     NVARCHAR(40) NOT NULL,
        RfidTag           NVARCHAR(60) NULL,
        CollarId          NVARCHAR(60) NULL, -- futuro hardware
        Categoria         NVARCHAR(40) NOT NULL CONSTRAINT DF_AgroAn_Cat DEFAULT (N'Novillo'),
        Sexo              NVARCHAR(10) NULL,
        FechaIngreso      DATE NULL,
        PesoIngresoKg     DECIMAL(10,2) NULL,
        PesoActualKg      DECIMAL(10,2) NULL,
        Estado            NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroAn_Estado DEFAULT (N'Activo'),
        UltimaLat         DECIMAL(10,6) NULL,
        UltimaLng         DECIMAL(10,6) NULL,
        UltimaUbicacionAt DATETIME2 NULL,
        BateriaCollarPct  INT NULL,
        Notas             NVARCHAR(500) NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_AgroAn_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroAn_Cre DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_AgroAn_Upd DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroAn_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroAn_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId),
        CONSTRAINT FK_AgroAn_Lote FOREIGN KEY (LoteId) REFERENCES dbo.AgroLotes(LoteId),
        CONSTRAINT CK_AgroAn_Estado CHECK (Estado IN (N'Activo', N'Vendido', N'Muerto', N'Tratamiento', N'FueraZona'))
    );
    PRINT 'AgroAnimales creada';
END
ELSE PRINT 'AgroAnimales ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroAnimales', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroAn_Est' AND object_id=OBJECT_ID(N'dbo.AgroAnimales'))
    CREATE INDEX IX_AgroAn_Est ON dbo.AgroAnimales (EmpresaId, EstablecimientoId, Activo, Estado);
GO

/* ========== PESAJES ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroPesajes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroPesajes (
        PesajeId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroPesaje PRIMARY KEY,
        EmpresaId    INT NOT NULL,
        AnimalId     INT NOT NULL,
        PesoKg       DECIMAL(10,2) NOT NULL,
        FechaPesaje  DATETIME2 NOT NULL CONSTRAINT DF_AgroPes_Fecha DEFAULT (SYSUTCDATETIME()),
        Origen       NVARCHAR(40) NOT NULL CONSTRAINT DF_AgroPes_Origen DEFAULT (N'Manual'), -- Manual | Balanza | RFID
        Operario     NVARCHAR(120) NULL,
        Notas        NVARCHAR(300) NULL,
        Activo       BIT NOT NULL CONSTRAINT DF_AgroPes_Activo DEFAULT (1),
        CONSTRAINT FK_AgroPes_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroPes_Animal FOREIGN KEY (AnimalId) REFERENCES dbo.AgroAnimales(AnimalId)
    );
    PRINT 'AgroPesajes creada';
END
ELSE PRINT 'AgroPesajes ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroPesajes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroPes_Animal' AND object_id=OBJECT_ID(N'dbo.AgroPesajes'))
    CREATE INDEX IX_AgroPes_Animal ON dbo.AgroPesajes (EmpresaId, AnimalId, Activo, FechaPesaje DESC);
GO

/* ========== RACIONES / ALIMENTACIÓN ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroRaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroRaciones (
        RacionId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroRacion PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        LoteId        INT NOT NULL,
        TipoAlimento  NVARCHAR(80) NOT NULL,
        CantidadKg    DECIMAL(12,2) NOT NULL,
        FechaEntrega  DATETIME2 NOT NULL CONSTRAINT DF_AgroRac_Fecha DEFAULT (SYSUTCDATETIME()),
        Operario      NVARCHAR(120) NULL,
        Vehiculo      NVARCHAR(80) NULL,
        CostoGs       DECIMAL(18,2) NULL,
        Notas         NVARCHAR(300) NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_AgroRac_Activo DEFAULT (1),
        CONSTRAINT FK_AgroRac_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroRac_Lote FOREIGN KEY (LoteId) REFERENCES dbo.AgroLotes(LoteId)
    );
    PRINT 'AgroRaciones creada';
END
ELSE PRINT 'AgroRaciones ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroRaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroRac_Lote' AND object_id=OBJECT_ID(N'dbo.AgroRaciones'))
    CREATE INDEX IX_AgroRac_Lote ON dbo.AgroRaciones (EmpresaId, LoteId, Activo, FechaEntrega DESC);
GO

/* ========== DISPOSITIVOS (futuro hardware) ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroDispositivos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroDispositivos (
        DispositivoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroDisp PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        Codigo        NVARCHAR(60) NOT NULL,
        Tipo          NVARCHAR(40) NOT NULL, -- Collar | RFID | Camara | Balanza | Gateway
        AnimalId      INT NULL,
        Estado        NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroDisp_Estado DEFAULT (N'Online'),
        BateriaPct    INT NULL,
        UltimoPingAt  DATETIME2 NULL,
        Notas         NVARCHAR(300) NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_AgroDisp_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_AgroDisp_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroDisp_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroDisp_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId),
        CONSTRAINT CK_AgroDisp_Tipo CHECK (Tipo IN (N'Collar', N'RFID', N'Camara', N'Balanza', N'Gateway'))
    );
    PRINT 'AgroDispositivos creada';
END
ELSE PRINT 'AgroDispositivos ya existe';
GO

/* ========== ALERTAS ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroAlertas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroAlertas (
        AlertaId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroAlerta PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        AnimalId      INT NULL,
        DispositivoId INT NULL,
        Tipo          NVARCHAR(40) NOT NULL, -- FueraZona | BateriaBaja | SinSenal | BajoPeso | Mortalidad | Camara
        Severidad     NVARCHAR(20) NOT NULL CONSTRAINT DF_AgroAl_Sev DEFAULT (N'Media'),
        Titulo        NVARCHAR(200) NOT NULL,
        Detalle       NVARCHAR(500) NULL,
        Resuelta      BIT NOT NULL CONSTRAINT DF_AgroAl_Res DEFAULT (0),
        FechaAlerta   DATETIME2 NOT NULL CONSTRAINT DF_AgroAl_Fecha DEFAULT (SYSUTCDATETIME()),
        Activo        BIT NOT NULL CONSTRAINT DF_AgroAl_Activo DEFAULT (1),
        CONSTRAINT FK_AgroAl_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroAl_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId)
    );
    PRINT 'AgroAlertas creada';
END
ELSE PRINT 'AgroAlertas ya existe';
GO

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroAlertas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroAl_Est' AND object_id=OBJECT_ID(N'dbo.AgroAlertas'))
    CREATE INDEX IX_AgroAl_Est ON dbo.AgroAlertas (EmpresaId, EstablecimientoId, Activo, Resuelta, FechaAlerta DESC);
GO

/* ========== SEED AGROFIELD ========== */
SET QUOTED_IDENTIFIER ON;
DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA');
IF @EmpresaId IS NULL
    SET @EmpresaId = (SELECT TOP 1 EmpresaId FROM dbo.Empresas ORDER BY EmpresaId);

-- Datos demo: se omiten cuando el despliegue de producción activa 'vordia_sin_demo'
IF ISNULL(CAST(SESSION_CONTEXT(N'vordia_sin_demo') AS INT), 0) = 0
   AND @EmpresaId IS NOT NULL AND OBJECT_ID(N'dbo.AgroEstablecimientos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.AgroEstablecimientos WHERE EmpresaId=@EmpresaId AND ClienteNombre=N'Agrofield')
BEGIN
    DECLARE @EstId INT, @L1 INT, @L2 INT, @L3 INT;
    DECLARE @A1 INT, @A2 INT, @A3 INT, @A4 INT, @A5 INT;

    INSERT INTO dbo.AgroEstablecimientos (EmpresaId, Nombre, ClienteNombre, Ubicacion, Hectareas, Notas)
    VALUES (@EmpresaId, N'Confinamiento Agrofield Central', N'Agrofield', N'Chaco paraguayo — demo', 420,
            N'Prototipo Vordia AGRO. Hardware (collar/LoRa/cámaras) se conectará después.');
    SET @EstId = SCOPE_IDENTITY();

    INSERT INTO dbo.AgroLotes (EmpresaId, EstablecimientoId, Codigo, Nombre, Tipo, Capacidad, CamaraCodigo)
    VALUES
    (@EmpresaId, @EstId, N'C-01', N'Corral engorde 1', N'Corral', 120, N'CAM-01'),
    (@EmpresaId, @EstId, N'C-04', N'Corral engorde 4', N'Corral', 100, N'CAM-04'),
    (@EmpresaId, @EstId, N'P-07', N'Potrero rotativo 7', N'Potrero', 80, N'CAM-07');
    SET @L1 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-01');
    SET @L2 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-04');
    SET @L3 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'P-07');

    INSERT INTO dbo.AgroZonas (EmpresaId, EstablecimientoId, Nombre, Tipo, LatCentro, LngCentro, RadioMetros)
    VALUES
    (@EmpresaId, @EstId, N'Zona engorde permitida', N'Permitida', -23.442500, -58.443800, 350),
    (@EmpresaId, @EstId, N'Perímetro silo (prohibido)', N'Prohibida', -23.441200, -58.445100, 80);

    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, CollarId, Categoria, Sexo, FechaIngreso, PesoIngresoKg, PesoActualKg, Estado, UltimaLat, UltimaLng, UltimaUbicacionAt, BateriaCollarPct)
    VALUES
    (@EmpresaId, @EstId, @L1, N'1847', N'RFID-1847', N'COL-1847', N'Novillo', N'M', DATEADD(DAY,-42,CAST(GETDATE() AS DATE)), 350, 410, N'Activo', -23.442600, -58.443900, SYSUTCDATETIME(), 78),
    (@EmpresaId, @EstId, @L1, N'1848', N'RFID-1848', N'COL-1848', N'Novillo', N'M', DATEADD(DAY,-42,CAST(GETDATE() AS DATE)), 362, 401, N'Activo', -23.442700, -58.444000, SYSUTCDATETIME(), 65),
    (@EmpresaId, @EstId, @L2, N'1901', N'RFID-1901', N'COL-1901', N'Vaquilla', N'H', DATEADD(DAY,-30,CAST(GETDATE() AS DATE)), 310, 338, N'Activo', -23.443100, -58.444500, SYSUTCDATETIME(), 91),
    (@EmpresaId, @EstId, @L2, N'1902', N'RFID-1902', N'COL-1902', N'Novillo', N'M', DATEADD(DAY,-30,CAST(GETDATE() AS DATE)), 380, 392, N'Tratamiento', -23.443000, -58.444400, SYSUTCDATETIME(), 42),
    (@EmpresaId, @EstId, @L3, N'2005', N'RFID-2005', N'COL-2005', N'Novillo', N'M', DATEADD(DAY,-15,CAST(GETDATE() AS DATE)), 340, 348, N'FueraZona', -23.441100, -58.445200, SYSUTCDATETIME(), 22);

    SET @A1 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1847');
    SET @A2 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1848');
    SET @A3 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1901');
    SET @A4 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1902');
    SET @A5 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'2005');

    INSERT INTO dbo.AgroPesajes (EmpresaId, AnimalId, PesoKg, FechaPesaje, Origen, Operario)
    VALUES
    (@EmpresaId, @A1, 350, DATEADD(DAY,-42,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A1, 380, DATEADD(DAY,-21,SYSUTCDATETIME()), N'RFID', N'Operario demo'),
    (@EmpresaId, @A1, 410, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A2, 362, DATEADD(DAY,-42,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A2, 401, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A3, 310, DATEADD(DAY,-30,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A3, 338, DATEADD(DAY,-1,SYSUTCDATETIME()), N'RFID', N'Operario demo');

    INSERT INTO dbo.AgroRaciones (EmpresaId, LoteId, TipoAlimento, CantidadKg, FechaEntrega, Operario, Vehiculo, CostoGs)
    VALUES
    (@EmpresaId, @L1, N'TMR engorde', 2500, DATEADD(HOUR,-6,SYSUTCDATETIME()), N'Juan P.', N'Camión 07', 8750000),
    (@EmpresaId, @L2, N'TMR engorde', 1800, DATEADD(HOUR,-5,SYSUTCDATETIME()), N'Juan P.', N'Camión 07', 6300000),
    (@EmpresaId, @L3, N'Pastura + concentrado', 900, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Mario L.', N'Tractor 03', 2700000);

    INSERT INTO dbo.AgroDispositivos (EmpresaId, EstablecimientoId, Codigo, Tipo, AnimalId, Estado, BateriaPct, UltimoPingAt)
    VALUES
    (@EmpresaId, @EstId, N'COL-1847', N'Collar', @A1, N'Online', 78, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'COL-2005', N'Collar', @A5, N'Alerta', 22, DATEADD(MINUTE,-40,SYSUTCDATETIME())),
    (@EmpresaId, @EstId, N'CAM-07', N'Camara', NULL, N'Online', NULL, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'BAL-01', N'Balanza', NULL, N'Online', NULL, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'GW-LORA-01', N'Gateway', NULL, N'Online', NULL, SYSUTCDATETIME());

    INSERT INTO dbo.AgroAlertas (EmpresaId, EstablecimientoId, AnimalId, Tipo, Severidad, Titulo, Detalle, Resuelta)
    VALUES
    (@EmpresaId, @EstId, @A5, N'FueraZona', N'Alta', N'Animal 2005 fuera de zona permitida', N'Cerca del perímetro silo. Cámara sugerida: CAM-07', 0),
    (@EmpresaId, @EstId, @A5, N'BateriaBaja', N'Media', N'Collar COL-2005 batería 22%', N'Programar recarga / reemplazo', 0),
    (@EmpresaId, @EstId, @A4, N'BajoPeso', N'Media', N'Animal 1902 por debajo de GPD esperada', N'En tratamiento · revisar ración y salud', 0);

    -- Actualizar producto AGRO en CRM si existe
    IF OBJECT_ID(N'dbo.CrmProductos', N'U') IS NOT NULL
    BEGIN
        UPDATE dbo.CrmProductos
        SET Estado = N'En desarrollo',
            Prioridad = N'P0',
            QueSera = N'Plataforma inteligente para confinamientos ganaderos (cliente Agrofield). Animales, alimentación, conversión, dispositivos y alertas.',
            QueIncluye = N'Establecimientos, lotes/corrales, animales RFID/collar, pesajes, raciones, zonas virtuales, dispositivos IoT, alertas y dashboard gerencial.',
            IdeaCentral = N'Ecosistema físico + digital del confinamiento: primero datos y operación; después hardware (LoRa/GPS/cámaras).',
            FechaActualizacion = SYSUTCDATETIME()
        WHERE EmpresaId = @EmpresaId AND Codigo = N'AGRO';
    END

    PRINT 'Seed Agrofield OK';
END
ELSE PRINT 'Seed Agrofield omitido (ya existe o sin empresa)';
GO

PRINT '=== VORDIA AGRO prototipo listo ===';
GO

/* ======================================================================
   VORDIA AGRO: ciclo productivo y módulos  (Upgrade_Vordia_Agro_Modulos.sql)
   ====================================================================== */
PRINT '--- VORDIA AGRO: ciclo productivo y módulos ---';
GO
/*
  VORDIA AGRO — Rediseño por módulos (Base + Cría + Recría + Feed + Health + Trace + Finance + Connect)
  Requiere: Create_Vordia_Agro.sql y Seed_Agrofield_Empresa.sql ya ejecutados.
  Idempotente: se puede correr varias veces.

  Agrega:
    - AgroConfiguracion: sistema productivo, paquete y módulos contratados por empresa.
    - AgroAnimales: etapa del ciclo productivo + genealogía + peso objetivo.
    - AgroLotes: etapa y peso objetivo del lote.
    - AgroSanidad: vacunas, tratamientos, desparasitaciones (programadas / aplicadas, días de retiro).
    - Seed demo Agrofield de ciclo completo (reproductores, cría, recría, engorde).
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

/* ========== CONFIGURACIÓN / LICENCIA POR MÓDULOS ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroConfiguracion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroConfiguracion (
        ConfiguracionId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroConf PRIMARY KEY,
        EmpresaId          INT NOT NULL,
        SistemaProductivo  NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroConf_Sist DEFAULT (N'CicloCompleto'), -- Cria | Recria | Engorde | Confinamiento | CicloCompleto
        Paquete            NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroConf_Paq DEFAULT (N'Professional'),   -- Essential | Professional | Enterprise
        ModulosActivos     NVARCHAR(400) NOT NULL CONSTRAINT DF_AgroConf_Mod DEFAULT (N''),             -- CSV: CRIA,RECRIA,FEED,HEALTH,TRACE,FINANCE,CONNECT
        PesoObjetivoKg     DECIMAL(10,2) NOT NULL CONSTRAINT DF_AgroConf_PObj DEFAULT (480),
        PesoObjetivoRecriaKg DECIMAL(10,2) NOT NULL CONSTRAINT DF_AgroConf_PRec DEFAULT (330),
        GpdMinimoKg        DECIMAL(6,2) NOT NULL CONSTRAINT DF_AgroConf_Gpd DEFAULT (0.8),
        PrecioKgGs         DECIMAL(18,2) NOT NULL CONSTRAINT DF_AgroConf_Precio DEFAULT (14000),
        CostoRacionKgGs    DECIMAL(18,2) NOT NULL CONSTRAINT DF_AgroConf_Costo DEFAULT (1250),
        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_AgroConf_Upd DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroConf_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT UQ_AgroConf_Empresa UNIQUE (EmpresaId)
    );
    PRINT 'AgroConfiguracion creada';
END
ELSE PRINT 'AgroConfiguracion ya existe';
GO

/* ========== ANIMALES: ETAPA DEL CICLO + GENEALOGÍA ========== */
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH(N'dbo.AgroAnimales', N'Etapa') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD Etapa NVARCHAR(30) NOT NULL CONSTRAINT DF_AgroAn_Etapa DEFAULT (N'Engorde');
IF COL_LENGTH(N'dbo.AgroAnimales', N'FechaEtapa') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD FechaEtapa DATETIME2 NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'Raza') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD Raza NVARCHAR(60) NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'FechaNacimiento') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD FechaNacimiento DATE NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'PesoNacimientoKg') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD PesoNacimientoKg DECIMAL(10,2) NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'MadreCodigo') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD MadreCodigo NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'PadreCodigo') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD PadreCodigo NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.AgroAnimales', N'PesoObjetivoKg') IS NULL
    ALTER TABLE dbo.AgroAnimales ADD PesoObjetivoKg DECIMAL(10,2) NULL;
GO

SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AgroAn_Etapa' AND object_id=OBJECT_ID(N'dbo.AgroAnimales'))
    CREATE INDEX IX_AgroAn_Etapa ON dbo.AgroAnimales (EmpresaId, EstablecimientoId, Activo, Etapa);
GO

/* ========== LOTES: ETAPA Y OBJETIVO ========== */
SET QUOTED_IDENTIFIER ON;
IF COL_LENGTH(N'dbo.AgroLotes', N'Etapa') IS NULL
    ALTER TABLE dbo.AgroLotes ADD Etapa NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.AgroLotes', N'PesoObjetivoKg') IS NULL
    ALTER TABLE dbo.AgroLotes ADD PesoObjetivoKg DECIMAL(10,2) NULL;
GO

/* ========== SANIDAD ========== */
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroSanidad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroSanidad (
        SanidadId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroSan PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        AnimalId          INT NULL,  -- evento individual
        LoteId            INT NULL,  -- evento por lote (campaña)
        Tipo              NVARCHAR(30) NOT NULL, -- Vacuna | Tratamiento | Desparasitacion | Diagnostico
        Producto          NVARCHAR(120) NOT NULL,
        Dosis             NVARCHAR(60) NULL,
        FechaProgramada   DATE NULL,
        FechaAplicacion   DATETIME2 NULL,
        DiasRetiro        INT NOT NULL CONSTRAINT DF_AgroSan_Ret DEFAULT (0),
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_AgroSan_Estado DEFAULT (N'Programada'), -- Programada | Aplicada | Cancelada
        Responsable       NVARCHAR(120) NULL,
        Notas             NVARCHAR(300) NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_AgroSan_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroSan_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroSan_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroSan_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId),
        CONSTRAINT FK_AgroSan_Animal FOREIGN KEY (AnimalId) REFERENCES dbo.AgroAnimales(AnimalId),
        CONSTRAINT FK_AgroSan_Lote FOREIGN KEY (LoteId) REFERENCES dbo.AgroLotes(LoteId),
        CONSTRAINT CK_AgroSan_Estado CHECK (Estado IN (N'Programada', N'Aplicada', N'Cancelada'))
    );
    CREATE INDEX IX_AgroSan_Est ON dbo.AgroSanidad (EmpresaId, EstablecimientoId, Activo, Estado);
    PRINT 'AgroSanidad creada';
END
ELSE PRINT 'AgroSanidad ya existe';
GO

/* ========== SEED DEMO AGROFIELD (ciclo completo) ========== */
SET QUOTED_IDENTIFIER ON;
DECLARE @EmpresaId INT = (
    SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield' ORDER BY EmpresaId
);
DECLARE @EstId INT = (
    SELECT TOP 1 EstablecimientoId FROM dbo.AgroEstablecimientos WHERE EmpresaId = @EmpresaId AND Activo = 1 ORDER BY EstablecimientoId
);

IF ISNULL(CAST(SESSION_CONTEXT(N'vordia_sin_demo') AS INT), 0) = 1
BEGIN
    PRINT 'Seed demo omitido (despliegue de producción)';
END
ELSE IF @EmpresaId IS NULL OR @EstId IS NULL
BEGIN
    PRINT 'Seed omitido: no hay empresa/establecimiento Agrofield';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.AgroConfiguracion WHERE EmpresaId = @EmpresaId)
BEGIN
    PRINT 'Seed omitido: Agrofield ya tiene configuración de módulos';
END
ELSE
BEGIN
    BEGIN TRAN;

    DECLARE @Hoy DATE = CAST(SYSUTCDATETIME() AS DATE);

    INSERT INTO dbo.AgroConfiguracion (EmpresaId, SistemaProductivo, Paquete, ModulosActivos, PesoObjetivoKg, PesoObjetivoRecriaKg, GpdMinimoKg, PrecioKgGs, CostoRacionKgGs)
    VALUES (@EmpresaId, N'CicloCompleto', N'Enterprise', N'CRIA,RECRIA,FEED,HEALTH,TRACE,FINANCE,CONNECT', 480, 330, 0.8, 14000, 1250);

    /* Lotes: los existentes pasan a tener etapa; se agregan potrero de cría y rodeo de madres */
    UPDATE dbo.AgroLotes SET Etapa = N'Engorde', PesoObjetivoKg = 480
     WHERE EstablecimientoId = @EstId AND Codigo IN (N'C-01', N'C-04');
    UPDATE dbo.AgroLotes SET Etapa = N'Recria', PesoObjetivoKg = 330, Nombre = N'Potrero recría 7'
     WHERE EstablecimientoId = @EstId AND Codigo = N'P-07';

    IF NOT EXISTS (SELECT 1 FROM dbo.AgroLotes WHERE EstablecimientoId = @EstId AND Codigo = N'P-02')
        INSERT INTO dbo.AgroLotes (EmpresaId, EstablecimientoId, Codigo, Nombre, Tipo, Capacidad, Etapa)
        VALUES (@EmpresaId, @EstId, N'P-02', N'Potrero cría 2 (vaca + ternero)', N'Potrero', 60, N'Cria');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgroLotes WHERE EstablecimientoId = @EstId AND Codigo = N'R-01')
        INSERT INTO dbo.AgroLotes (EmpresaId, EstablecimientoId, Codigo, Nombre, Tipo, Capacidad, Etapa)
        VALUES (@EmpresaId, @EstId, N'R-01', N'Rodeo de madres', N'Potrero', 40, N'Reproductor');

    DECLARE @LC01 INT = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-01');
    DECLARE @LC04 INT = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-04');
    DECLARE @LP07 INT = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'P-07');
    DECLARE @LP02 INT = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'P-02');
    DECLARE @LR01 INT = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'R-01');

    /* Animales existentes */
    UPDATE dbo.AgroAnimales SET Etapa = N'Engorde', Raza = ISNULL(Raza, N'Brangus'), FechaEtapa = FechaIngreso
     WHERE EstablecimientoId = @EstId AND LoteId IN (@LC01, @LC04);
    UPDATE dbo.AgroAnimales SET Etapa = N'Recria', Raza = ISNULL(Raza, N'Brangus'), FechaEtapa = FechaIngreso
     WHERE EstablecimientoId = @EstId AND LoteId = @LP07;

    /* Tabla de números 1..60 */
    DECLARE @N TABLE (n INT PRIMARY KEY);
    INSERT INTO @N (n) SELECT TOP 60 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) FROM sys.all_objects;

    /* ---- Reproductores: 24 vacas + 2 toros ---- */
    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, FechaNacimiento, PesoIngresoKg, PesoActualKg, Estado, Etapa, FechaEtapa)
    SELECT @EmpresaId, @EstId, @LR01, CONCAT(N'', 6000 + n), CONCAT(N'RFID-', 6000 + n), N'Vaca', N'H',
           CASE WHEN n % 3 = 0 THEN N'Nelore' ELSE N'Brangus' END,
           DATEADD(DAY, -900, @Hoy), DATEADD(DAY, -(1300 + (n * 41) % 700), @Hoy),
           410 + (n * 7) % 60, 420 + (n * 11) % 70, N'Activo', N'Reproductor', DATEADD(DAY, -900, @Hoy)
      FROM @N WHERE n <= 24;

    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, FechaNacimiento, PesoIngresoKg, PesoActualKg, Estado, Etapa, FechaEtapa)
    VALUES
    (@EmpresaId, @EstId, @LR01, N'T-084', N'RFID-T084', N'Toro', N'M', N'Brangus', DATEADD(DAY,-700,@Hoy), DATEADD(DAY,-1500,@Hoy), 780, 805, N'Activo', N'Reproductor', DATEADD(DAY,-700,@Hoy)),
    (@EmpresaId, @EstId, @LR01, N'T-091', N'RFID-T091', N'Toro', N'M', N'Nelore', DATEADD(DAY,-500,@Hoy), DATEADD(DAY,-1200,@Hoy), 720, 760, N'Activo', N'Reproductor', DATEADD(DAY,-500,@Hoy));

    /* ---- Cría: 22 terneros al pie de la madre ---- */
    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, FechaNacimiento, PesoNacimientoKg, PesoIngresoKg, PesoActualKg, MadreCodigo, PadreCodigo, Estado, Etapa, FechaEtapa)
    SELECT @EmpresaId, @EstId, @LP02, CONCAT(N'', 5000 + n), CONCAT(N'RFID-', 5000 + n),
           CASE WHEN n % 2 = 0 THEN N'Ternera' ELSE N'Ternero' END,
           CASE WHEN n % 2 = 0 THEN N'H' ELSE N'M' END,
           N'Brangus',
           DATEADD(DAY, -(45 + (n * 7) % 150), @Hoy),
           DATEADD(DAY, -(45 + (n * 7) % 150), @Hoy),
           30 + n % 8, 30 + n % 8,
           CAST(30 + n % 8 + (0.70 + ((n * 3) % 5) * 0.05) * (45 + (n * 7) % 150) AS DECIMAL(10,2)),
           CONCAT(N'', 6000 + 1 + (n * 3) % 24),
           CASE WHEN n % 3 = 0 THEN N'T-091' ELSE N'T-084' END,
           N'Activo', N'Cria', DATEADD(DAY, -(45 + (n * 7) % 150), @Hoy)
      FROM @N WHERE n <= 22;

    /* ---- Recría: 26 animales en P-07 ---- */
    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, FechaNacimiento, PesoIngresoKg, PesoActualKg, MadreCodigo, Estado, Etapa, FechaEtapa)
    SELECT @EmpresaId, @EstId, @LP07, CONCAT(N'', 4000 + n), CONCAT(N'RFID-', 4000 + n),
           CASE WHEN n % 4 = 0 THEN N'Vaquilla' ELSE N'Novillito' END,
           CASE WHEN n % 4 = 0 THEN N'H' ELSE N'M' END,
           CASE WHEN n % 3 = 0 THEN N'Nelore' ELSE N'Brangus' END,
           DATEADD(DAY, -(40 + (n * 9) % 120), @Hoy),
           DATEADD(DAY, -(250 + (n * 9) % 120), @Hoy),
           175 + (n * 13) % 40,
           CAST(175 + (n * 13) % 40 + (0.55 + ((n * 5) % 7) * 0.06) * (40 + (n * 9) % 120) AS DECIMAL(10,2)),
           CONCAT(N'', 6000 + 1 + (n * 5) % 24),
           N'Activo', N'Recria', DATEADD(DAY, -(40 + (n * 9) % 120), @Hoy)
      FROM @N WHERE n <= 26;

    /* ---- Engorde / confinamiento: C-01 (46) y C-04 (38) ---- */
    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, PesoIngresoKg, PesoActualKg, Estado, Etapa, FechaEtapa)
    SELECT @EmpresaId, @EstId, @LC01, CONCAT(N'', 3000 + n), CONCAT(N'RFID-', 3000 + n), N'Novillo', N'M',
           CASE WHEN n % 3 = 0 THEN N'Nelore' ELSE N'Brangus' END,
           DATEADD(DAY, -(28 + (n * 13) % 70), @Hoy),
           300 + (n * 17) % 90,
           CAST(300 + (n * 17) % 90 + (CASE WHEN n % 15 = 0 THEN 0.55 ELSE 1.05 + ((n * 7) % 9) * 0.06 END) * (28 + (n * 13) % 70) AS DECIMAL(10,2)),
           N'Activo', N'Engorde', DATEADD(DAY, -(28 + (n * 13) % 70), @Hoy)
      FROM @N WHERE n <= 46;

    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, Categoria, Sexo, Raza, FechaIngreso, PesoIngresoKg, PesoActualKg, Estado, Etapa, FechaEtapa)
    SELECT @EmpresaId, @EstId, @LC04, CONCAT(N'', 3100 + n), CONCAT(N'RFID-', 3100 + n),
           CASE WHEN n % 5 = 0 THEN N'Vaquilla' ELSE N'Novillo' END,
           CASE WHEN n % 5 = 0 THEN N'H' ELSE N'M' END,
           N'Brangus',
           DATEADD(DAY, -(14 + (n * 11) % 50), @Hoy),
           320 + (n * 19) % 80,
           CAST(320 + (n * 19) % 80 + (CASE WHEN n % 12 = 0 THEN 0.6 ELSE 0.95 + ((n * 5) % 8) * 0.07 END) * (14 + (n * 11) % 50) AS DECIMAL(10,2)),
           N'Activo', N'Engorde', DATEADD(DAY, -(14 + (n * 11) % 50), @Hoy)
      FROM @N WHERE n <= 38;

    /* ---- Pesajes semanales (curva de evolución) para cría, recría y engorde ---- */
    INSERT INTO dbo.AgroPesajes (EmpresaId, AnimalId, PesoKg, FechaPesaje, Origen, Operario)
    SELECT a.EmpresaId, a.AnimalId,
           CAST(a.PesoIngresoKg + (a.PesoActualKg - a.PesoIngresoKg) * (w.n - 1) * 7.0
                / NULLIF(DATEDIFF(DAY, a.FechaIngreso, @Hoy), 0) AS DECIMAL(10,2)),
           DATEADD(HOUR, 8, CAST(DATEADD(DAY, (w.n - 1) * 7, a.FechaIngreso) AS DATETIME2)),
           CASE WHEN a.Etapa = N'Engorde' THEN N'Balanza' ELSE N'RFID' END,
           N'Operario demo'
      FROM dbo.AgroAnimales a
      JOIN @N w ON (w.n - 1) * 7 < DATEDIFF(DAY, a.FechaIngreso, @Hoy)
     WHERE a.EstablecimientoId = @EstId AND a.Activo = 1
       AND a.Etapa IN (N'Cria', N'Recria', N'Engorde')
       AND a.FechaIngreso IS NOT NULL AND a.PesoIngresoKg IS NOT NULL AND a.PesoActualKg IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AgroPesajes p WHERE p.AnimalId = a.AnimalId);

    /* Último pesaje = peso actual (ayer) */
    INSERT INTO dbo.AgroPesajes (EmpresaId, AnimalId, PesoKg, FechaPesaje, Origen, Operario)
    SELECT a.EmpresaId, a.AnimalId, a.PesoActualKg, DATEADD(HOUR, 8, CAST(DATEADD(DAY, -1, @Hoy) AS DATETIME2)),
           CASE WHEN a.Etapa = N'Engorde' THEN N'Balanza' ELSE N'RFID' END, N'Operario demo'
      FROM dbo.AgroAnimales a
     WHERE a.EstablecimientoId = @EstId AND a.Activo = 1 AND a.Etapa IN (N'Cria', N'Recria', N'Engorde')
       AND a.PesoActualKg IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AgroPesajes p WHERE p.AnimalId = a.AnimalId AND p.FechaPesaje >= DATEADD(DAY, -2, @Hoy));

    /* ---- Raciones diarias últimos 30 días ---- */
    DECLARE @CabC01 INT = (SELECT COUNT(*) FROM dbo.AgroAnimales WHERE LoteId=@LC01 AND Activo=1);
    DECLARE @CabC04 INT = (SELECT COUNT(*) FROM dbo.AgroAnimales WHERE LoteId=@LC04 AND Activo=1);
    DECLARE @CabP07 INT = (SELECT COUNT(*) FROM dbo.AgroAnimales WHERE LoteId=@LP07 AND Activo=1);

    INSERT INTO dbo.AgroRaciones (EmpresaId, LoteId, TipoAlimento, CantidadKg, FechaEntrega, Operario, Vehiculo, CostoGs)
    SELECT @EmpresaId, x.LoteId, x.Tipo, x.Kg, x.Fecha, x.Operario, x.Vehiculo, x.Kg * x.CostoKg
      FROM (
        SELECT @LC01 AS LoteId, N'TMR engorde' AS Tipo,
               CAST(@CabC01 * (10.8 + (d.n % 5) * 0.2) AS DECIMAL(12,2)) AS Kg,
               DATEADD(HOUR, 7, CAST(DATEADD(DAY, -(d.n - 1), @Hoy) AS DATETIME2)) AS Fecha,
               N'Juan P.' AS Operario, N'Mixer 01' AS Vehiculo, CAST(1250 AS DECIMAL(18,2)) AS CostoKg
          FROM @N d WHERE d.n <= 30
        UNION ALL
        SELECT @LC04, N'TMR engorde',
               CAST(@CabC04 * (10.2 + (d.n % 4) * 0.25) AS DECIMAL(12,2)),
               DATEADD(HOUR, 8, CAST(DATEADD(DAY, -(d.n - 1), @Hoy) AS DATETIME2)),
               N'Juan P.', N'Mixer 01', 1250
          FROM @N d WHERE d.n <= 30
        UNION ALL
        SELECT @LP07, N'Suplemento proteico',
               CAST(@CabP07 * (2.2 + (d.n % 3) * 0.2) AS DECIMAL(12,2)),
               DATEADD(HOUR, 9, CAST(DATEADD(DAY, -(d.n - 1), @Hoy) AS DATETIME2)),
               N'Mario L.', N'Tractor 03', 2600
          FROM @N d WHERE d.n <= 30
      ) x;

    /* ---- Sanidad ---- */
    DECLARE @A1902 INT = (SELECT TOP 1 AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1902');

    INSERT INTO dbo.AgroSanidad (EmpresaId, EstablecimientoId, AnimalId, LoteId, Tipo, Producto, Dosis, FechaProgramada, FechaAplicacion, DiasRetiro, Estado, Responsable, Notas)
    VALUES
    (@EmpresaId, @EstId, NULL, @LC01, N'Vacuna', N'Aftosa (campaña oficial SENACSA)', N'2 ml', DATEADD(DAY,-40,@Hoy), DATEADD(DAY,-40,SYSUTCDATETIME()), 0, N'Aplicada', N'Dr. Benítez', N'1ra dosis del período'),
    (@EmpresaId, @EstId, NULL, @LC04, N'Vacuna', N'Aftosa (campaña oficial SENACSA)', N'2 ml', DATEADD(DAY,3,@Hoy), NULL, 0, N'Programada', N'Dr. Benítez', NULL),
    (@EmpresaId, @EstId, NULL, @LP07, N'Vacuna', N'Clostridiales polivalente', N'5 ml', DATEADD(DAY,-2,@Hoy), NULL, 0, N'Programada', N'Dr. Benítez', N'Refuerzo pendiente'),
    (@EmpresaId, @EstId, NULL, @LP02, N'Desparasitacion', N'Ivermectina 1%', N'1 ml / 50 kg', @Hoy, NULL, 28, N'Programada', N'Mario L.', N'Terneros al pie'),
    (@EmpresaId, @EstId, NULL, @LP02, N'Vacuna', N'Brucelosis (terneras 3-8 meses)', N'2 ml', DATEADD(DAY,10,@Hoy), NULL, 0, N'Programada', N'Dr. Benítez', N'Solo hembras'),
    (@EmpresaId, @EstId, NULL, @LR01, N'Diagnostico', N'Tacto / diagnóstico de preñez', NULL, DATEADD(DAY,6,@Hoy), NULL, 0, N'Programada', N'Dr. Benítez', N'Post temporada de servicio'),
    (@EmpresaId, @EstId, NULL, @LC01, N'Desparasitacion', N'Doramectina 1%', N'1 ml / 50 kg', DATEADD(DAY,-40,@Hoy), DATEADD(DAY,-40,SYSUTCDATETIME()), 35, N'Aplicada', N'Juan P.', N'Ingreso a corral');

    IF @A1902 IS NOT NULL
        INSERT INTO dbo.AgroSanidad (EmpresaId, EstablecimientoId, AnimalId, LoteId, Tipo, Producto, Dosis, FechaProgramada, FechaAplicacion, DiasRetiro, Estado, Responsable, Notas)
        VALUES (@EmpresaId, @EstId, @A1902, NULL, N'Tratamiento', N'Oxitetraciclina LA', N'20 ml', DATEADD(DAY,-5,@Hoy), DATEADD(DAY,-5,SYSUTCDATETIME()), 28, N'Aplicada', N'Dr. Benítez', N'Neumonía leve — control en 7 días');

    COMMIT;
    PRINT 'Seed VORDIA Agro por módulos OK';
END
GO

/* ========== v2: OBJETIVOS PRODUCTIVOS ========== */
IF COL_LENGTH(N'dbo.AgroConfiguracion', N'GpdObjetivoKg') IS NULL
    ALTER TABLE dbo.AgroConfiguracion ADD GpdObjetivoKg DECIMAL(6,2) NOT NULL CONSTRAINT DF_AgroConf_GpdObj DEFAULT (1.4);
IF COL_LENGTH(N'dbo.AgroConfiguracion', N'ConversionObjetivo') IS NULL
    ALTER TABLE dbo.AgroConfiguracion ADD ConversionObjetivo DECIMAL(6,2) NOT NULL CONSTRAINT DF_AgroConf_ConvObj DEFAULT (8.0);
IF COL_LENGTH(N'dbo.AgroEstablecimientos', N'Latitud') IS NULL
    ALTER TABLE dbo.AgroEstablecimientos ADD Latitud DECIMAL(10,6) NULL;
IF COL_LENGTH(N'dbo.AgroEstablecimientos', N'Longitud') IS NULL
    ALTER TABLE dbo.AgroEstablecimientos ADD Longitud DECIMAL(10,6) NULL;
GO

/* ========== v2: INVENTARIO DE INSUMOS (alimento, medicamentos) ========== */
IF OBJECT_ID(N'dbo.AgroInsumos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroInsumos (
        InsumoId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroInsumo PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        EstablecimientoId INT NOT NULL,
        Nombre            NVARCHAR(120) NOT NULL,      -- para alimento, igual al TipoAlimento de las raciones
        Tipo              NVARCHAR(20) NOT NULL CONSTRAINT DF_AgroInsumo_Tipo DEFAULT (N'Alimento'), -- Alimento | Medicamento | Otro
        Unidad            NVARCHAR(10) NOT NULL CONSTRAINT DF_AgroInsumo_Unidad DEFAULT (N'kg'),
        StockMinimo       DECIMAL(14,2) NOT NULL CONSTRAINT DF_AgroInsumo_Min DEFAULT (0),
        Activo            BIT NOT NULL CONSTRAINT DF_AgroInsumo_Activo DEFAULT (1),
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_AgroInsumo_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AgroInsumo_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_AgroInsumo_Est FOREIGN KEY (EstablecimientoId) REFERENCES dbo.AgroEstablecimientos(EstablecimientoId),
        CONSTRAINT CK_AgroInsumo_Tipo CHECK (Tipo IN (N'Alimento', N'Medicamento', N'Otro'))
    );
    CREATE INDEX IX_AgroInsumo_Est ON dbo.AgroInsumos (EmpresaId, EstablecimientoId, Activo);
    PRINT 'AgroInsumos creada';
END
GO

IF OBJECT_ID(N'dbo.AgroInsumoMovimientos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgroInsumoMovimientos (
        MovimientoId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgroInsMov PRIMARY KEY,
        EmpresaId       INT NOT NULL,
        InsumoId        INT NOT NULL,
        Tipo            NVARCHAR(20) NOT NULL,          -- Ingreso | Consumo | Ajuste (cantidad con signo)
        Cantidad        DECIMAL(14,2) NOT NULL,
        CostoUnitarioGs DECIMAL(18,2) NULL,
        Fecha           DATETIME2 NOT NULL CONSTRAINT DF_AgroInsMov_Fecha DEFAULT (SYSUTCDATETIME()),
        Referencia      NVARCHAR(200) NULL,             -- proveedor, factura, lote aplicado
        Operario        NVARCHAR(120) NULL,
        Activo          BIT NOT NULL CONSTRAINT DF_AgroInsMov_Activo DEFAULT (1),
        CONSTRAINT FK_AgroInsMov_Insumo FOREIGN KEY (InsumoId) REFERENCES dbo.AgroInsumos(InsumoId),
        CONSTRAINT CK_AgroInsMov_Tipo CHECK (Tipo IN (N'Ingreso', N'Consumo', N'Ajuste'))
    );
    CREATE INDEX IX_AgroInsMov_Insumo ON dbo.AgroInsumoMovimientos (InsumoId, Activo, Fecha);
    PRINT 'AgroInsumoMovimientos creada';
END
GO

/* Seed demo Agrofield: coordenadas, insumos y compras que dejan el balanceado justo (≈ 2,8 días) */
DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield' ORDER BY EmpresaId);
DECLARE @EstId INT = (SELECT TOP 1 EstablecimientoId FROM dbo.AgroEstablecimientos WHERE EmpresaId = @EmpresaId AND Activo = 1 ORDER BY EstablecimientoId);

IF @EstId IS NOT NULL AND ISNULL(CAST(SESSION_CONTEXT(N'vordia_sin_demo') AS INT), 0) = 0
BEGIN
    UPDATE dbo.AgroEstablecimientos SET Latitud = ISNULL(Latitud, -23.4425), Longitud = ISNULL(Longitud, -58.4438)
     WHERE EstablecimientoId = @EstId;

    IF NOT EXISTS (SELECT 1 FROM dbo.AgroInsumos WHERE EstablecimientoId = @EstId)
    BEGIN
        DECLARE @Desde DATETIME2 = DATEADD(DAY, -30, CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIME2));
        INSERT INTO dbo.AgroInsumos (EmpresaId, EstablecimientoId, Nombre, Tipo, Unidad, StockMinimo, FechaCreacion)
        VALUES
        (@EmpresaId, @EstId, N'TMR engorde', N'Alimento', N'kg', 5000, @Desde),
        (@EmpresaId, @EstId, N'Suplemento proteico', N'Alimento', N'kg', 800, @Desde),
        (@EmpresaId, @EstId, N'Vacuna aftosa (dosis)', N'Medicamento', N'dosis', 50, @Desde),
        (@EmpresaId, @EstId, N'Ivermectina 1%', N'Medicamento', N'ml', 200, @Desde);

        DECLARE @Tmr INT = (SELECT InsumoId FROM dbo.AgroInsumos WHERE EstablecimientoId = @EstId AND Nombre = N'TMR engorde');
        DECLARE @Sup INT = (SELECT InsumoId FROM dbo.AgroInsumos WHERE EstablecimientoId = @EstId AND Nombre = N'Suplemento proteico');
        DECLARE @Aft INT = (SELECT InsumoId FROM dbo.AgroInsumos WHERE EstablecimientoId = @EstId AND Nombre = N'Vacuna aftosa (dosis)');
        DECLARE @Ive INT = (SELECT InsumoId FROM dbo.AgroInsumos WHERE EstablecimientoId = @EstId AND Nombre = N'Ivermectina 1%');

        -- Consumo real registrado en raciones desde la creación del insumo
        DECLARE @UsoTmr DECIMAL(14,2) = ISNULL((SELECT SUM(r.CantidadKg) FROM dbo.AgroRaciones r JOIN dbo.AgroLotes l ON l.LoteId = r.LoteId
                                                WHERE l.EstablecimientoId = @EstId AND r.Activo = 1 AND r.TipoAlimento = N'TMR engorde' AND r.FechaEntrega >= @Desde), 0);
        DECLARE @DiaTmr DECIMAL(14,2) = ISNULL((SELECT SUM(r.CantidadKg) / 7.0 FROM dbo.AgroRaciones r JOIN dbo.AgroLotes l ON l.LoteId = r.LoteId
                                                WHERE l.EstablecimientoId = @EstId AND r.Activo = 1 AND r.TipoAlimento = N'TMR engorde' AND r.FechaEntrega >= DATEADD(DAY, -7, SYSUTCDATETIME())), 0);
        DECLARE @UsoSup DECIMAL(14,2) = ISNULL((SELECT SUM(r.CantidadKg) FROM dbo.AgroRaciones r JOIN dbo.AgroLotes l ON l.LoteId = r.LoteId
                                                WHERE l.EstablecimientoId = @EstId AND r.Activo = 1 AND r.TipoAlimento = N'Suplemento proteico' AND r.FechaEntrega >= @Desde), 0);

        DECLARE @TercioTmr DECIMAL(14,2) = ROUND((@UsoTmr + @DiaTmr * 2.8) / 3, 0);
        INSERT INTO dbo.AgroInsumoMovimientos (EmpresaId, InsumoId, Tipo, Cantidad, CostoUnitarioGs, Fecha, Referencia, Operario)
        VALUES
        (@EmpresaId, @Tmr, N'Ingreso', @TercioTmr, 1250, DATEADD(DAY, -30, SYSUTCDATETIME()), N'Planta de balanceados · Fact. 001-001-0004512', N'Juan P.'),
        (@EmpresaId, @Tmr, N'Ingreso', @TercioTmr, 1250, DATEADD(DAY, -20, SYSUTCDATETIME()), N'Planta de balanceados · Fact. 001-001-0004620', N'Juan P.'),
        (@EmpresaId, @Tmr, N'Ingreso', ROUND(@UsoTmr + @DiaTmr * 2.8, 0) - 2 * @TercioTmr, 1250, DATEADD(DAY, -10, SYSUTCDATETIME()), N'Planta de balanceados · Fact. 001-001-0004733', N'Juan P.'),
        (@EmpresaId, @Sup, N'Ingreso', ROUND(@UsoSup * 1.3, 0), 2600, DATEADD(DAY, -28, SYSUTCDATETIME()), N'Cooperativa · Fact. 002-001-0001180', N'Mario L.'),
        (@EmpresaId, @Aft, N'Ingreso', 300, 4500, DATEADD(DAY, -45, SYSUTCDATETIME()), N'SENACSA · campaña', N'Dr. Benítez'),
        (@EmpresaId, @Aft, N'Consumo', 48, NULL, DATEADD(DAY, -40, SYSUTCDATETIME()), N'Campaña lote C-01', N'Dr. Benítez'),
        (@EmpresaId, @Ive, N'Ingreso', 500, 180, DATEADD(DAY, -40, SYSUTCDATETIME()), N'Veterinaria del Chaco', N'Dr. Benítez'),
        (@EmpresaId, @Ive, N'Consumo', 410, NULL, DATEADD(DAY, -40, SYSUTCDATETIME()), N'Desparasitación lote C-01', N'Juan P.');
        PRINT 'Inventario demo Agrofield cargado';
    END
END
GO

PRINT '=== VORDIA AGRO por módulos listo ===';
GO

/* ======================================================================
   HQ: Necesidades del Mercado, contactos, pedidos y bajas  (Create_Vordia_HQ_Mercado.sql)
   ====================================================================== */
PRINT '--- HQ: Necesidades del Mercado, contactos, pedidos y bajas ---';
GO
/*
  VORDIA HQ — Necesidades del Mercado
  Radar de pedidos públicos de sistemas y aplicaciones para convertirlos en ventas y microapps:
  DNCP Paraguay, SECOP II Colombia, Banco Mundial, Freelancer.com (es/en/pt), feeds RSS
  (Google Alerts) y carga manual / captura rápida (Workana, Facebook, LinkedIn).
  Idempotente. Guardar/ejecutar como UTF-8.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ========== FUENTES ========== */
IF OBJECT_ID(N'dbo.HqMercadoFuentes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoFuentes (
        FuenteId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercFuente PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Tipo              NVARCHAR(20) NOT NULL,   -- Freelancer | DNCP | SECOP | BancoMundial | RSS
        Nombre            NVARCHAR(120) NOT NULL,
        Consulta          NVARCHAR(500) NULL,      -- palabras clave (Freelancer) o URL del feed (RSS)
        Activa            BIT NOT NULL CONSTRAINT DF_HqMercFuente_Activa DEFAULT (1),
        UltimaSync        DATETIME2 NULL,
        UltimoResultado   NVARCHAR(300) NULL,
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_HqMercFuente_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HqMercFuente_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_HqMercFuente_Tipo CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'RSS'))
    );
    PRINT 'HqMercadoFuentes creada';
END
ELSE PRINT 'HqMercadoFuentes ya existe';
GO

/* ========== NECESIDADES DETECTADAS ========== */
IF OBJECT_ID(N'dbo.HqMercadoNecesidades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoNecesidades (
        NecesidadId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercNec PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Fuente            NVARCHAR(20) NOT NULL,       -- Freelancer | DNCP | RSS | Manual
        ExternoId         NVARCHAR(150) NOT NULL,      -- id en la fuente (deduplicación)
        Titulo            NVARCHAR(400) NOT NULL,
        Descripcion       NVARCHAR(MAX) NULL,
        Url               NVARCHAR(600) NULL,
        Solicitante       NVARCHAR(250) NULL,
        Pais              NVARCHAR(60) NULL,
        PresupuestoMin    DECIMAL(18,2) NULL,
        PresupuestoMax    DECIMAL(18,2) NULL,
        Moneda            NVARCHAR(10) NULL,
        FechaPublicacion  DATETIME2 NULL,
        FechaCierre       DATETIME2 NULL,
        Habilidades       NVARCHAR(400) NULL,
        Vertical          NVARCHAR(30) NULL,           -- HABITAT | STAY | DINE | RETAIL | BOUTIQUE | OPS | AGRO | HQ | A_MEDIDA
        Categoria         NVARCHAR(60) NULL,           -- Punto de venta, Reservas, App móvil, ...
        ProductoSugerido  NVARCHAR(300) NULL,
        Puntaje           INT NOT NULL CONSTRAINT DF_HqMercNec_Puntaje DEFAULT (0),
        MotivoPuntaje     NVARCHAR(500) NULL,
        Confianza         INT NOT NULL CONSTRAINT DF_HqMercNec_Confianza DEFAULT (0),  -- 0-100: qué tan seguro es el comprador / el cobro
        ConfianzaMotivo   NVARCHAR(300) NULL,
        Competidores      INT NULL,                    -- propuestas ya enviadas (Freelancer)
        Alcance           NVARCHAR(20) NULL,           -- Paraguay | LatAm | Internacional
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercNec_Estado DEFAULT (N'Nueva'), -- Nueva | Evaluando | Contactada | Propuesta | Ganada | Descartada
        ResponsableNombre NVARCHAR(150) NULL,
        Notas             NVARCHAR(MAX) NULL,
        ClienteId         INT NULL,                    -- lead creado en CRM
        RoadmapId         INT NULL,                    -- idea de producto rápido
        FechaCaptura      DATETIME2 NOT NULL CONSTRAINT DF_HqMercNec_Cap DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_HqMercNec_Activo DEFAULT (1),
        CONSTRAINT FK_HqMercNec_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_HqMercNec_Estado CHECK (Estado IN (N'Nueva', N'Evaluando', N'Contactada', N'Propuesta', N'Ganada', N'Descartada'))
    );
    CREATE UNIQUE INDEX UX_HqMercNec_Externo ON dbo.HqMercadoNecesidades (EmpresaId, Fuente, ExternoId);
    CREATE INDEX IX_HqMercNec_Lista ON dbo.HqMercadoNecesidades (EmpresaId, Activo, Estado, Puntaje DESC);
    PRINT 'HqMercadoNecesidades creada';
END
ELSE PRINT 'HqMercadoNecesidades ya existe';
GO

/* ========== ACTUALIZACIÓN v2 (bases creadas con la primera versión) ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Confianza') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Confianza INT NOT NULL CONSTRAINT DF_HqMercNec_Confianza DEFAULT (0);
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ConfianzaMotivo') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ConfianzaMotivo NVARCHAR(300) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Competidores') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Competidores INT NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Alcance') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Alcance NVARCHAR(20) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo'
           AND definition NOT LIKE N'%BancoMundial%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'RSS'));
    PRINT 'Tipos de fuente ampliados';
END
GO

/* ========== FUENTES INICIALES PARA VORDIA HQ ========== */
DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);

IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    VALUES
    (@EmpresaId, N'DNCP', N'Licitaciones públicas Paraguay (DNCP)', NULL),
    (@EmpresaId, N'Freelancer', N'Freelancer · sistemas de gestión', N'sistema de gestion'),
    (@EmpresaId, N'Freelancer', N'Freelancer · software / ERP', N'software'),
    (@EmpresaId, N'Freelancer', N'Freelancer · aplicaciones móviles', N'app movil'),
    (@EmpresaId, N'Freelancer', N'Freelancer · punto de venta / inventario', N'inventario'),
    (@EmpresaId, N'Freelancer', N'Freelancer · reservas / turnos', N'reservas'),
    (@EmpresaId, N'Freelancer', N'Freelancer · sitio web para negocio', N'pagina web');
    PRINT CONCAT('Fuentes iniciales creadas para EmpresaId=', @EmpresaId);
END
ELSE PRINT 'Fuentes iniciales omitidas (ya existen o no hay empresa VORDIA)';
GO

/* ========== FUENTES INTERNACIONALES (v2) ========== */
DECLARE @Emp INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    SELECT @Emp, v.Tipo, v.Nombre, v.Consulta
    FROM (VALUES
        (N'SECOP', N'Licitaciones públicas Colombia (SECOP II)', NULL),
        (N'BancoMundial', N'Licitaciones Banco Mundial (internacional)', N'software information system digital platform'),
        (N'Freelancer', N'Freelancer · software (inglés, internacional)', N'software development|en'),
        (N'Freelancer', N'Freelancer · web app / SaaS (inglés)', N'web app|en'),
        (N'Freelancer', N'Freelancer · plataforma web (español)', N'plataforma web')
    ) AS v(Tipo, Nombre, Consulta)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp AND f.Tipo = v.Tipo AND ISNULL(f.Consulta, N'') = ISNULL(v.Consulta, N''));
    PRINT CONCAT('Fuentes internacionales agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v3: CONTACTO Y PEDIDOS RÁPIDOS ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoNombre') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoNombre NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoEmail') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoEmail NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoTelefono') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoTelefono NVARCHAR(40) NULL;
GO

/* Historial de contactos (WhatsApp, correo, plataforma, llamada) */
IF OBJECT_ID(N'dbo.HqMercadoContactos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoContactos (
        ContactoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercContacto PRIMARY KEY,
        EmpresaId      INT NOT NULL,
        NecesidadId    INT NOT NULL,
        Canal          NVARCHAR(20) NOT NULL,           -- WhatsApp | Correo | Plataforma | Llamada
        Destino        NVARCHAR(150) NULL,
        Asunto         NVARCHAR(200) NULL,
        Mensaje        NVARCHAR(MAX) NULL,
        Resultado      NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercContacto_Res DEFAULT (N'Registrado'), -- Registrado | Enviado | Error
        Detalle        NVARCHAR(300) NULL,
        UsuarioNombre  NVARCHAR(150) NULL,
        Fecha          DATETIME2 NOT NULL CONSTRAINT DF_HqMercContacto_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HqMercContacto_Nec FOREIGN KEY (NecesidadId) REFERENCES dbo.HqMercadoNecesidades(NecesidadId)
    );
    CREATE INDEX IX_HqMercContacto_Nec ON dbo.HqMercadoContactos (NecesidadId, Fecha DESC);
    PRINT 'HqMercadoContactos creada';
END
GO

/* Pedidos / propuestas rápidas con link público para aceptar */
IF OBJECT_ID(N'dbo.HqMercadoPedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoPedidos (
        PedidoId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercPedido PRIMARY KEY,
        EmpresaId           INT NOT NULL,
        NecesidadId         INT NULL,
        Token               NVARCHAR(40) NOT NULL,
        Numero              NVARCHAR(20) NOT NULL,
        ClienteNombre       NVARCHAR(200) NOT NULL,
        ClienteEmail        NVARCHAR(150) NULL,
        ClienteTelefono     NVARCHAR(40) NULL,
        Producto            NVARCHAR(200) NOT NULL,
        Incluye             NVARCHAR(MAX) NULL,
        Moneda              NVARCHAR(10) NOT NULL CONSTRAINT DF_HqMercPedido_Moneda DEFAULT (N'USD'),
        PrecioSetup         DECIMAL(18,2) NOT NULL CONSTRAINT DF_HqMercPedido_Setup DEFAULT (0),
        PrecioMensual       DECIMAL(18,2) NOT NULL CONSTRAINT DF_HqMercPedido_Mensual DEFAULT (0),
        PlazoDias           INT NOT NULL CONSTRAINT DF_HqMercPedido_Plazo DEFAULT (7),
        ValidoHasta         DATETIME2 NOT NULL,
        CondicionesPago     NVARCHAR(500) NULL,
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercPedido_Estado DEFAULT (N'Borrador'),
        FechaEnvio          DATETIME2 NULL,
        FechaVisto          DATETIME2 NULL,
        FechaRespuesta      DATETIME2 NULL,
        RespuestaNombre     NVARCHAR(150) NULL,
        RespuestaComentario NVARCHAR(1000) NULL,
        FechaPago           DATETIME2 NULL,
        UsuarioNombre       NVARCHAR(150) NULL,
        FechaCreacion       DATETIME2 NOT NULL CONSTRAINT DF_HqMercPedido_Cre DEFAULT (SYSUTCDATETIME()),
        Activo              BIT NOT NULL CONSTRAINT DF_HqMercPedido_Activo DEFAULT (1),
        CONSTRAINT FK_HqMercPedido_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_HqMercPedido_Nec FOREIGN KEY (NecesidadId) REFERENCES dbo.HqMercadoNecesidades(NecesidadId),
        CONSTRAINT CK_HqMercPedido_Estado CHECK (Estado IN (N'Borrador', N'Enviado', N'Visto', N'Aceptado', N'Rechazado', N'Pagado', N'Vencido'))
    );
    CREATE UNIQUE INDEX UX_HqMercPedido_Token ON dbo.HqMercadoPedidos (Token);
    CREATE INDEX IX_HqMercPedido_Empresa ON dbo.HqMercadoPedidos (EmpresaId, Activo, Estado);
    PRINT 'HqMercadoPedidos creada';
END
GO

/* ========== v4: CONTACTO OFICIAL Y FUENTES BRASIL / REINO UNIDO ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoOrigen') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoOrigen NVARCHAR(60) NULL;   -- de dónde salió el contacto (publicado por la entidad, cargado a mano)
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo' AND definition NOT LIKE N'%PNCP%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'PNCP', N'ContractsFinder', N'RSS'));
    PRINT 'Tipos de fuente: PNCP y ContractsFinder habilitados';
END
GO

DECLARE @Emp4 INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp4 IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    SELECT @Emp4, v.Tipo, v.Nombre, v.Consulta
    FROM (VALUES
        (N'PNCP', N'Licitações Brasil (PNCP) · pregão eletrônico', N'6'),
        (N'PNCP', N'Licitações Brasil (PNCP) · compra direta (dispensa)', N'8'),
        (N'ContractsFinder', N'Public tenders UK (Contracts Finder) · software', N'software'),
        (N'ContractsFinder', N'Public tenders UK (Contracts Finder) · digital platform', N'digital platform')
    ) AS v(Tipo, Nombre, Consulta)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp4 AND f.Tipo = v.Tipo AND ISNULL(f.Consulta, N'') = ISNULL(v.Consulta, N''));
    PRINT CONCAT('Fuentes Brasil / Reino Unido agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v5: SUDAMÉRICA (PERÚ, URUGUAY, CHILE, PNUD) ========== */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo' AND definition NOT LIKE N'%SEACE%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'PNCP', N'ContractsFinder',
                        N'SEACE', N'ARCE', N'PNUD', N'MercadoPublico', N'RSS'));
    PRINT 'Tipos de fuente: SEACE, ARCE, PNUD y MercadoPublico habilitados';
END
GO

DECLARE @Emp5 INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp5 IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta, Activa)
    SELECT @Emp5, v.Tipo, v.Nombre, v.Consulta, v.Activa
    FROM (VALUES
        (N'SEACE', N'Licitaciones Perú (SEACE · OECE)', N'software,sistema,aplicativo,plataforma', 1),
        (N'ARCE', N'Licitaciones Uruguay (ARCE · compras estatales)', NULL, 1),
        (N'PNUD', N'PNUD Naciones Unidas · Sudamérica (incluye Paraguay, Argentina, Bolivia, Venezuela)', NULL, 1),
        -- Chile necesita un ticket gratuito de api.mercadopublico.cl: se activa al cargarlo en la consulta
        (N'MercadoPublico', N'Licitaciones Chile (Mercado Público)', NULL, 0)
    ) AS v(Tipo, Nombre, Consulta, Activa)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp5 AND f.Tipo = v.Tipo);
    PRINT CONCAT('Fuentes de Sudamérica agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v7: ESTUDIO DE PRECIOS (Van Westendorp + Gabor-Granger) ========== */
IF OBJECT_ID(N'dbo.HqEstudioPreciosRespuestas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqEstudioPreciosRespuestas (
        RespuestaId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqEstudioPrecios PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Producto          NVARCHAR(20) NOT NULL,
        Origen            NVARCHAR(60) NULL,
        Tamano            INT NULL,
        Ciudad            NVARCHAR(80) NULL,
        HerramientaActual NVARCHAR(40) NULL,
        GastoActualGs     DECIMAL(18,2) NULL,
        ProblemaPrincipal NVARCHAR(80) NULL,
        Decisor           NVARCHAR(20) NULL,
        MuyBaratoGs       DECIMAL(18,2) NOT NULL,
        BaratoGs          DECIMAL(18,2) NOT NULL,
        CaroGs            DECIMAL(18,2) NOT NULL,
        MuyCaroGs         DECIMAL(18,2) NOT NULL,
        PrecioMostradoGs  DECIMAL(18,2) NOT NULL,
        IntencionCompra   INT NOT NULL,
        Comentario        NVARCHAR(1000) NULL,
        ContactoNombre    NVARCHAR(150) NULL,
        ContactoEmpresa   NVARCHAR(150) NULL,
        ContactoDato      NVARCHAR(150) NULL,
        AceptaContacto    BIT NOT NULL CONSTRAINT DF_HqEstudioPrecios_Acepta DEFAULT (0),
        Fecha             DATETIME2 NOT NULL CONSTRAINT DF_HqEstudioPrecios_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_HqEstudioPrecios_Producto ON dbo.HqEstudioPreciosRespuestas (EmpresaId, Producto, Fecha);
    PRINT 'Tabla HqEstudioPreciosRespuestas creada';
END
GO

/* ========== v8: SECUENCIAS DE CORREO CON IA ========== */
IF OBJECT_ID(N'dbo.HqSecuencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqSecuencias (
        SecuenciaId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqSecuencias PRIMARY KEY,
        EmpresaId      INT NOT NULL,
        Nombre         NVARCHAR(120) NOT NULL,
        Producto       NVARCHAR(20) NOT NULL,
        Oferta         NVARCHAR(1000) NOT NULL,
        Firma          NVARCHAR(120) NOT NULL,
        ResponderA     NVARCHAR(150) NULL,
        Pasos          INT NOT NULL CONSTRAINT DF_HqSecuencias_Pasos DEFAULT (3),
        DiasEntrePasos INT NOT NULL CONSTRAINT DF_HqSecuencias_Dias DEFAULT (3),
        LimiteDiario   INT NOT NULL CONSTRAINT DF_HqSecuencias_Limite DEFAULT (30),
        Activa         BIT NOT NULL CONSTRAINT DF_HqSecuencias_Activa DEFAULT (0),
        FechaCreacion  DATETIME2 NOT NULL CONSTRAINT DF_HqSecuencias_Fecha DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Tabla HqSecuencias creada';
END
GO

IF OBJECT_ID(N'dbo.HqSecuenciaContactos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqSecuenciaContactos (
        ContactoId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqSecuenciaContactos PRIMARY KEY,
        EmpresaId    INT NOT NULL,
        SecuenciaId  INT NOT NULL CONSTRAINT FK_HqSecContactos_Secuencia REFERENCES dbo.HqSecuencias(SecuenciaId),
        ClienteId    INT NULL,
        NecesidadId  INT NULL,
        Email        NVARCHAR(150) NOT NULL,
        Nombre       NVARCHAR(150) NULL,
        Empresa      NVARCHAR(200) NULL,
        Contexto     NVARCHAR(2000) NULL,
        Estado       NVARCHAR(20) NOT NULL CONSTRAINT DF_HqSecContactos_Estado DEFAULT (N'Borrador'),
        PasoActual   INT NOT NULL CONSTRAINT DF_HqSecContactos_Paso DEFAULT (0),
        Asunto       NVARCHAR(200) NULL,
        Cuerpo       NVARCHAR(MAX) NULL,
        ProximoEnvio DATETIME2 NULL,
        UltimoEnvio  DATETIME2 NULL,
        Detalle      NVARCHAR(300) NULL,
        FechaAlta    DATETIME2 NOT NULL CONSTRAINT DF_HqSecContactos_Alta DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_HqSecContactos_Email ON dbo.HqSecuenciaContactos (SecuenciaId, Email);
    CREATE INDEX IX_HqSecContactos_Proximo ON dbo.HqSecuenciaContactos (Estado, ProximoEnvio);
    PRINT 'Tabla HqSecuenciaContactos creada';
END
GO

IF OBJECT_ID(N'dbo.HqSecuenciaEnvios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqSecuenciaEnvios (
        EnvioId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqSecuenciaEnvios PRIMARY KEY,
        EmpresaId   INT NOT NULL,
        SecuenciaId INT NOT NULL,
        ContactoId  INT NOT NULL,
        Paso        INT NOT NULL,
        Asunto      NVARCHAR(200) NOT NULL,
        Cuerpo      NVARCHAR(MAX) NOT NULL,
        Resultado   NVARCHAR(20) NOT NULL,
        Detalle     NVARCHAR(300) NULL,
        Fecha       DATETIME2 NOT NULL CONSTRAINT DF_HqSecEnvios_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_HqSecEnvios_Secuencia ON dbo.HqSecuenciaEnvios (SecuenciaId, Fecha);
    PRINT 'Tabla HqSecuenciaEnvios creada';
END
GO

PRINT '=== VORDIA HQ · Necesidades del Mercado listo ===';
GO

/* ======================================================================
   VORDIA CONTA: estudios contables  (Create_Vordia_Conta.sql)
   ====================================================================== */
PRINT '--- VORDIA CONTA: estudios contables ---';
GO
/*
  VORDIA CONTA · Estudios contables: clientes (RUC), comprobantes, exportaciones RG90
  y el estudio de demostración (rubro CONTA). Se puede correr más de una vez.
*/
USE GESCO_DB;
GO
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ContaClientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaClientes (
        ClienteId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaClientes PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        RazonSocial      NVARCHAR(250) NOT NULL,
        Ruc              NVARCHAR(20) NOT NULL,
        Dv               NVARCHAR(1) NOT NULL,
        Regimen          NVARCHAR(30) NULL,
        ObligacionIva    BIT NOT NULL CONSTRAINT DF_ContaCli_Iva DEFAULT (1),
        ObligacionIre    BIT NOT NULL CONSTRAINT DF_ContaCli_Ire DEFAULT (1),
        ObligacionIrp    BIT NOT NULL CONSTRAINT DF_ContaCli_Irp DEFAULT (0),
        ContactoNombre   NVARCHAR(150) NULL,
        ContactoWhatsapp NVARCHAR(40) NULL,
        ContactoEmail    NVARCHAR(150) NULL,
        TokenPortal      NVARCHAR(40) NOT NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_ContaCli_Activo DEFAULT (1),
        FechaAlta        DATETIME2 NOT NULL CONSTRAINT DF_ContaCli_Alta DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_ContaClientes_Empresa ON dbo.ContaClientes (EmpresaId, Ruc);
    CREATE UNIQUE INDEX UX_ContaClientes_Token ON dbo.ContaClientes (TokenPortal);
    PRINT 'Tabla ContaClientes creada';
END
GO

IF OBJECT_ID(N'dbo.ContaComprobantes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaComprobantes (
        ComprobanteId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaComprobantes PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        ClienteId         INT NOT NULL CONSTRAINT FK_ContaComp_Cliente REFERENCES dbo.ContaClientes(ClienteId),
        Tipo              NVARCHAR(10) NOT NULL,
        Origen            NVARCHAR(20) NOT NULL,
        Electronico       BIT NOT NULL CONSTRAINT DF_ContaComp_Elec DEFAULT (0),
        Cdc               NVARCHAR(44) NULL,
        TipoComprobante   INT NOT NULL CONSTRAINT DF_ContaComp_TipoComp DEFAULT (109),
        Timbrado          NVARCHAR(8) NOT NULL CONSTRAINT DF_ContaComp_Timb DEFAULT (N''),
        Numero            NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Num DEFAULT (N''),
        Fecha             DATETIME2 NOT NULL,
        Credito           BIT NOT NULL CONSTRAINT DF_ContaComp_Cred DEFAULT (0),
        ContraparteTipoId INT NOT NULL CONSTRAINT DF_ContaComp_TipoId DEFAULT (11),
        ContraparteDoc    NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Doc DEFAULT (N''),
        ContraparteDv     NVARCHAR(1) NULL,
        ContraparteNombre NVARCHAR(250) NULL,
        Gravado10         DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_G10 DEFAULT (0),
        Gravado5          DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_G5 DEFAULT (0),
        Exento            DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_Exe DEFAULT (0),
        Total             DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_Tot DEFAULT (0),
        Iva10             DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_I10 DEFAULT (0),
        Iva5              DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_I5 DEFAULT (0),
        NumeroAsociado    NVARCHAR(20) NULL,
        TimbradoAsociado  NVARCHAR(8) NULL,
        ImputaIva         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIva DEFAULT (1),
        ImputaIre         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIre DEFAULT (0),
        ImputaIrp         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIrp DEFAULT (0),
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Estado DEFAULT (N'Pendiente'),
        Observaciones     NVARCHAR(1000) NULL,
        Periodo           INT NOT NULL,
        ArchivoNombre     NVARCHAR(150) NULL,
        CargadoPor        NVARCHAR(150) NULL,
        FechaCarga        DATETIME2 NOT NULL CONSTRAINT DF_ContaComp_Carga DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Tabla ContaComprobantes creada';
END
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_ClientePeriodo')
    CREATE INDEX IX_ContaComp_ClientePeriodo ON dbo.ContaComprobantes (ClienteId, Periodo, Tipo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_EmpresaPeriodo')
    CREATE INDEX IX_ContaComp_EmpresaPeriodo ON dbo.ContaComprobantes (EmpresaId, Periodo, Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_Cdc')
    CREATE INDEX IX_ContaComp_Cdc ON dbo.ContaComprobantes (ClienteId, Cdc) WHERE Cdc IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.ContaExportaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaExportaciones (
        ExportacionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaExportaciones PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        ClienteId     INT NOT NULL,
        Periodo       INT NOT NULL,
        Archivo       NVARCHAR(60) NOT NULL,
        Filas         INT NOT NULL,
        GeneradoPor   NVARCHAR(150) NULL,
        Fecha         DATETIME2 NOT NULL CONSTRAINT DF_ContaExp_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_ContaExp_Cliente ON dbo.ContaExportaciones (ClienteId, Periodo);
    PRINT 'Tabla ContaExportaciones creada';
END
GO

/* Estudio de demostración: el usuario se crea desde HQ → Usuarios con el entorno "Estudio Contable Demo" */
IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Rubro = N'CONTA')
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Estudio Contable Demo', N'80000000-0', N'Paraguay', N'CONTA');
    PRINT 'Empresa demo CONTA creada';
END
GO

PRINT '=== VORDIA CONTA listo ===';
GO

/* ======================================================================
   VORDIA BOUTIQUE + VORDIA CLOSET (marketplace)
   ====================================================================== */
/*
  VORDIA BOUTIQUE · Gestión de boutiques (prendas con talle y color, stock, caja con apartados,
  alquileres por fecha, pruebas y clientas) y VORDIA CLOSET, el marketplace que junta el stock
  de todas las boutiques de Asunción y Gran Asunción. Se puede correr más de una vez.
*/
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

EXEC sp_set_session_context @key = N'vordia_sin_demo', @value = NULL;
PRINT '=== Actualización VORDIA octubre 2026 — fin ===';
GO
