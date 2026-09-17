/*
  Datos demo opcionales para VORDIA STAY (idempotente).
  Requiere: Create_Vordia_Stay.sql ya ejecutado.
  Ajustá @EmpresaId si hace falta.
*/

USE GESCO_DB;
GO

DECLARE @EmpresaId INT = 7;

IF NOT EXISTS (SELECT 1 FROM dbo.StayTarifas WHERE EmpresaId = @EmpresaId AND Nombre = N'Demo Noche Stay')
BEGIN
    INSERT INTO dbo.StayTarifas (EmpresaId, Nombre, TipoHabitacion, PrecioPorNocheGs, PrecioPorSemanaGs, PrecioPorMesGs, Activo, Notas)
    VALUES
    (@EmpresaId, N'Demo Noche Stay', N'Habitación', 250000, 1500000, 4500000, 1, N'Seed demo Stay'),
    (@EmpresaId, N'Demo Privada Stay', N'Privada', 350000, NULL, NULL, 1, N'Seed demo Stay');
    PRINT 'Tarifas demo insertadas';
END
ELSE
    PRINT 'Tarifas demo ya existen';
GO
