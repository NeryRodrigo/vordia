/*
  VORDIA HQ — Documentos y Automatizaciones
  =================================================================
  Crea tres tablas:

    dbo.CrmDocumentos              Repositorio de archivos y enlaces
                                   del CRM (contratos firmados, actas,
                                   propuestas, facturas), con fecha de
                                   vencimiento opcional para los que
                                   caducan.

    dbo.CrmAutomatizaciones        Reglas del motor "Automation First":
                                   un disparador + una accion. Nacen
                                   pausadas y las enciende el usuario.

    dbo.CrmAutomatizacionEjecuciones
                                   Bitacora de cada disparo. La clave
                                   unica evita que la misma regla avise
                                   dos veces por el mismo hecho.

  Los archivos NO se guardan en la base: en la tabla queda la ruta
  relativa (/crm-documentos/...) y el binario vive en wwwroot del API.

  Script idempotente: se puede correr varias veces sin efecto adverso.

  Uso: SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-------------------------------------------------------------
-- 1) Documentos del CRM
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmDocumentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmDocumentos
    (
        DocumentoId        INT IDENTITY(1, 1) NOT NULL,
        EmpresaId          INT            NOT NULL,
        ClienteId          INT            NULL,
        ProyectoId         INT            NULL,
        Titulo             NVARCHAR(200)  NOT NULL,
        Categoria          NVARCHAR(40)   NOT NULL CONSTRAINT DF_CrmDocumentos_Categoria DEFAULT (N'Otro'),
        NombreArchivo      NVARCHAR(260)  NULL,
        Url                NVARCHAR(500)  NOT NULL,
        EsEnlaceExterno    BIT            NOT NULL CONSTRAINT DF_CrmDocumentos_Enlace DEFAULT (0),
        Extension          NVARCHAR(12)   NULL,
        TamanoBytes        BIGINT         NOT NULL CONSTRAINT DF_CrmDocumentos_Tamano DEFAULT (0),
        FechaVencimiento   DATE           NULL,
        Notas              NVARCHAR(500)  NULL,
        SubidoPor          NVARCHAR(150)  NULL,
        Activo             BIT            NOT NULL CONSTRAINT DF_CrmDocumentos_Activo DEFAULT (1),
        FechaCreacion      DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmDocumentos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2(7)   NULL,
        CONSTRAINT PK_CrmDocumentos PRIMARY KEY CLUSTERED (DocumentoId)
    );
    PRINT 'dbo.CrmDocumentos creada';
END
ELSE PRINT 'dbo.CrmDocumentos ya existe';
GO

IF OBJECT_ID(N'dbo.CrmDocumentos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmDocumentos_Categoria')
BEGIN
    ALTER TABLE dbo.CrmDocumentos
        ADD CONSTRAINT CK_CrmDocumentos_Categoria
            CHECK (Categoria IN (N'Contrato', N'Propuesta', N'Acta', N'Legal', N'Factura', N'Tecnico', N'Otro'));
    PRINT 'CK_CrmDocumentos_Categoria creado';
END
ELSE PRINT 'CK_CrmDocumentos_Categoria ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmDocumentos', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CrmClientes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CrmDocumentos_Cliente')
BEGIN
    ALTER TABLE dbo.CrmDocumentos
        ADD CONSTRAINT FK_CrmDocumentos_Cliente
            FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes (ClienteId);
    PRINT 'FK_CrmDocumentos_Cliente creado';
END
ELSE PRINT 'FK_CrmDocumentos_Cliente ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmDocumentos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmDocumentos_Empresa_Categoria' AND object_id = OBJECT_ID(N'dbo.CrmDocumentos'))
BEGIN
    CREATE INDEX IX_CrmDocumentos_Empresa_Categoria
        ON dbo.CrmDocumentos (EmpresaId, Activo, Categoria)
        INCLUDE (ClienteId, Titulo, FechaVencimiento);
    PRINT 'IX_CrmDocumentos_Empresa_Categoria creado';
END
ELSE PRINT 'IX_CrmDocumentos_Empresa_Categoria ya existe';
GO

-- Los documentos que caducan se consultan por fecha en cada corrida del motor
IF OBJECT_ID(N'dbo.CrmDocumentos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmDocumentos_Vencimiento' AND object_id = OBJECT_ID(N'dbo.CrmDocumentos'))
BEGIN
    CREATE INDEX IX_CrmDocumentos_Vencimiento
        ON dbo.CrmDocumentos (EmpresaId, FechaVencimiento)
        WHERE Activo = 1 AND FechaVencimiento IS NOT NULL;
    PRINT 'IX_CrmDocumentos_Vencimiento creado';
END
ELSE PRINT 'IX_CrmDocumentos_Vencimiento ya existe';
GO

-------------------------------------------------------------
-- 2) Reglas de automatizacion
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmAutomatizaciones
    (
        AutomatizacionId   INT IDENTITY(1, 1) NOT NULL,
        EmpresaId          INT            NOT NULL,
        Nombre             NVARCHAR(150)  NOT NULL,
        Descripcion        NVARCHAR(500)  NULL,
        Disparador         NVARCHAR(50)   NOT NULL,
        DiasAnticipacion   INT            NOT NULL CONSTRAINT DF_CrmAutomatizaciones_Dias DEFAULT (5),
        Accion             NVARCHAR(30)   NOT NULL CONSTRAINT DF_CrmAutomatizaciones_Accion DEFAULT (N'email'),
        EmailDestino       NVARCHAR(250)  NULL,
        AsuntoPlantilla    NVARCHAR(200)  NULL,
        MensajePlantilla   NVARCHAR(MAX)  NULL,
        -- Activa = encendida o pausada por el usuario. Activo = no eliminada.
        Activa             BIT            NOT NULL CONSTRAINT DF_CrmAutomatizaciones_Activa DEFAULT (0),
        Activo             BIT            NOT NULL CONSTRAINT DF_CrmAutomatizaciones_Activo DEFAULT (1),
        UltimaEjecucion    DATETIME2(7)   NULL,
        UltimoResultado    NVARCHAR(400)  NULL,
        VecesDisparada     INT            NOT NULL CONSTRAINT DF_CrmAutomatizaciones_Veces DEFAULT (0),
        CreadoPor          NVARCHAR(150)  NULL,
        FechaCreacion      DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmAutomatizaciones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2(7)   NULL,
        CONSTRAINT PK_CrmAutomatizaciones PRIMARY KEY CLUSTERED (AutomatizacionId)
    );
    PRINT 'dbo.CrmAutomatizaciones creada';
END
ELSE PRINT 'dbo.CrmAutomatizaciones ya existe';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmAutomatizaciones_Disparador')
BEGIN
    ALTER TABLE dbo.CrmAutomatizaciones
        ADD CONSTRAINT CK_CrmAutomatizaciones_Disparador
            CHECK (Disparador IN (
                N'documento_por_vencer',
                N'contrato_por_cobrar',
                N'contrato_vencido',
                N'tarea_por_vencer',
                N'propuesta_sin_respuesta',
                N'lead_web_sin_contactar'));
    PRINT 'CK_CrmAutomatizaciones_Disparador creado';
END
ELSE PRINT 'CK_CrmAutomatizaciones_Disparador ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmAutomatizaciones_Accion')
BEGIN
    ALTER TABLE dbo.CrmAutomatizaciones
        ADD CONSTRAINT CK_CrmAutomatizaciones_Accion
            CHECK (Accion IN (N'email', N'tarea', N'email_y_tarea'));
    PRINT 'CK_CrmAutomatizaciones_Accion creado';
END
ELSE PRINT 'CK_CrmAutomatizaciones_Accion ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmAutomatizaciones_Dias')
BEGIN
    ALTER TABLE dbo.CrmAutomatizaciones
        ADD CONSTRAINT CK_CrmAutomatizaciones_Dias CHECK (DiasAnticipacion BETWEEN 0 AND 365);
    PRINT 'CK_CrmAutomatizaciones_Dias creado';
END
ELSE PRINT 'CK_CrmAutomatizaciones_Dias ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmAutomatizaciones_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmAutomatizaciones'))
BEGIN
    CREATE INDEX IX_CrmAutomatizaciones_Empresa
        ON dbo.CrmAutomatizaciones (EmpresaId, Activo, Activa)
        INCLUDE (Disparador, Accion, DiasAnticipacion);
    PRINT 'IX_CrmAutomatizaciones_Empresa creado';
END
ELSE PRINT 'IX_CrmAutomatizaciones_Empresa ya existe';
GO

-------------------------------------------------------------
-- 3) Bitacora de ejecuciones
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmAutomatizacionEjecuciones
    (
        EjecucionId      INT IDENTITY(1, 1) NOT NULL,
        AutomatizacionId INT             NOT NULL,
        EmpresaId        INT             NOT NULL,
        -- Identifica el hecho concreto que disparo la regla, por ejemplo
        -- "documento:41:2026-10-15". Es lo que evita el aviso repetido.
        ClaveUnica       NVARCHAR(200)   NOT NULL,
        Resultado        NVARCHAR(20)    NOT NULL,
        Referencia       NVARCHAR(200)   NULL,
        Detalle          NVARCHAR(1000)  NULL,
        Fecha            DATETIME2(7)    NOT NULL CONSTRAINT DF_CrmAutoEjec_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CrmAutomatizacionEjecuciones PRIMARY KEY CLUSTERED (EjecucionId)
    );
    PRINT 'dbo.CrmAutomatizacionEjecuciones creada';
END
ELSE PRINT 'dbo.CrmAutomatizacionEjecuciones ya existe';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmAutoEjec_Resultado')
BEGIN
    ALTER TABLE dbo.CrmAutomatizacionEjecuciones
        ADD CONSTRAINT CK_CrmAutoEjec_Resultado
            CHECK (Resultado IN (N'email', N'tarea', N'email_y_tarea', N'error', N'omitido'));
    PRINT 'CK_CrmAutoEjec_Resultado creado';
END
ELSE PRINT 'CK_CrmAutoEjec_Resultado ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CrmAutomatizaciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CrmAutoEjec_Automatizacion')
BEGIN
    ALTER TABLE dbo.CrmAutomatizacionEjecuciones
        ADD CONSTRAINT FK_CrmAutoEjec_Automatizacion
            FOREIGN KEY (AutomatizacionId) REFERENCES dbo.CrmAutomatizaciones (AutomatizacionId);
    PRINT 'FK_CrmAutoEjec_Automatizacion creado';
END
ELSE PRINT 'FK_CrmAutoEjec_Automatizacion ya existe u omitido';
GO

-- Unico solo para los disparos exitosos: si algo falla, el motor puede reintentarlo
IF OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CrmAutoEjec_Clave' AND object_id = OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones'))
BEGIN
    CREATE UNIQUE INDEX UX_CrmAutoEjec_Clave
        ON dbo.CrmAutomatizacionEjecuciones (AutomatizacionId, ClaveUnica)
        WHERE Resultado <> N'error';
    PRINT 'UX_CrmAutoEjec_Clave creado';
END
ELSE PRINT 'UX_CrmAutoEjec_Clave ya existe';
GO

IF OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmAutoEjec_Empresa_Fecha' AND object_id = OBJECT_ID(N'dbo.CrmAutomatizacionEjecuciones'))
BEGIN
    CREATE INDEX IX_CrmAutoEjec_Empresa_Fecha
        ON dbo.CrmAutomatizacionEjecuciones (EmpresaId, Fecha DESC)
        INCLUDE (AutomatizacionId, Resultado, Referencia);
    PRINT 'IX_CrmAutoEjec_Empresa_Fecha creado';
END
ELSE PRINT 'IX_CrmAutoEjec_Empresa_Fecha ya existe';
GO

-------------------------------------------------------------
-- 4) Reglas de ejemplo, PAUSADAS
--    Nacen apagadas a proposito: nadie manda correos hasta que
--    el usuario revise el texto y la active desde Vordia HQ.
-------------------------------------------------------------
DECLARE @EmpresaId INT = (
    SELECT TOP 1 EmpresaId
    FROM dbo.Empresas
    WHERE Rubro = N'VORDIA'
    ORDER BY EmpresaId
);

IF @EmpresaId IS NULL
    PRINT 'Empresa Vordia no encontrada: se omiten las reglas de ejemplo';
ELSE IF EXISTS (SELECT 1 FROM dbo.CrmAutomatizaciones WHERE EmpresaId = @EmpresaId)
    PRINT 'Ya hay automatizaciones cargadas: se omiten las reglas de ejemplo';
ELSE
BEGIN
    INSERT INTO dbo.CrmAutomatizaciones
        (EmpresaId, Nombre, Descripcion, Disparador, DiasAnticipacion, Accion, AsuntoPlantilla, MensajePlantilla, Activa, CreadoPor)
    VALUES
        (@EmpresaId,
         N'Aviso de documento por vencer',
         N'Avisa por correo cuando a un documento del repositorio le faltan pocos dias para caducar.',
         N'documento_por_vencer', 15, N'email',
         N'Documento por vencer: {referencia}',
         N'El documento {referencia} vence el {fecha} ({dias} dias). Revisalo en Vordia HQ.',
         0, N'Script de instalacion'),

        (@EmpresaId,
         N'Recordatorio de cobro de contrato',
         N'Crea la tarea de cobranza y avisa por correo antes de la fecha de cobro de cada contrato activo.',
         N'contrato_por_cobrar', 5, N'email_y_tarea',
         N'Cobro proximo: {referencia}',
         N'El contrato {referencia} se cobra el {fecha} ({dias} dias). Monto: {monto}.',
         0, N'Script de instalacion'),

        (@EmpresaId,
         N'Lead web sin contactar',
         N'Avisa cuando una solicitud entrante de vordia.com.py sigue en Prospecto despues de los dias indicados.',
         N'lead_web_sin_contactar', 2, N'email_y_tarea',
         N'Lead sin contactar: {referencia}',
         N'{referencia} dejo sus datos en la web hace {dias} dias y sigue sin contacto.',
         0, N'Script de instalacion');

    PRINT 'Tres automatizaciones de ejemplo cargadas (pausadas)';
END
GO

-------------------------------------------------------------
-- 5) Verificacion
-------------------------------------------------------------
SELECT Tabla = N'CrmDocumentos', Registros = COUNT(*) FROM dbo.CrmDocumentos
UNION ALL
SELECT Tabla = N'CrmAutomatizaciones', Registros = COUNT(*) FROM dbo.CrmAutomatizaciones
UNION ALL
SELECT Tabla = N'CrmAutomatizacionEjecuciones', Registros = COUNT(*) FROM dbo.CrmAutomatizacionEjecuciones;
GO

SELECT AutomatizacionId, Nombre, Disparador, Accion, DiasAnticipacion, Activa
FROM dbo.CrmAutomatizaciones
ORDER BY AutomatizacionId;
GO

PRINT '=== Documentos y Automatizaciones listos. Reiniciar la API para que EF tome las tablas. ===';
GO
