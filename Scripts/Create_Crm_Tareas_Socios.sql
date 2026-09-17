/*
  CRM Vordia — Tareas + Socios + módulo Flujo de caja
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.CrmTareas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmTareas (
        TareaId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmTareas PRIMARY KEY,
        EmpresaId          INT NOT NULL,
        Titulo             NVARCHAR(200) NOT NULL,
        Descripcion        NVARCHAR(MAX) NULL,
        FechaVencimiento   DATE NULL,
        Estado             NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmTareas_Estado DEFAULT (N'Pendiente'),
        Prioridad          NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmTareas_Prioridad DEFAULT (N'Media'),
        ClienteId          INT NULL,
        ContratoId         INT NULL,
        AsignadoUsuarioId  INT NULL,
        AsignadoNombre     NVARCHAR(150) NULL,
        Activo             BIT NOT NULL CONSTRAINT DF_CrmTareas_Activo DEFAULT (1),
        FechaCreacion      DATETIME2 NOT NULL CONSTRAINT DF_CrmTareas_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaCompletada    DATETIME2 NULL,
        CONSTRAINT FK_CrmTareas_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_CrmTareas_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes(ClienteId),
        CONSTRAINT CK_CrmTareas_Estado CHECK (Estado IN (N'Pendiente', N'En curso', N'Hecha', N'Cancelada')),
        CONSTRAINT CK_CrmTareas_Prioridad CHECK (Prioridad IN (N'Baja', N'Media', N'Alta'))
    );
    CREATE INDEX IX_CrmTareas_Empresa ON dbo.CrmTareas (EmpresaId, Activo, FechaVencimiento);
    PRINT 'Tabla CrmTareas creada';
END
GO

IF OBJECT_ID(N'dbo.CrmSocios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmSocios (
        SocioId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmSocios PRIMARY KEY,
        EmpresaId   INT NOT NULL,
        Nombre      NVARCHAR(150) NOT NULL,
        Email       NVARCHAR(150) NULL,
        Porcentaje  DECIMAL(5,2) NOT NULL,
        UsuarioId   INT NULL,
        Activo      BIT NOT NULL CONSTRAINT DF_CrmSocios_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CrmSocios_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmSocios_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmSocios_Porcentaje CHECK (Porcentaje >= 0 AND Porcentaje <= 100)
    );
    CREATE INDEX IX_CrmSocios_Empresa ON dbo.CrmSocios (EmpresaId, Activo);
    PRINT 'Tabla CrmSocios creada';
END
GO

DECLARE @EmpresaId INT;
SELECT @EmpresaId = EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' AND RazonSocial = N'Vordia';
IF @EmpresaId IS NULL BEGIN PRINT 'ERROR: Empresa Vordia no encontrada'; RETURN; END

DECLARE @EliId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py');
DECLARE @NeryId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py');
DECLARE @Fontana INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Fontana Di Luque');
DECLARE @Clinica INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Clinica Santa Rita');
DECLARE @Hotel INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Hotel Paraguay Plaza');

-- Módulo Flujo de caja
IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado=N'VORDIA' AND RutaFrontend=N'vordia_caja')
BEGIN
    INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
    VALUES (N'Flujo de caja', N'VORDIA', N'Wallet', N'vordia_caja');
END

DECLARE @ModCaja INT = (SELECT TOP 1 ModuloId FROM dbo.Modulos WHERE RubroAsociado=N'VORDIA' AND RutaFrontend=N'vordia_caja');

IF @ModCaja IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.LicenciasModulos WHERE EmpresaId=@EmpresaId AND ModuloId=@ModCaja)
        INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
        VALUES (@EmpresaId, @ModCaja, 1, 0, NULL);

    INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId)
    SELECT u.UsuarioId, @ModCaja
    FROM dbo.Usuarios u
    WHERE u.EmpresaId = @EmpresaId AND u.Rol = N'ADMIN'
      AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioModulos um WHERE um.UsuarioId=u.UsuarioId AND um.ModuloId=@ModCaja);
END

-- Socios 50/50
IF NOT EXISTS (SELECT 1 FROM dbo.CrmSocios WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.CrmSocios (EmpresaId, Nombre, Email, Porcentaje, UsuarioId) VALUES
    (@EmpresaId, N'Eli Ortigoza', N'eli.ortigoza@vordia.com.py', 50.00, @EliId),
    (@EmpresaId, N'Nery Chavez', N'nery.chavez@vordia.com.py', 50.00, @NeryId);
    PRINT 'Socios seed 50/50';
END

-- Tareas ejemplo
IF NOT EXISTS (SELECT 1 FROM dbo.CrmTareas WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.CrmTareas (EmpresaId, Titulo, Descripcion, FechaVencimiento, Estado, Prioridad, ClienteId, AsignadoUsuarioId, AsignadoNombre)
    VALUES
    (@EmpresaId, N'Enviar demo Clinica Santa Rita', N'Preparar demo de turnos y WhatsApp.', DATEADD(DAY, 3, CAST(GETDATE() AS DATE)), N'En curso', N'Alta', @Clinica, @EliId, N'Eli Ortigoza'),
    (@EmpresaId, N'Seguimiento propuesta Hotel Paraguay', N'Llamar para feedback de la propuesta PMS.', DATEADD(DAY, 2, CAST(GETDATE() AS DATE)), N'Pendiente', N'Alta', @Hotel, @NeryId, N'Nery Chavez'),
    (@EmpresaId, N'Cobrar fee Agro Norte (primer mes)', N'Registrar pago pendiente en Ingresos.', DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), N'Pendiente', N'Media', NULL, @NeryId, N'Nery Chavez'),
    (@EmpresaId, N'Revision renovacion Fontana Q4', N'Revisar alcance y fee para renovacion.', DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), N'Pendiente', N'Baja', @Fontana, @EliId, N'Eli Ortigoza'),
    (@EmpresaId, N'Definir % reparto socios 2026', N'Confirmar 50/50 o ajustar en Flujo de caja.', DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), N'Hecha', N'Media', NULL, @NeryId, N'Nery Chavez');

    UPDATE dbo.CrmTareas SET FechaCompletada = SYSUTCDATETIME()
    WHERE EmpresaId=@EmpresaId AND Titulo LIKE N'Definir % reparto%';

    PRINT 'Tareas seed cargadas';
END

SELECT Nombre, Porcentaje FROM dbo.CrmSocios WHERE EmpresaId=@EmpresaId;
SELECT TareaId, Titulo, Estado, Prioridad, FechaVencimiento, AsignadoNombre FROM dbo.CrmTareas WHERE EmpresaId=@EmpresaId AND Activo=1;
SELECT ModuloId, NombreModulo, RutaFrontend FROM dbo.Modulos WHERE RubroAsociado=N'VORDIA' ORDER BY ModuloId;
GO
