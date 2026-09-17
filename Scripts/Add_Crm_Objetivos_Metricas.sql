/*
  VORDIA HQ — Métricas de avance para Objetivos
  =================================================================
  Agrega a dbo.CrmObjetivos las columnas que el dashboard ejecutivo
  necesita para dibujar el progreso real ("7 de 10", 70%):

    MetaValor    -> el objetivo numérico (ej. 10 clientes nuevos)
    ValorActual  -> lo alcanzado hasta hoy (ej. 7)
    AvancePct    -> avance manual 0..100, para objetivos sin número

  Regla de cálculo en el dashboard:
    - Si MetaValor > 0        -> porcentaje = ValorActual / MetaValor
    - Si no, y hay AvancePct  -> se usa AvancePct
    - Si no                   -> 100% si está Logrado, 0% en otro caso

  Script idempotente: se puede correr varias veces sin efecto adverso.
  Requiere haber ejecutado antes Create_Crm_Hq_Capital_Objetivos.sql.

  Uso: SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NULL
BEGIN
    RAISERROR(N'No existe dbo.CrmObjetivos. Ejecutar antes Create_Crm_Hq_Capital_Objetivos.sql', 16, 1);
END
GO

-------------------------------------------------------------
-- 1) MetaValor
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'MetaValor') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD MetaValor DECIMAL(18, 2) NULL;
    PRINT 'CrmObjetivos.MetaValor agregada';
END
ELSE PRINT 'CrmObjetivos.MetaValor ya existe';
GO

-------------------------------------------------------------
-- 2) ValorActual
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'ValorActual') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD ValorActual DECIMAL(18, 2) NULL;
    PRINT 'CrmObjetivos.ValorActual agregada';
END
ELSE PRINT 'CrmObjetivos.ValorActual ya existe';
GO

-------------------------------------------------------------
-- 3) AvancePct
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'AvancePct') IS NULL
BEGIN
    ALTER TABLE dbo.CrmObjetivos ADD AvancePct INT NULL;
    PRINT 'CrmObjetivos.AvancePct agregada';
END
ELSE PRINT 'CrmObjetivos.AvancePct ya existe';
GO

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmObjetivos', N'AvancePct') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmObj_AvancePct')
BEGIN
    ALTER TABLE dbo.CrmObjetivos
        ADD CONSTRAINT CK_CrmObj_AvancePct CHECK (AvancePct IS NULL OR (AvancePct >= 0 AND AvancePct <= 100));
    PRINT 'CK_CrmObj_AvancePct creado';
END
ELSE PRINT 'CK_CrmObj_AvancePct ya existe u omitido';
GO

-------------------------------------------------------------
-- 4) Verificación
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
BEGIN
    SELECT
        Columna     = c.name,
        Tipo        = t.name,
        Permite_Null = c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CrmObjetivos')
      AND c.name IN (N'MetaValor', N'ValorActual', N'AvancePct')
    ORDER BY c.name;
END
GO

PRINT '=== Métricas de Objetivos listas ===';
GO
