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
