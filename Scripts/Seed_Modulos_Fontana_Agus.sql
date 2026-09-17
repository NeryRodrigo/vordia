-- Módulos de menú Fontana / Condominio Agus (por rubro de la empresa)
-- Ejecutar en GESCO_DB. Ajusta @Rubro si tu empresa usa otro valor.

DECLARE @Rubro NVARCHAR(50) = (
    SELECT TOP 1 Rubro FROM dbo.Empresas WHERE EmpresaId = 7
);
IF @Rubro IS NULL OR LTRIM(RTRIM(@Rubro)) = N'' SET @Rubro = N'CONDOMINIO';

DECLARE @Mods TABLE (NombreModulo NVARCHAR(50), Icono NVARCHAR(50), RutaFrontend NVARCHAR(100));
INSERT INTO @Mods (NombreModulo, Icono, RutaFrontend) VALUES
(N'Condominio Agus', N'LayoutDashboard', N'condominio'),
(N'Fontana', N'Bed', N'fontana'),
(N'Actividades', N'Activity', N'actividades'),
(N'Facturación', N'Wallet', N'facturacion'),
(N'Administración', N'FileText', N'administracion'),
(N'Configuración', N'ShieldCheck', N'configuracion'),
(N'Gastos del condominio', N'DollarSign', N'gastos'),
(N'Histórico de contratos', N'History', N'historico_contratos');

DECLARE @Nombre NVARCHAR(50), @Icono NVARCHAR(50), @Ruta NVARCHAR(100);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT NombreModulo, Icono, RutaFrontend FROM @Mods;
OPEN cur;
FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado = @Rubro AND RutaFrontend = @Ruta)
        INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
        VALUES (@Nombre, @Rubro, @Icono, @Ruta);
    ELSE
        UPDATE dbo.Modulos SET NombreModulo = @Nombre, Icono = @Icono
        WHERE RubroAsociado = @Rubro AND RutaFrontend = @Ruta;

    FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
END
CLOSE cur; DEALLOCATE cur;

PRINT CONCAT('Módulos Fontana/Agus listos para rubro: ', @Rubro);
SELECT ModuloId, NombreModulo, RubroAsociado, RutaFrontend
FROM dbo.Modulos WHERE RubroAsociado = @Rubro ORDER BY ModuloId;
GO
