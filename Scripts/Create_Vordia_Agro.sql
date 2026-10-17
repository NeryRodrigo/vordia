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
