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
