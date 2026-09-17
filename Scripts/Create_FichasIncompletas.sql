/*
  Condominio Agus — Fichas incompletas (borradores de ficha de inquilino)
  Ejecutar en GESCO_DB (idempotente).
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.FichasIncompletas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FichasIncompletas (
        FichaIncompletaId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FichasIncompletas PRIMARY KEY,
        EmpresaId           INT NOT NULL,
        UnidadId            INT NOT NULL,
        CodigoUnidad        NVARCHAR(50) NULL,
        NombreCondominio    NVARCHAR(150) NULL,
        NombreInquilino     NVARCHAR(200) NULL,
        Modo                NVARCHAR(20) NOT NULL CONSTRAINT DF_FichasIncompletas_Modo DEFAULT (N'edit'),
        PayloadJson         NVARCHAR(MAX) NOT NULL,
        UsuarioId           INT NULL,
        UsuarioNombre       NVARCHAR(150) NULL,
        Activo              BIT NOT NULL CONSTRAINT DF_FichasIncompletas_Activo DEFAULT (1),
        FechaCreacion       DATETIME2 NOT NULL CONSTRAINT DF_FichasIncompletas_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion  DATETIME2 NOT NULL CONSTRAINT DF_FichasIncompletas_Update DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_FichasIncompletas_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT UQ_FichasIncompletas_EmpresaUnidad UNIQUE (EmpresaId, UnidadId)
    );
    CREATE INDEX IX_FichasIncompletas_Empresa ON dbo.FichasIncompletas (EmpresaId, Activo, FechaActualizacion DESC);
    PRINT 'Tabla FichasIncompletas creada';
END
ELSE
    PRINT 'Tabla FichasIncompletas ya existe';
GO
