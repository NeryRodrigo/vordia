/*
  VORDIA HQ — Gastos de ejemplo
  =================================================================
  Carga gastos de muestra en dbo.CrmGastos para que el dashboard
  ejecutivo muestre con datos:

    - "Gastos pagados"        -> los que tienen FechaPago en este mes
    - "Debemos pagar"         -> los que quedan en Pendiente
    - "Vence en los proximos 7 dias"
    - "Gasto vencido" en Prioridades de hoy

  Las fechas son relativas a la fecha de ejecucion, asi que el
  dashboard siempre queda con datos del mes en curso.

  Requiere haber ejecutado antes Create_Crm_Gastos.sql.
  Script idempotente: borra los gastos demo previos antes de insertar.

  Uso: SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.CrmGastos', N'U') IS NULL
BEGIN
    RAISERROR(N'No existe dbo.CrmGastos. Ejecutar antes Create_Crm_Gastos.sql', 16, 1);
    RETURN;
END
GO

DECLARE @EmpresaId INT;
DECLARE @Hoy DATE = CAST(GETDATE() AS DATE);
DECLARE @InicioMes DATE = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);

SELECT TOP 1 @EmpresaId = EmpresaId
FROM dbo.Empresas
WHERE Rubro = N'VORDIA'
ORDER BY EmpresaId;

IF @EmpresaId IS NULL
BEGIN
    RAISERROR(N'No se encontró empresa Rubro=VORDIA.', 16, 1);
    RETURN;
END

PRINT CONCAT('=== Seed gastos HQ empresaId=', @EmpresaId, ' ===');

-- Limpieza de la corrida anterior para poder repetir el script.
DELETE FROM dbo.CrmGastos
WHERE EmpresaId = @EmpresaId
  AND Notas = N'SEED_DEMO';

INSERT INTO dbo.CrmGastos
    (EmpresaId, Concepto, Categoria, MontoGs, FechaEmision, FechaVencimiento, FechaPago, Estado,
     ProveedorNombre, MetodoPago, NroComprobante, Notas, UsuarioNombre, Activo, FechaCreacion)
VALUES
    -- ---------- Ya pagados este mes ----------
    (@EmpresaId, N'Servidor y hosting cloud', N'Infraestructura', 2850000,
     DATEADD(DAY, 1, @InicioMes), DATEADD(DAY, 8, @InicioMes), DATEADD(DAY, 3, @InicioMes), N'Pagado',
     N'Google Cloud', N'Tarjeta', N'FC-001-0001', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Internet y telefonía de oficina', N'Servicios', 780000,
     DATEADD(DAY, 2, @InicioMes), DATEADD(DAY, 10, @InicioMes), DATEADD(DAY, 6, @InicioMes), N'Pagado',
     N'Tigo', N'Débito automático', N'FC-002-0154', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Licencias de software del equipo', N'Operativo', 1450000,
     DATEADD(DAY, 4, @InicioMes), DATEADD(DAY, 12, @InicioMes), DATEADD(DAY, 9, @InicioMes), N'Pagado',
     N'Microsoft', N'Tarjeta', N'FC-003-0088', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Publicidad en redes', N'Marketing', 1200000,
     DATEADD(DAY, 5, @InicioMes), DATEADD(DAY, 15, @InicioMes), DATEADD(DAY, 12, @InicioMes), N'Pagado',
     N'Meta Ads', N'Tarjeta', N'FC-004-0021', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Honorarios contables', N'Honorarios', 900000,
     DATEADD(DAY, 6, @InicioMes), DATEADD(DAY, 16, @InicioMes), DATEADD(DAY, 14, @InicioMes), N'Pagado',
     N'Estudio Contable', N'Transferencia', N'FC-005-0310', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    -- ---------- Vencidos: aparecen en Prioridades de hoy ----------
    (@EmpresaId, N'IVA del mes anterior', N'Impuestos', 3100000,
     DATEADD(DAY, -25, @Hoy), DATEADD(DAY, -4, @Hoy), NULL, N'Pendiente',
     N'SET', NULL, N'DJ-0912', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    -- ---------- Vencen dentro de los próximos 7 días ----------
    (@EmpresaId, N'Alquiler de oficina', N'Operativo', 2500000,
     DATEADD(DAY, -6, @Hoy), DATEADD(DAY, 3, @Hoy), NULL, N'Pendiente',
     N'Inmobiliaria Central', NULL, N'FC-006-0044', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Servicio de energía eléctrica', N'Servicios', 640000,
     DATEADD(DAY, -5, @Hoy), DATEADD(DAY, 5, @Hoy), NULL, N'Pendiente',
     N'ANDE', NULL, N'FC-007-7781', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Consultoría de seguridad', N'Honorarios', 1800000,
     DATEADD(DAY, -3, @Hoy), DATEADD(DAY, 6, @Hoy), NULL, N'Pendiente',
     N'Consultora Externa', NULL, N'FC-008-0012', N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    -- ---------- Pendientes más adelante ----------
    (@EmpresaId, N'Renovación de dominios', N'Infraestructura', 420000,
     @Hoy, DATEADD(DAY, 18, @Hoy), NULL, N'Pendiente',
     N'Registrador', NULL, NULL, N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME()),

    (@EmpresaId, N'Equipos para el equipo de desarrollo', N'Operativo', 5200000,
     @Hoy, DATEADD(DAY, 24, @Hoy), NULL, N'Pendiente',
     N'Distribuidora Tech', NULL, NULL, N'SEED_DEMO', N'Admin', 1, SYSUTCDATETIME());

PRINT CONCAT('Gastos demo insertados: ', @@ROWCOUNT);

-------------------------------------------------------------
-- Verificación: lo mismo que va a mostrar el dashboard
-------------------------------------------------------------
SELECT
    Pagado_Este_Mes = SUM(CASE WHEN Estado = N'Pagado'
                                AND FechaPago >= @InicioMes
                                AND FechaPago < DATEADD(MONTH, 1, @InicioMes)
                               THEN MontoGs ELSE 0 END),
    Debemos_Pagar   = SUM(CASE WHEN Estado = N'Pendiente' THEN MontoGs ELSE 0 END),
    Vencido         = SUM(CASE WHEN Estado = N'Pendiente' AND FechaVencimiento < @Hoy
                               THEN MontoGs ELSE 0 END),
    Vence_7_Dias    = SUM(CASE WHEN Estado = N'Pendiente'
                                AND FechaVencimiento >= @Hoy
                                AND FechaVencimiento <= DATEADD(DAY, 7, @Hoy)
                               THEN MontoGs ELSE 0 END)
FROM dbo.CrmGastos
WHERE EmpresaId = @EmpresaId AND Activo = 1;
GO

PRINT '=== Seed de gastos listo ===';
GO
