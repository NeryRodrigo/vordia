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
