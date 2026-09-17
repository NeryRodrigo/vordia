/*
  VORDIA HQ — Propuestas comerciales
  =================================================================
  Crea dbo.CrmPropuestas: las propuestas que se le envian a cada
  cliente, con su monto y si se cerraron o no.

  Regla de negocio (la aplica la API, no la base):
    Estado = 'Ganada'  -> el cliente pasa a 'Cliente activo'
    Estado = 'Perdida' -> el cliente pasa a 'Cerrado'
    Estado = 'Enviada' -> si el cliente venia de Prospecto,
                          Contactado o En negociacion, pasa a
                          'Propuesta enviada'

  Cada cambio de estado del cliente queda registrado en
  dbo.CrmHistorial como 'cambio_estado'.

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
-- 1) Tabla
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmPropuestas
    (
        PropuestaId   INT IDENTITY(1, 1) NOT NULL,
        EmpresaId     INT            NOT NULL,
        ClienteId     INT            NOT NULL,
        Descripcion   NVARCHAR(300)  NOT NULL,
        MontoGs       DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CrmPropuestas_Monto DEFAULT (0),
        Estado        NVARCHAR(20)   NOT NULL CONSTRAINT DF_CrmPropuestas_Estado DEFAULT (N'Enviada'),
        FechaEnvio    DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmPropuestas_FechaEnvio DEFAULT (SYSUTCDATETIME()),
        FechaCierre   DATETIME2(7)   NULL,
        Notas         NVARCHAR(500)  NULL,
        UsuarioNombre NVARCHAR(150)  NULL,
        Activo        BIT            NOT NULL CONSTRAINT DF_CrmPropuestas_Activo DEFAULT (1),
        FechaCreacion DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmPropuestas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CrmPropuestas PRIMARY KEY CLUSTERED (PropuestaId)
    );
    PRINT 'dbo.CrmPropuestas creada';
END
ELSE PRINT 'dbo.CrmPropuestas ya existe';
GO

-------------------------------------------------------------
-- 2) Reglas de dominio
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmPropuestas_Estado')
BEGIN
    ALTER TABLE dbo.CrmPropuestas
        ADD CONSTRAINT CK_CrmPropuestas_Estado
            CHECK (Estado IN (N'Enviada', N'Ganada', N'Perdida'));
    PRINT 'CK_CrmPropuestas_Estado creado';
END
ELSE PRINT 'CK_CrmPropuestas_Estado ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmPropuestas_Monto')
BEGIN
    ALTER TABLE dbo.CrmPropuestas
        ADD CONSTRAINT CK_CrmPropuestas_Monto CHECK (MontoGs >= 0);
    PRINT 'CK_CrmPropuestas_Monto creado';
END
ELSE PRINT 'CK_CrmPropuestas_Monto ya existe u omitido';
GO

-------------------------------------------------------------
-- 3) Relacion con el cliente
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CrmClientes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CrmPropuestas_Cliente')
BEGIN
    ALTER TABLE dbo.CrmPropuestas
        ADD CONSTRAINT FK_CrmPropuestas_Cliente
            FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes (ClienteId);
    PRINT 'FK_CrmPropuestas_Cliente creado';
END
ELSE PRINT 'FK_CrmPropuestas_Cliente ya existe u omitido';
GO

-------------------------------------------------------------
-- 4) Indices de consulta
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmPropuestas_Empresa_Estado' AND object_id = OBJECT_ID(N'dbo.CrmPropuestas'))
BEGIN
    CREATE INDEX IX_CrmPropuestas_Empresa_Estado
        ON dbo.CrmPropuestas (EmpresaId, Activo, Estado)
        INCLUDE (ClienteId, MontoGs, FechaEnvio);
    PRINT 'IX_CrmPropuestas_Empresa_Estado creado';
END
ELSE PRINT 'IX_CrmPropuestas_Empresa_Estado ya existe';
GO

IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmPropuestas_Cliente' AND object_id = OBJECT_ID(N'dbo.CrmPropuestas'))
BEGIN
    CREATE INDEX IX_CrmPropuestas_Cliente
        ON dbo.CrmPropuestas (ClienteId)
        WHERE Activo = 1;
    PRINT 'IX_CrmPropuestas_Cliente creado';
END
ELSE PRINT 'IX_CrmPropuestas_Cliente ya existe';
GO

-------------------------------------------------------------
-- 5) Verificacion
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
BEGIN
    SELECT
        Columna      = c.name,
        Tipo         = t.name,
        Permite_Null = c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CrmPropuestas')
    ORDER BY c.column_id;
END
GO

PRINT '=== CrmPropuestas lista. Reiniciar la API para que EF tome la tabla. ===';
GO
