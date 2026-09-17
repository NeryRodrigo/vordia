/*
  Agrofield — alta usuario Juan Quiñonez
  Idempotente. Corre en GESCO_DB (después de Seed_Agrofield_Empresa.sql).

  Login:
    juan.quinonez@agrofield.com.py  /  juan12345
    Rol: ADMIN (ve el prototipo Agro completo)
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @EmpresaId INT = (
    SELECT TOP 1 EmpresaId
    FROM dbo.Empresas
    WHERE Rubro = N'AGRO' AND RazonSocial = N'Agrofield'
);

IF @EmpresaId IS NULL
BEGIN
    RAISERROR(N'No existe empresa Agrofield. Ejecutá primero Seed_Agrofield_Empresa.sql', 16, 1);
    RETURN;
END

-- BCrypt cost 11 de "juan12345"
DECLARE @JuanHash NVARCHAR(MAX) = N'$2a$11$7DFcsGBeNJ1vVAxzzNCaqeH3ZAQ62i4qmzfe9UH6OWzlwF0nnBze2';
DECLARE @Uid INT;

SELECT @Uid = UsuarioId FROM dbo.Usuarios WHERE Email = N'juan.quinonez@agrofield.com.py';

IF @Uid IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (
        Nombre, Email, PasswordHash, Rol, EmpresaId,
        RequiereCambioPass, Estado, FechaCreacion, UltimaConexion
    )
    VALUES (
        N'Juan Quiñonez',
        N'juan.quinonez@agrofield.com.py',
        @JuanHash,
        N'ADMIN',
        @EmpresaId,
        0,
        N'ACTIVO',
        SYSUTCDATETIME(),
        NULL
    );
    SET @Uid = SCOPE_IDENTITY();
    PRINT CONCAT('Usuario Juan Quiñonez creado. UsuarioId=', @Uid, ' EmpresaId=', @EmpresaId);
END
ELSE
BEGIN
    UPDATE dbo.Usuarios
    SET Nombre = N'Juan Quiñonez',
        PasswordHash = @JuanHash,
        Rol = N'ADMIN',
        EmpresaId = @EmpresaId,
        RequiereCambioPass = 0,
        Estado = N'ACTIVO'
    WHERE UsuarioId = @Uid;
    PRINT CONCAT('Usuario Juan Quiñonez actualizado. UsuarioId=', @Uid, ' EmpresaId=', @EmpresaId);
END

-- Licencias AGRO para la empresa
INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
SELECT @EmpresaId, m.ModuloId, 1, 0, NULL
FROM dbo.Modulos m
WHERE m.RubroAsociado = N'AGRO'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LicenciasModulos lm
      WHERE lm.EmpresaId = @EmpresaId AND lm.ModuloId = m.ModuloId
  );

-- Módulos al usuario (ve Confinamiento / prototipo)
INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId)
SELECT @Uid, m.ModuloId
FROM dbo.Modulos m
WHERE m.RubroAsociado = N'AGRO'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UsuarioModulos um
      WHERE um.UsuarioId = @Uid AND um.ModuloId = m.ModuloId
  );

PRINT '=== Juan Quiñonez listo para Agrofield ===';
PRINT 'Login: juan.quinonez@agrofield.com.py / juan12345';
PRINT 'Al entrar verá el shell Agrofield (Confinamiento = prototipo).';

SELECT u.UsuarioId, u.Nombre, u.Email, u.Rol, u.EmpresaId, e.RazonSocial, e.Rubro
FROM dbo.Usuarios u
INNER JOIN dbo.Empresas e ON e.EmpresaId = u.EmpresaId
WHERE u.Email = N'juan.quinonez@agrofield.com.py';
GO
