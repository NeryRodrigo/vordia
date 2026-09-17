-- Permisos granulares por usuario (JSON array de ids, ej: ["fontana_ver","condo_registrar_pago"])
IF COL_LENGTH('dbo.Usuarios', 'PermisosJson') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD PermisosJson NVARCHAR(MAX) NULL;
    PRINT 'Columna Usuarios.PermisosJson agregada.';
END
ELSE
    PRINT 'Columna Usuarios.PermisosJson ya existe.';
GO
