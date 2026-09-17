/*
  VORDIA HQ — Reiniciar datos operativos a 0 (empezar carga real)
  =================================================================
  Solo afecta la empresa Rubro = VORDIA (ej. "Vordia").
  NO toca: Habitat/Condominio, Fontana (módulo), Stay, Agrofield (Rubro AGRO).

  Qué limpia (demo → vacío para cargar real):
    - Pagos, contratos, clientes + historial CRM
    - Tareas
    - Objetivos (incl. seed)
    - Capital movimientos, empleados HQ
    - Proyectos / proveedores HQ (si existen)
    - Roadmap ideas demo (opcional, ver flag abajo)

  Qué CONSERVA:
    - Empresa Vordia + usuarios (nery/eli)
    - Socios Eli/Nery y % (reparto) — por defecto se conservan
    - Productos del portafolio (HQ/Habitat/Stay/Dine/Retail/Agro)
    - Módulos / licencias

  Uso SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @EmpresaId INT;
DECLARE @LimpiarRoadmap BIT = 1;
DECLARE @Cnt INT;

SELECT TOP 1 @EmpresaId = EmpresaId
FROM dbo.Empresas
WHERE Rubro = N'VORDIA'
ORDER BY EmpresaId;

IF @EmpresaId IS NULL
BEGIN
    RAISERROR(N'No se encontró empresa Rubro=VORDIA.', 16, 1);
    RETURN;
END

PRINT CONCAT('=== Reset HQ empresaId=', @EmpresaId, ' ===');
PRINT '--- Antes ---';

IF OBJECT_ID(N'dbo.CrmPagos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmPagos WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('Pagos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmContratos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmContratos WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Contratos activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmClientes', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmClientes WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Clientes activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmTareas', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmTareas WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Tareas activas: ', @Cnt);
END

BEGIN TRAN;

------------------------------------------------------------
-- 1) Pagos (hard delete)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPagos', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.CrmPagos WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('CrmPagos eliminados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 2) Objetivos
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmObjetivos
    SET Activo = 0, FechaActualizacion = SYSUTCDATETIME()
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmObjetivos desactivados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 3) Tareas
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmTareas', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmTareas
    SET Activo = 0
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmTareas desactivadas: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 4) Contratos
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmContratos', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmContratos
    SET Activo = 0, EstadoContrato = N'Finalizado'
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmContratos desactivados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 5) Historial CRM
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmHistorial', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.CrmHistorial WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('CrmHistorial eliminado: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 6) Clientes pipeline
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmClientes', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmClientes
    SET Activo = 0
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmClientes desactivados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 7) Capital + empleados HQ
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmCapitalMovimientos
    SET Activo = 0
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmCapitalMovimientos desactivados: ', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmEmpleadosHq
    SET Activo = 0, Estado = N'Inactivo', FechaActualizacion = SYSUTCDATETIME()
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmEmpleadosHq desactivados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 8) Proyectos / proveedores
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmProyectos', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmProyectos
    SET Activo = 0, FechaActualizacion = SYSUTCDATETIME()
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmProyectos desactivados: ', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.CrmProveedores', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmProveedores
    SET Activo = 0, FechaActualizacion = SYSUTCDATETIME()
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmProveedores desactivados: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 9) Roadmap demo (@LimpiarRoadmap = 0 para conservar)
------------------------------------------------------------
IF @LimpiarRoadmap = 1 AND OBJECT_ID(N'dbo.CrmRoadmap', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.CrmRoadmapHistorial', N'U') IS NOT NULL
    BEGIN
        DELETE h
        FROM dbo.CrmRoadmapHistorial h
        INNER JOIN dbo.CrmRoadmap r ON r.RoadmapId = h.RoadmapId
        WHERE r.EmpresaId = @EmpresaId;
        PRINT CONCAT('CrmRoadmapHistorial eliminado: ', @@ROWCOUNT);
    END

    UPDATE dbo.CrmRoadmap
    SET Activo = 0
    WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmRoadmap desactivado: ', @@ROWCOUNT);
END

------------------------------------------------------------
-- 10) Socios: por defecto NO se borran (Eli/Nery %).
--     Descomentá el bloque si también querés dejar socios en 0.
------------------------------------------------------------
/*
IF OBJECT_ID(N'dbo.CrmSocios', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.CrmSocios SET Activo = 0 WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('CrmSocios desactivados: ', @@ROWCOUNT);
END
*/

COMMIT TRAN;

PRINT '--- Después (activos) ---';
IF OBJECT_ID(N'dbo.CrmPagos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmPagos WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('Pagos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmContratos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmContratos WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Contratos activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmClientes', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmClientes WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Clientes activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmTareas', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmTareas WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Tareas activas: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmObjetivos WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Objetivos activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmCapitalMovimientos WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Capital activos: ', @Cnt);
END
IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NOT NULL
BEGIN
    SELECT @Cnt = COUNT(*) FROM dbo.CrmEmpleadosHq WHERE EmpresaId = @EmpresaId AND Activo = 1;
    PRINT CONCAT('Empleados activos: ', @Cnt);
END

PRINT '=== HQ listo en 0. Recargá Inicio en el navegador (F5). ===';
PRINT 'Cargá datos reales: Clientes → Contratos → Ingresos → Tareas → Capital/Empleados.';
GO
