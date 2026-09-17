/*
  Seed Vordia Interno — GESCO_DB
  --------------------------------
  1) Empresa Vordia (Rubro = VORDIA)
  2) Módulos del CRM / admin comercial-financiero (vacíos en frontend)
  3) Usuarios administradores:
       - eli.ortigoza@vordia.com.py  / eli12345
       - nery.chavez@vordia.com.py   / nery12345
  4) UsuarioModulos + LicenciasModulos

  Idempotente: se puede re-ejecutar sin duplicar.
  Contraseñas hasheadas con BCrypt (mismo algoritmo que AuthService).
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

------------------------------------------------------------
-- 1) Empresa Vordia
------------------------------------------------------------
DECLARE @EmpresaId INT;

SELECT @EmpresaId = EmpresaId
FROM dbo.Empresas
WHERE RazonSocial = N'Vordia'
   OR RUC = N'80012345-6';

IF @EmpresaId IS NULL
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Vordia', N'80012345-6', N'Asunción, Paraguay', N'VORDIA');

    SET @EmpresaId = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa Vordia creada. EmpresaId=', @EmpresaId);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET RazonSocial = N'Vordia',
        Rubro = N'VORDIA',
        Direccion = ISNULL(Direccion, N'Asunción, Paraguay')
    WHERE EmpresaId = @EmpresaId;

    PRINT CONCAT('Empresa Vordia ya existía. EmpresaId=', @EmpresaId);
END

------------------------------------------------------------
-- 2) Módulos Vordia Interno
------------------------------------------------------------
DECLARE @Mods TABLE (
    NombreModulo  NVARCHAR(50)  NOT NULL,
    Icono         NVARCHAR(50)  NOT NULL,
    RutaFrontend  NVARCHAR(100) NOT NULL
);

INSERT INTO @Mods (NombreModulo, Icono, RutaFrontend) VALUES
(N'Resumen operativo',   N'LayoutDashboard', N'inicio'),
(N'Clientes',            N'Users',           N'vordia_clientes'),
(N'Pipeline comercial',  N'List',            N'vordia_pipeline'),
(N'Contratos y fees',    N'FileText',        N'vordia_contratos'),
(N'Ingresos y pagos',    N'DollarSign',      N'vordia_ingresos'),
(N'Tareas',              N'CheckSquare',     N'vordia_tareas'),
(N'Configuracion',       N'Settings',        N'configuracion'),
(N'Usuarios / Roles',    N'ShieldCheck',     N'usuarios');

DECLARE @Nombre NVARCHAR(50), @Icono NVARCHAR(50), @Ruta NVARCHAR(100);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT NombreModulo, Icono, RutaFrontend FROM @Mods;

OPEN cur;
FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Modulos
        WHERE RubroAsociado = N'VORDIA'
          AND RutaFrontend = @Ruta
    )
    BEGIN
        INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
        VALUES (@Nombre, N'VORDIA', @Icono, @Ruta);
        PRINT CONCAT('Módulo creado: ', @Nombre);
    END
    ELSE
    BEGIN
        UPDATE dbo.Modulos
        SET NombreModulo = @Nombre,
            Icono = @Icono
        WHERE RubroAsociado = N'VORDIA'
          AND RutaFrontend = @Ruta;
    END

    FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
END

CLOSE cur;
DEALLOCATE cur;

------------------------------------------------------------
-- 3) Usuarios administradores (BCrypt)
--    eli12345  → $2a$11$hKCBY3TO5tIi57Hh.h.ileRbuUWcpOph.7GZ9GRnaw88qWSX.QcI.
--    nery12345 → $2a$11$1h3Ih1e8qx0UU9PDgXayjuXbuEgN7NULN0.bxiFHCJEnrKPsMcOQ2
------------------------------------------------------------
DECLARE @EliHash  NVARCHAR(MAX) = N'$2a$11$hKCBY3TO5tIi57Hh.h.ileRbuUWcpOph.7GZ9GRnaw88qWSX.QcI.';
DECLARE @NeryHash NVARCHAR(MAX) = N'$2a$11$1h3Ih1e8qx0UU9PDgXayjuXbuEgN7NULN0.bxiFHCJEnrKPsMcOQ2';

DECLARE @EliId INT, @NeryId INT;

-- Eli Ortigoza
SELECT @EliId = UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py';

IF @EliId IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (
        Nombre, Email, PasswordHash, Rol, EmpresaId,
        RequiereCambioPass, Estado, FechaCreacion, UltimaConexion
    )
    VALUES (
        N'Eli Ortigoza',
        N'eli.ortigoza@vordia.com.py',
        @EliHash,
        N'ADMIN',
        @EmpresaId,
        0,
        N'ACTIVO',
        SYSUTCDATETIME(),
        NULL
    );
    SET @EliId = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario Eli creado. UsuarioId=', @EliId);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET Nombre = N'Eli Ortigoza',
        PasswordHash = @EliHash,
        Rol = N'ADMIN',
        EmpresaId = @EmpresaId,
        RequiereCambioPass = 0,
        Estado = N'ACTIVO'
    WHERE UsuarioId = @EliId;
    PRINT CONCAT('Usuario Eli actualizado. UsuarioId=', @EliId);
END

-- Nery Chávez
SELECT @NeryId = UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py';

IF @NeryId IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (
        Nombre, Email, PasswordHash, Rol, EmpresaId,
        RequiereCambioPass, Estado, FechaCreacion, UltimaConexion
    )
    VALUES (
        N'Nery Chavez',
        N'nery.chavez@vordia.com.py',
        @NeryHash,
        N'ADMIN',
        @EmpresaId,
        0,
        N'ACTIVO',
        SYSUTCDATETIME(),
        NULL
    );
    SET @NeryId = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario Nery creado. UsuarioId=', @NeryId);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET Nombre = N'Nery Chavez',
        PasswordHash = @NeryHash,
        Rol = N'ADMIN',
        EmpresaId = @EmpresaId,
        RequiereCambioPass = 0,
        Estado = N'ACTIVO'
    WHERE UsuarioId = @NeryId;
    PRINT CONCAT('Usuario Nery actualizado. UsuarioId=', @NeryId);
END

------------------------------------------------------------
-- 4) LicenciasModulos (empresa Vordia → todos los módulos VORDIA)
------------------------------------------------------------
INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
SELECT @EmpresaId, m.ModuloId, 1, 0, NULL
FROM dbo.Modulos m
WHERE m.RubroAsociado = N'VORDIA'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.LicenciasModulos l
      WHERE l.EmpresaId = @EmpresaId
        AND l.ModuloId = m.ModuloId
  );

UPDATE dbo.LicenciasModulos
SET Activo = 1,
    PruebaGratisActiva = 0,
    FechaExpiracion = NULL
WHERE EmpresaId = @EmpresaId
  AND ModuloId IN (SELECT ModuloId FROM dbo.Modulos WHERE RubroAsociado = N'VORDIA');

------------------------------------------------------------
-- 5) UsuarioModulos (admins → todos los módulos VORDIA)
------------------------------------------------------------
;WITH Admins AS (
    SELECT UsuarioId FROM (VALUES (@EliId), (@NeryId)) AS u(UsuarioId)
)
INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId)
SELECT a.UsuarioId, m.ModuloId
FROM Admins a
CROSS JOIN dbo.Modulos m
WHERE m.RubroAsociado = N'VORDIA'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.UsuarioModulos um
      WHERE um.UsuarioId = a.UsuarioId
        AND um.ModuloId = m.ModuloId
  );

COMMIT TRAN;

------------------------------------------------------------
-- Resumen
------------------------------------------------------------
PRINT '---- RESUMEN VORDIA INTERNO ----';
SELECT EmpresaId, RazonSocial, RUC, Rubro
FROM dbo.Empresas
WHERE EmpresaId = @EmpresaId;

SELECT UsuarioId, Nombre, Email, Rol, EmpresaId, Estado, RequiereCambioPass
FROM dbo.Usuarios
WHERE UsuarioId IN (@EliId, @NeryId);

SELECT ModuloId, NombreModulo, RubroAsociado, Icono, RutaFrontend
FROM dbo.Modulos
WHERE RubroAsociado = N'VORDIA'
ORDER BY ModuloId;

SELECT um.UsuarioId, u.Email, um.ModuloId, m.NombreModulo
FROM dbo.UsuarioModulos um
JOIN dbo.Usuarios u ON u.UsuarioId = um.UsuarioId
JOIN dbo.Modulos m ON m.ModuloId = um.ModuloId
WHERE um.UsuarioId IN (@EliId, @NeryId)
ORDER BY u.Email, m.ModuloId;

SELECT LicenciaId, EmpresaId, ModuloId, Activo
FROM dbo.LicenciasModulos
WHERE EmpresaId = @EmpresaId;

GO
