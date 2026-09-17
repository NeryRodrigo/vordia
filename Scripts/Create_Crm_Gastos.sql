/*
  VORDIA HQ — Gastos y cuentas por pagar
  =================================================================
  Crea dbo.CrmGastos, la tabla que le faltaba al dashboard ejecutivo
  para mostrar con datos reales:

    - "Gastos pagados"   -> gastos con FechaPago dentro del mes
    - "Debemos pagar"    -> gastos en estado Pendiente
    - "Vence en los proximos 7 dias"
    - "Ganancia o perdida" y "Dinero disponible"

  Una sola tabla cubre el gasto ya pagado y la cuenta por pagar:
  mientras Estado = 'Pendiente' cuenta como deuda, y al pagarse se
  carga FechaPago y pasa a 'Pagado'.

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
IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmGastos
    (
        GastoId          INT IDENTITY(1, 1) NOT NULL,
        EmpresaId        INT            NOT NULL,
        Concepto         NVARCHAR(200)  NOT NULL,
        Categoria        NVARCHAR(40)   NOT NULL CONSTRAINT DF_CrmGastos_Categoria DEFAULT (N'Operativo'),
        MontoGs          DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CrmGastos_Monto DEFAULT (0),
        FechaEmision     DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmGastos_FechaEmision DEFAULT (SYSUTCDATETIME()),
        FechaVencimiento DATETIME2(7)   NULL,
        FechaPago        DATETIME2(7)   NULL,
        Estado           NVARCHAR(20)   NOT NULL CONSTRAINT DF_CrmGastos_Estado DEFAULT (N'Pendiente'),
        ProveedorId      INT            NULL,
        ProveedorNombre  NVARCHAR(200)  NULL,
        MetodoPago       NVARCHAR(40)   NULL,
        NroComprobante   NVARCHAR(60)   NULL,
        Notas            NVARCHAR(500)  NULL,
        UsuarioNombre    NVARCHAR(150)  NULL,
        Activo           BIT            NOT NULL CONSTRAINT DF_CrmGastos_Activo DEFAULT (1),
        FechaCreacion    DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmGastos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CrmGastos PRIMARY KEY CLUSTERED (GastoId)
    );
    PRINT 'dbo.CrmGastos creada';
END
ELSE PRINT 'dbo.CrmGastos ya existe';
GO

-------------------------------------------------------------
-- 2) Reglas de dominio
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmGastos_Estado')
BEGIN
    ALTER TABLE dbo.CrmGastos
        ADD CONSTRAINT CK_CrmGastos_Estado
            CHECK (Estado IN (N'Pendiente', N'Pagado', N'Anulado'));
    PRINT 'CK_CrmGastos_Estado creado';
END
ELSE PRINT 'CK_CrmGastos_Estado ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmGastos_Monto')
BEGIN
    ALTER TABLE dbo.CrmGastos
        ADD CONSTRAINT CK_CrmGastos_Monto CHECK (MontoGs >= 0);
    PRINT 'CK_CrmGastos_Monto creado';
END
ELSE PRINT 'CK_CrmGastos_Monto ya existe u omitido';
GO

-------------------------------------------------------------
-- 3) Indices de consulta
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmGastos_Empresa_Estado' AND object_id = OBJECT_ID(N'dbo.CrmGastos'))
BEGIN
    CREATE INDEX IX_CrmGastos_Empresa_Estado
        ON dbo.CrmGastos (EmpresaId, Activo, Estado)
        INCLUDE (MontoGs, FechaVencimiento, FechaPago);
    PRINT 'IX_CrmGastos_Empresa_Estado creado';
END
ELSE PRINT 'IX_CrmGastos_Empresa_Estado ya existe';
GO

IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmGastos_Vencimiento' AND object_id = OBJECT_ID(N'dbo.CrmGastos'))
BEGIN
    CREATE INDEX IX_CrmGastos_Vencimiento
        ON dbo.CrmGastos (EmpresaId, FechaVencimiento)
        WHERE Activo = 1;
    PRINT 'IX_CrmGastos_Vencimiento creado';
END
ELSE PRINT 'IX_CrmGastos_Vencimiento ya existe';
GO

-------------------------------------------------------------
-- 4) Verificacion
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NOT NULL
BEGIN
    SELECT
        Columna      = c.name,
        Tipo         = t.name,
        Longitud     = c.max_length,
        Permite_Null = c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CrmGastos')
    ORDER BY c.column_id;
END
GO

PRINT '=== CrmGastos lista. Reiniciar la API para que EF tome la tabla. ===';
GO
