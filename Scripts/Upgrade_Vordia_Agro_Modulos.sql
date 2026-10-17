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
