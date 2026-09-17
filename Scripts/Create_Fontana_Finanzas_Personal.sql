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
