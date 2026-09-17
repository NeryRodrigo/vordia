/*
  VORDIA AGRO — Empresa de trabajo AGROFIELD + usuarios + datos demo
  Idempotente. Compatible sqlcmd (QUOTED_IDENTIFIER ON).

  Acceso desde HQ:
    nery.chavez@vordia.com.py + tu password → Inicio Agrofield o /agro

  Usuarios Agrofield (shell AGRO al login):
    juan.quinonez@agrofield.com.py / juan12345
    nery.chavez@agrofield.com.py   / nery12345
    admin@agrofield.com.py         / nery12345

  Script solo Juan: Seed_Agrofield_Usuario_Juan.sql
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

------------------------------------------------------------
-- 1) Empresa Agrofield (Rubro = AGRO)
--    Solo columnas base: RazonSocial, RUC, Direccion, Rubro
--    (tu GESCO_DB no tiene NombreFantasia en Empresas)
------------------------------------------------------------
DECLARE @EmpresaId INT;

SELECT @EmpresaId = EmpresaId
FROM dbo.Empresas
WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield';

IF @EmpresaId IS NULL
   SELECT @EmpresaId = EmpresaId FROM dbo.Empresas WHERE RazonSocial = N'Agrofield';

IF @EmpresaId IS NULL
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Agrofield', N'80099887-1', N'Chaco, Paraguay — confinamiento demo', N'AGRO');
    SET @EmpresaId = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa Agrofield creada. EmpresaId=', @EmpresaId);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET RazonSocial = N'Agrofield',
        Rubro = N'AGRO',
        Direccion = ISNULL(Direccion, N'Chaco, Paraguay — confinamiento demo')
    WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('Empresa Agrofield actualizada. EmpresaId=', @EmpresaId);
END
GO

------------------------------------------------------------
-- 2) Módulos AGRO
------------------------------------------------------------
SET QUOTED_IDENTIFIER ON;
DECLARE @Nombre NVARCHAR(100), @Icono NVARCHAR(50), @Ruta NVARCHAR(100);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT * FROM (VALUES
    (N'Inicio Agro', N'Sprout', N'agro_inicio'),
    (N'Ganado', N'Beef', N'agro_ganado'),
    (N'Alimentacion', N'Scale', N'agro_alimentacion'),
    (N'Dispositivos', N'Radio', N'agro_dispositivos'),
    (N'Zonas', N'MapPin', N'agro_zonas'),
    (N'Configuracion', N'Settings', N'configuracion')
) AS v(Nombre, Icono, Ruta);

OPEN cur;
FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado = N'AGRO' AND RutaFrontend = @Ruta)
        INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
        VALUES (@Nombre, N'AGRO', @Icono, @Ruta);
    FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
END
CLOSE cur; DEALLOCATE cur;
PRINT 'Modulos AGRO OK';
GO

------------------------------------------------------------
-- 3) Usuarios (BCrypt de nery12345 — mismo hash que HQ)
------------------------------------------------------------
SET QUOTED_IDENTIFIER ON;
DECLARE @EmpresaId INT = (
    SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield'
);
DECLARE @NeryHash NVARCHAR(MAX) = N'$2a$11$1h3Ih1e8qx0UU9PDgXayjuXbuEgN7NULN0.bxiFHCJEnrKPsMcOQ2';
DECLARE @Uid INT;

IF @EmpresaId IS NULL
BEGIN
    RAISERROR(N'Empresa Agrofield no encontrada', 16, 1);
    RETURN;
END

-- admin@agrofield.com.py
SELECT @Uid = UsuarioId FROM dbo.Usuarios WHERE Email = N'admin@agrofield.com.py';
IF @Uid IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, Rol, EmpresaId, RequiereCambioPass, Estado, FechaCreacion, UltimaConexion)
    VALUES (N'Admin Agrofield', N'admin@agrofield.com.py', @NeryHash, N'ADMIN', @EmpresaId, 0, N'ACTIVO', SYSUTCDATETIME(), NULL);
    SET @Uid = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario admin@agrofield creado. Id=', @Uid);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET PasswordHash = @NeryHash, Rol = N'ADMIN', EmpresaId = @EmpresaId, RequiereCambioPass = 0, Estado = N'ACTIVO', Nombre = N'Admin Agrofield'
    WHERE UsuarioId = @Uid;
    PRINT CONCAT('Usuario admin@agrofield actualizado. Id=', @Uid);
END

-- nery.chavez@agrofield.com.py  (NO es @vordia — email único)
SET @Uid = NULL;
SELECT @Uid = UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@agrofield.com.py';
IF @Uid IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, Rol, EmpresaId, RequiereCambioPass, Estado, FechaCreacion, UltimaConexion)
    VALUES (N'Nery Chavez (Agrofield)', N'nery.chavez@agrofield.com.py', @NeryHash, N'ADMIN', @EmpresaId, 0, N'ACTIVO', SYSUTCDATETIME(), NULL);
    SET @Uid = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario nery.chavez@agrofield creado. Id=', @Uid);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET PasswordHash = @NeryHash, Rol = N'ADMIN', EmpresaId = @EmpresaId, RequiereCambioPass = 0, Estado = N'ACTIVO',
        Nombre = N'Nery Chavez (Agrofield)'
    WHERE UsuarioId = @Uid;
    PRINT CONCAT('Usuario nery.chavez@agrofield actualizado. Id=', @Uid);
END

-- juan.quinonez@agrofield.com.py / juan12345
DECLARE @JuanHash NVARCHAR(MAX) = N'$2a$11$7DFcsGBeNJ1vVAxzzNCaqeH3ZAQ62i4qmzfe9UH6OWzlwF0nnBze2';
SET @Uid = NULL;
SELECT @Uid = UsuarioId FROM dbo.Usuarios WHERE Email = N'juan.quinonez@agrofield.com.py';
IF @Uid IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, Rol, EmpresaId, RequiereCambioPass, Estado, FechaCreacion, UltimaConexion)
    VALUES (N'Juan Quiñonez', N'juan.quinonez@agrofield.com.py', @JuanHash, N'ADMIN', @EmpresaId, 0, N'ACTIVO', SYSUTCDATETIME(), NULL);
    SET @Uid = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario juan.quinonez@agrofield creado. Id=', @Uid);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET PasswordHash = @JuanHash, Rol = N'ADMIN', EmpresaId = @EmpresaId, RequiereCambioPass = 0, Estado = N'ACTIVO',
        Nombre = N'Juan Quiñonez'
    WHERE UsuarioId = @Uid;
    PRINT CONCAT('Usuario juan.quinonez@agrofield actualizado. Id=', @Uid);
END

-- Licencias + UsuarioModulos
INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
SELECT @EmpresaId, m.ModuloId, 1, 0, NULL
FROM dbo.Modulos m
WHERE m.RubroAsociado = N'AGRO'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LicenciasModulos lm
      WHERE lm.EmpresaId = @EmpresaId AND lm.ModuloId = m.ModuloId
  );

INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId)
SELECT u.UsuarioId, m.ModuloId
FROM dbo.Usuarios u
CROSS JOIN dbo.Modulos m
WHERE u.EmpresaId = @EmpresaId
  AND m.RubroAsociado = N'AGRO'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UsuarioModulos um
      WHERE um.UsuarioId = u.UsuarioId AND um.ModuloId = m.ModuloId
  );

PRINT 'Usuarios y licencias Agrofield OK';
GO

------------------------------------------------------------
-- 4) Asegurar tablas Agro* (si Create_Vordia_Agro aún no corrió)
------------------------------------------------------------
-- Si faltan tablas, el usuario debe correr Create_Vordia_Agro.sql primero.
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'dbo.AgroEstablecimientos', N'U') IS NULL
BEGIN
    RAISERROR(N'Faltan tablas Agro*. Ejecutá primero Create_Vordia_Agro.sql y luego este seed.', 16, 1);
END
GO

------------------------------------------------------------
-- 5) Seed operativo bajo empresa Agrofield
------------------------------------------------------------
SET QUOTED_IDENTIFIER ON;
DECLARE @EmpresaId INT = (
    SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield'
);

IF @EmpresaId IS NOT NULL
   AND OBJECT_ID(N'dbo.AgroEstablecimientos', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.AgroEstablecimientos
        WHERE EmpresaId = @EmpresaId AND ClienteNombre = N'Agrofield'
   )
BEGIN
    DECLARE @EstId INT, @L1 INT, @L2 INT, @L3 INT;
    DECLARE @A1 INT, @A2 INT, @A3 INT, @A4 INT, @A5 INT;

    INSERT INTO dbo.AgroEstablecimientos (EmpresaId, Nombre, ClienteNombre, Ubicacion, Hectareas, Notas)
    VALUES (@EmpresaId, N'Confinamiento Agrofield Central', N'Agrofield', N'Chaco paraguayo', 420,
            N'Entorno de trabajo empresa Agrofield (Rubro AGRO).');
    SET @EstId = SCOPE_IDENTITY();

    INSERT INTO dbo.AgroLotes (EmpresaId, EstablecimientoId, Codigo, Nombre, Tipo, Capacidad, CamaraCodigo)
    VALUES
    (@EmpresaId, @EstId, N'C-01', N'Corral engorde 1', N'Corral', 120, N'CAM-01'),
    (@EmpresaId, @EstId, N'C-04', N'Corral engorde 4', N'Corral', 100, N'CAM-04'),
    (@EmpresaId, @EstId, N'P-07', N'Potrero rotativo 7', N'Potrero', 80, N'CAM-07');
    SET @L1 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-01');
    SET @L2 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'C-04');
    SET @L3 = (SELECT LoteId FROM dbo.AgroLotes WHERE EstablecimientoId=@EstId AND Codigo=N'P-07');

    INSERT INTO dbo.AgroZonas (EmpresaId, EstablecimientoId, Nombre, Tipo, LatCentro, LngCentro, RadioMetros)
    VALUES
    (@EmpresaId, @EstId, N'Zona engorde permitida', N'Permitida', -23.442500, -58.443800, 350),
    (@EmpresaId, @EstId, N'Perímetro silo (prohibido)', N'Prohibida', -23.441200, -58.445100, 80);

    INSERT INTO dbo.AgroAnimales (EmpresaId, EstablecimientoId, LoteId, CodigoInterno, RfidTag, CollarId, Categoria, Sexo, FechaIngreso, PesoIngresoKg, PesoActualKg, Estado, UltimaLat, UltimaLng, UltimaUbicacionAt, BateriaCollarPct)
    VALUES
    (@EmpresaId, @EstId, @L1, N'1847', N'RFID-1847', N'COL-1847', N'Novillo', N'M', DATEADD(DAY,-42,CAST(GETDATE() AS DATE)), 350, 410, N'Activo', -23.442600, -58.443900, SYSUTCDATETIME(), 78),
    (@EmpresaId, @EstId, @L1, N'1848', N'RFID-1848', N'COL-1848', N'Novillo', N'M', DATEADD(DAY,-42,CAST(GETDATE() AS DATE)), 362, 401, N'Activo', -23.442700, -58.444000, SYSUTCDATETIME(), 65),
    (@EmpresaId, @EstId, @L2, N'1901', N'RFID-1901', N'COL-1901', N'Vaquilla', N'H', DATEADD(DAY,-30,CAST(GETDATE() AS DATE)), 310, 338, N'Activo', -23.443100, -58.444500, SYSUTCDATETIME(), 91),
    (@EmpresaId, @EstId, @L2, N'1902', N'RFID-1902', N'COL-1902', N'Novillo', N'M', DATEADD(DAY,-30,CAST(GETDATE() AS DATE)), 380, 392, N'Tratamiento', -23.443000, -58.444400, SYSUTCDATETIME(), 42),
    (@EmpresaId, @EstId, @L3, N'2005', N'RFID-2005', N'COL-2005', N'Novillo', N'M', DATEADD(DAY,-15,CAST(GETDATE() AS DATE)), 340, 348, N'FueraZona', -23.441100, -58.445200, SYSUTCDATETIME(), 22);

    SET @A1 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1847');
    SET @A2 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1848');
    SET @A3 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1901');
    SET @A4 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'1902');
    SET @A5 = (SELECT AnimalId FROM dbo.AgroAnimales WHERE EstablecimientoId=@EstId AND CodigoInterno=N'2005');

    INSERT INTO dbo.AgroPesajes (EmpresaId, AnimalId, PesoKg, FechaPesaje, Origen, Operario)
    VALUES
    (@EmpresaId, @A1, 350, DATEADD(DAY,-42,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A1, 410, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A2, 401, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Balanza', N'Operario demo'),
    (@EmpresaId, @A3, 338, DATEADD(DAY,-1,SYSUTCDATETIME()), N'RFID', N'Operario demo');

    INSERT INTO dbo.AgroRaciones (EmpresaId, LoteId, TipoAlimento, CantidadKg, FechaEntrega, Operario, Vehiculo, CostoGs)
    VALUES
    (@EmpresaId, @L1, N'TMR engorde', 2500, DATEADD(HOUR,-6,SYSUTCDATETIME()), N'Juan P.', N'Camión 07', 8750000),
    (@EmpresaId, @L2, N'TMR engorde', 1800, DATEADD(HOUR,-5,SYSUTCDATETIME()), N'Juan P.', N'Camión 07', 6300000),
    (@EmpresaId, @L3, N'Pastura + concentrado', 900, DATEADD(DAY,-1,SYSUTCDATETIME()), N'Mario L.', N'Tractor 03', 2700000);

    INSERT INTO dbo.AgroDispositivos (EmpresaId, EstablecimientoId, Codigo, Tipo, AnimalId, Estado, BateriaPct, UltimoPingAt)
    VALUES
    (@EmpresaId, @EstId, N'COL-1847', N'Collar', @A1, N'Online', 78, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'COL-2005', N'Collar', @A5, N'Alerta', 22, DATEADD(MINUTE,-40,SYSUTCDATETIME())),
    (@EmpresaId, @EstId, N'CAM-07', N'Camara', NULL, N'Online', NULL, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'BAL-01', N'Balanza', NULL, N'Online', NULL, SYSUTCDATETIME()),
    (@EmpresaId, @EstId, N'GW-LORA-01', N'Gateway', NULL, N'Online', NULL, SYSUTCDATETIME());

    INSERT INTO dbo.AgroAlertas (EmpresaId, EstablecimientoId, AnimalId, Tipo, Severidad, Titulo, Detalle, Resuelta)
    VALUES
    (@EmpresaId, @EstId, @A5, N'FueraZona', N'Alta', N'Animal 2005 fuera de zona permitida', N'Cámara sugerida: CAM-07', 0),
    (@EmpresaId, @EstId, @A5, N'BateriaBaja', N'Media', N'Collar COL-2005 batería 22%', N'Programar recarga', 0),
    (@EmpresaId, @EstId, @A4, N'BajoPeso', N'Media', N'Animal 1902 por debajo de GPD esperada', N'En tratamiento', 0);

    PRINT 'Seed operativo Agrofield OK';
END
ELSE PRINT 'Seed operativo omitido (ya existe o sin tablas)';
GO

PRINT '=== AGROFIELD listo ===';
PRINT 'Login: nery.chavez@agrofield.com.py / nery12345';
PRINT 'Login: admin@agrofield.com.py / nery12345';
PRINT 'Login: juan.quinonez@agrofield.com.py / juan12345';
PRINT 'HQ sigue con: nery.chavez@vordia.com.py / nery12345';
SELECT EmpresaId, RazonSocial, Rubro FROM dbo.Empresas WHERE Rubro = N'AGRO';
SELECT UsuarioId, Nombre, Email, EmpresaId, Rol FROM dbo.Usuarios WHERE Email LIKE N'%@agrofield.com.py';
GO
