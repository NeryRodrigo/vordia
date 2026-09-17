/*
================================================================================
  VORDIA INTERNO — SCRIPT COMPLETO PARA SERVIDOR (GESCO_DB)
================================================================================
  Incluye (idempotente; se puede re-ejecutar):
    1) Empresa Vordia
    2) Modulos Vordia + LicenciasModulos + UsuarioModulos
    3) Usuarios ADMIN:
         eli.ortigoza@vordia.com.py  / eli12345
         nery.chavez@vordia.com.py   / nery12345
    4) Tablas CRM: CrmClientes, CrmHistorial, CrmContratos, CrmPagos,
                   CrmTareas, CrmSocios
    5) Datos de ejemplo (solo si las tablas estan vacias para Vordia)

  Requisitos:
    - SQL Server con base GESCO_DB
    - Tablas base Gesco ya creadas (Empresas, Usuarios, Modulos, etc.)

  Ejecucion (ejemplo):
    sqlcmd -S TU_SERVIDOR -d GESCO_DB -U usuario -P clave -i Seed_Vordia_Completo_Servidor.sql
    o abrir en SSMS y ejecutar (F5)

  Tras el script: desplegar API + frontend actualizados y reiniciar contenedores/servicios.
================================================================================
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '==== INICIO Seed Vordia Completo ====';

------------------------------------------------------------
-- A) EMPRESA + USUARIOS + MODULOS
------------------------------------------------------------
BEGIN TRAN;

DECLARE @EmpresaId INT;

SELECT @EmpresaId = EmpresaId
FROM dbo.Empresas
WHERE RazonSocial = N'Vordia' OR RUC = N'80012345-6';

IF @EmpresaId IS NULL
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Vordia', N'80012345-6', N'Asuncion, Paraguay', N'VORDIA');
    SET @EmpresaId = SCOPE_IDENTITY();
    PRINT CONCAT('Empresa Vordia creada. EmpresaId=', @EmpresaId);
END
ELSE
BEGIN
    UPDATE dbo.Empresas
    SET RazonSocial = N'Vordia',
        Rubro = N'VORDIA',
        Direccion = ISNULL(Direccion, N'Asuncion, Paraguay')
    WHERE EmpresaId = @EmpresaId;
    PRINT CONCAT('Empresa Vordia ya existia. EmpresaId=', @EmpresaId);
END

-- Modulos
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
(N'Flujo de caja',       N'Wallet',          N'vordia_caja'),
(N'Tareas',              N'CheckSquare',     N'vordia_tareas'),
(N'Configuracion',       N'Settings',        N'configuracion'),
(N'Usuarios / Roles',    N'ShieldCheck',     N'usuarios');

DECLARE @Nombre NVARCHAR(50), @Icono NVARCHAR(50), @Ruta NVARCHAR(100);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT NombreModulo, Icono, RutaFrontend FROM @Mods;
OPEN cur;
FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Modulos WHERE RubroAsociado = N'VORDIA' AND RutaFrontend = @Ruta)
        INSERT INTO dbo.Modulos (NombreModulo, RubroAsociado, Icono, RutaFrontend)
        VALUES (@Nombre, N'VORDIA', @Icono, @Ruta);
    ELSE
        UPDATE dbo.Modulos SET NombreModulo = @Nombre, Icono = @Icono
        WHERE RubroAsociado = N'VORDIA' AND RutaFrontend = @Ruta;
    FETCH NEXT FROM cur INTO @Nombre, @Icono, @Ruta;
END
CLOSE cur; DEALLOCATE cur;

-- Passwords BCrypt (AuthService / BCrypt.Net)
DECLARE @EliHash  NVARCHAR(MAX) = N'$2a$11$f/pd/tSy3oLwq9QwoXX9M.cplPE8Qszgc/LMNSAwS6QfjQa9VRvRy';
DECLARE @NeryHash NVARCHAR(MAX) = N'$2a$11$eHbvxEj0tnajNOyhxmnvJetXjPV1f4T6Oc33d4C6Km2Qz9Dm83u9m';
DECLARE @EliId INT, @NeryId INT;

SELECT @EliId = UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py';
IF @EliId IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, Rol, EmpresaId, RequiereCambioPass, Estado, FechaCreacion, UltimaConexion)
    VALUES (N'Eli Ortigoza', N'eli.ortigoza@vordia.com.py', @EliHash, N'ADMIN', @EmpresaId, 0, N'ACTIVO', SYSUTCDATETIME(), NULL);
    SET @EliId = SCOPE_IDENTITY();
END
ELSE
    UPDATE dbo.Usuarios
    SET Nombre=N'Eli Ortigoza', PasswordHash=@EliHash, Rol=N'ADMIN', EmpresaId=@EmpresaId,
        RequiereCambioPass=0, Estado=N'ACTIVO'
    WHERE UsuarioId=@EliId;

SELECT @NeryId = UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py';
IF @NeryId IS NULL
BEGIN
    INSERT INTO dbo.Usuarios (Nombre, Email, PasswordHash, Rol, EmpresaId, RequiereCambioPass, Estado, FechaCreacion, UltimaConexion)
    VALUES (N'Nery Chavez', N'nery.chavez@vordia.com.py', @NeryHash, N'ADMIN', @EmpresaId, 0, N'ACTIVO', SYSUTCDATETIME(), NULL);
    SET @NeryId = SCOPE_IDENTITY();
END
ELSE
    UPDATE dbo.Usuarios
    SET Nombre=N'Nery Chavez', PasswordHash=@NeryHash, Rol=N'ADMIN', EmpresaId=@EmpresaId,
        RequiereCambioPass=0, Estado=N'ACTIVO'
    WHERE UsuarioId=@NeryId;

-- Licencias
INSERT INTO dbo.LicenciasModulos (EmpresaId, ModuloId, Activo, PruebaGratisActiva, FechaExpiracion)
SELECT @EmpresaId, m.ModuloId, 1, 0, NULL
FROM dbo.Modulos m
WHERE m.RubroAsociado = N'VORDIA'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LicenciasModulos l
      WHERE l.EmpresaId = @EmpresaId AND l.ModuloId = m.ModuloId
  );

UPDATE dbo.LicenciasModulos
SET Activo = 1, PruebaGratisActiva = 0, FechaExpiracion = NULL
WHERE EmpresaId = @EmpresaId
  AND ModuloId IN (SELECT ModuloId FROM dbo.Modulos WHERE RubroAsociado = N'VORDIA');

-- UsuarioModulos
;WITH Admins AS (
    SELECT UsuarioId FROM (VALUES (@EliId), (@NeryId)) AS u(UsuarioId)
)
INSERT INTO dbo.UsuarioModulos (UsuarioId, ModuloId)
SELECT a.UsuarioId, m.ModuloId
FROM Admins a
CROSS JOIN dbo.Modulos m
WHERE m.RubroAsociado = N'VORDIA'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UsuarioModulos um
      WHERE um.UsuarioId = a.UsuarioId AND um.ModuloId = m.ModuloId
  );

COMMIT TRAN;
PRINT 'Empresa, usuarios y modulos OK';
GO

------------------------------------------------------------
-- B) TABLAS CRM
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmClientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmClientes (
        ClienteId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmClientes PRIMARY KEY,
        EmpresaId            INT NOT NULL,
        RazonSocial          NVARCHAR(200) NOT NULL,
        NombreComercial      NVARCHAR(200) NULL,
        Rubro                NVARCHAR(100) NULL,
        Documento            NVARCHAR(50) NULL,
        Email                NVARCHAR(150) NULL,
        Telefono             NVARCHAR(50) NULL,
        ContactoNombre       NVARCHAR(150) NULL,
        ContactoCargo        NVARCHAR(100) NULL,
        ContactoEmail        NVARCHAR(150) NULL,
        ContactoTelefono     NVARCHAR(50) NULL,
        EstadoPipeline       NVARCHAR(40) NOT NULL CONSTRAINT DF_CrmClientes_Estado DEFAULT (N'Prospecto'),
        SistemaServicio      NVARCHAR(200) NULL,
        ProximosPasos        NVARCHAR(500) NULL,
        FechaEstimadaCierre  DATE NULL,
        FeeEstimadoMensual   DECIMAL(18,2) NULL,
        Notas                NVARCHAR(MAX) NULL,
        Activo               BIT NOT NULL CONSTRAINT DF_CrmClientes_Activo DEFAULT (1),
        FechaCreacion        DATETIME2 NOT NULL CONSTRAINT DF_CrmClientes_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion   DATETIME2 NULL,
        UsuarioResponsableId INT NULL,
        CONSTRAINT FK_CrmClientes_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmClientes_Estado CHECK (EstadoPipeline IN (
            N'Prospecto', N'Contactado', N'En negociacion',
            N'Propuesta enviada', N'Cerrado', N'Cliente activo'
        ))
    );
    CREATE INDEX IX_CrmClientes_Empresa ON dbo.CrmClientes (EmpresaId, Activo);
    CREATE INDEX IX_CrmClientes_Estado ON dbo.CrmClientes (EmpresaId, EstadoPipeline);
    PRINT 'CrmClientes creada';
END
GO

IF OBJECT_ID(N'dbo.CrmHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmHistorial (
        HistorialId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmHistorial PRIMARY KEY,
        ClienteId       INT NOT NULL,
        EmpresaId       INT NOT NULL,
        Tipo            NVARCHAR(40) NOT NULL,
        Titulo          NVARCHAR(200) NOT NULL,
        Detalle         NVARCHAR(MAX) NULL,
        EstadoAnterior  NVARCHAR(40) NULL,
        EstadoNuevo     NVARCHAR(40) NULL,
        Fecha           DATETIME2 NOT NULL CONSTRAINT DF_CrmHistorial_Fecha DEFAULT (SYSUTCDATETIME()),
        UsuarioId       INT NULL,
        UsuarioNombre   NVARCHAR(150) NULL,
        CONSTRAINT FK_CrmHistorial_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes(ClienteId) ON DELETE CASCADE,
        CONSTRAINT FK_CrmHistorial_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId)
    );
    CREATE INDEX IX_CrmHistorial_Cliente ON dbo.CrmHistorial (ClienteId, Fecha DESC);
    PRINT 'CrmHistorial creada';
END
GO

IF OBJECT_ID(N'dbo.CrmContratos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmContratos (
        ContratoId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmContratos PRIMARY KEY,
        EmpresaId          INT NOT NULL,
        ClienteId          INT NOT NULL,
        SistemaServicio    NVARCHAR(200) NOT NULL,
        FeeAcordado        DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmContratos_FeeAcordado DEFAULT (0),
        FeeMensual         DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmContratos_FeeMensual DEFAULT (0),
        FeeAnual           DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmContratos_FeeAnual DEFAULT (0),
        FechaInicio        DATE NOT NULL,
        ProximoVencimiento DATE NULL,
        EstadoPago         NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmContratos_EstadoPago DEFAULT (N'Pendiente'),
        EstadoContrato     NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmContratos_EstadoContrato DEFAULT (N'Activo'),
        EsRecurrente       BIT NOT NULL CONSTRAINT DF_CrmContratos_Recurrente DEFAULT (1),
        Periodicidad       NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmContratos_Periodicidad DEFAULT (N'Mensual'),
        Notas              NVARCHAR(MAX) NULL,
        Activo             BIT NOT NULL CONSTRAINT DF_CrmContratos_Activo DEFAULT (1),
        FechaCreacion      DATETIME2 NOT NULL CONSTRAINT DF_CrmContratos_Creacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NULL,
        CONSTRAINT FK_CrmContratos_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_CrmContratos_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes(ClienteId),
        CONSTRAINT CK_CrmContratos_EstadoPago CHECK (EstadoPago IN (N'Al dia', N'Pendiente', N'Vencido', N'Pagado')),
        CONSTRAINT CK_CrmContratos_EstadoContrato CHECK (EstadoContrato IN (N'Borrador', N'Activo', N'Suspendido', N'Finalizado')),
        CONSTRAINT CK_CrmContratos_Periodicidad CHECK (Periodicidad IN (N'Mensual', N'Anual'))
    );
    CREATE INDEX IX_CrmContratos_Empresa ON dbo.CrmContratos (EmpresaId, Activo);
    CREATE INDEX IX_CrmContratos_Cliente ON dbo.CrmContratos (ClienteId, Activo);
    PRINT 'CrmContratos creada';
END
GO

IF OBJECT_ID(N'dbo.CrmPagos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmPagos (
        PagoId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmPagos PRIMARY KEY,
        EmpresaId      INT NOT NULL,
        ContratoId     INT NOT NULL,
        ClienteId      INT NOT NULL,
        Monto          DECIMAL(18,2) NOT NULL,
        FechaPago      DATE NOT NULL,
        Periodo        NVARCHAR(20) NULL,
        MetodoPago     NVARCHAR(50) NULL,
        Concepto       NVARCHAR(200) NULL,
        Estado         NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmPagos_Estado DEFAULT (N'Registrado'),
        Notas          NVARCHAR(500) NULL,
        FechaCreacion  DATETIME2 NOT NULL CONSTRAINT DF_CrmPagos_Creacion DEFAULT (SYSUTCDATETIME()),
        UsuarioId      INT NULL,
        UsuarioNombre  NVARCHAR(150) NULL,
        CONSTRAINT FK_CrmPagos_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_CrmPagos_Contrato FOREIGN KEY (ContratoId) REFERENCES dbo.CrmContratos(ContratoId),
        CONSTRAINT FK_CrmPagos_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.CrmClientes(ClienteId)
    );
    CREATE INDEX IX_CrmPagos_Empresa ON dbo.CrmPagos (EmpresaId, FechaPago DESC);
    CREATE INDEX IX_CrmPagos_Contrato ON dbo.CrmPagos (ContratoId, FechaPago DESC);
    PRINT 'CrmPagos creada';
END
GO

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
    PRINT 'CrmTareas creada';
END
GO

IF OBJECT_ID(N'dbo.CrmSocios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmSocios (
        SocioId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmSocios PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        Nombre        NVARCHAR(150) NOT NULL,
        Email         NVARCHAR(150) NULL,
        Porcentaje    DECIMAL(5,2) NOT NULL,
        UsuarioId     INT NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_CrmSocios_Activo DEFAULT (1),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_CrmSocios_Creacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CrmSocios_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_CrmSocios_Porcentaje CHECK (Porcentaje >= 0 AND Porcentaje <= 100)
    );
    CREATE INDEX IX_CrmSocios_Empresa ON dbo.CrmSocios (EmpresaId, Activo);
    PRINT 'CrmSocios creada';
END
GO

------------------------------------------------------------
-- C) DATOS DE EJEMPLO (solo si vacio)
------------------------------------------------------------
DECLARE @EmpresaId INT =
    (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' AND RazonSocial = N'Vordia');

IF @EmpresaId IS NULL
BEGIN
    PRINT 'ERROR: Empresa Vordia no encontrada.';
    RETURN;
END

DECLARE @EliId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py');
DECLARE @NeryId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py');

-- Clientes
IF NOT EXISTS (SELECT 1 FROM dbo.CrmClientes WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.CrmClientes (
        EmpresaId, RazonSocial, NombreComercial, Rubro, Documento, Email, Telefono,
        ContactoNombre, ContactoCargo, ContactoEmail, ContactoTelefono,
        EstadoPipeline, SistemaServicio, ProximosPasos, FechaEstimadaCierre,
        FeeEstimadoMensual, Notas, Activo, UsuarioResponsableId
    ) VALUES
    (@EmpresaId, N'Fontana Di Luque', N'Fontana', N'Hospedaje', N'80000001-1',
     N'admin@fontana.com.py', N'0981 111 111', N'Maria Lopez', N'Administracion', N'maria@fontana.com.py', N'0981 111 112',
     N'Cliente activo', N'Sistema de hospedaje / reservas', N'Renovacion anual Q4', DATEADD(MONTH, 4, CAST(GETDATE() AS DATE)),
     2500000, N'Cliente activo de referencia Vordia.', 1, @EliId),
    (@EmpresaId, N'Clinica Santa Rita', N'Santa Rita', N'Salud', N'80000002-2',
     N'gerencia@santarita.com.py', N'0982 222 222', N'Dr. Juan Perez', N'Director', N'jperez@santarita.com.py', N'0982 222 223',
     N'En negociacion', N'Sistema de turnos + ficha clinica', N'Enviar demo personalizada', DATEADD(DAY, 20, CAST(GETDATE() AS DATE)),
     1800000, N'Interesados en agenda y WhatsApp.', 1, @EliId),
    (@EmpresaId, N'Constructora del Este SA', N'CDE Construcciones', N'Construccion', N'80000003-3',
     N'info@cde.com.py', N'0971 333 333', N'Ana Gomez', N'Gerente comercial', N'ana@cde.com.py', N'0971 333 334',
     N'Prospecto', N'CRM + facturacion', N'Primera llamada de discovery', DATEADD(DAY, 45, CAST(GETDATE() AS DATE)),
     1200000, N'Lead de LinkedIn.', 1, @NeryId),
    (@EmpresaId, N'Hotel Paraguay Plaza', N'HPP', N'Hoteleria', N'80000004-4',
     N'ops@hpp.com.py', N'0961 444 444', N'Carlos Ruiz', N'Gerente ops', N'cruiz@hpp.com.py', N'0961 444 445',
     N'Propuesta enviada', N'PMS / reservas multi-propiedad', N'Seguimiento propuesta lunes', DATEADD(DAY, 10, CAST(GETDATE() AS DATE)),
     3200000, N'Propuesta enviada por mail.', 1, @NeryId),
    (@EmpresaId, N'Retail Sur SRL', N'Retail Sur', N'Retail', N'80000005-5',
     N'compras@retailsur.com.py', N'0972 555 555', N'Lucia Vera', N'TI', N'lucia@retailsur.com.py', N'0972 555 556',
     N'Contactado', N'Inventario + caja POS', N'Agendar segunda reunion', DATEADD(DAY, 30, CAST(GETDATE() AS DATE)),
     900000, N'Respondieron al correo inicial.', 1, @EliId),
    (@EmpresaId, N'Agro Norte PY', N'Agro Norte', N'Agro', N'80000006-6',
     N'admin@agronorte.com.py', N'0983 666 666', N'Roberto Diaz', N'CFO', N'rdiaz@agronorte.com.py', N'0983 666 667',
     N'Cerrado', N'Contabilidad + reportes', N'Kickoff implementacion', DATEADD(DAY, 5, CAST(GETDATE() AS DATE)),
     1500000, N'Cerrado verbal; falta contrato formal.', 1, @NeryId);

    INSERT INTO dbo.CrmHistorial (ClienteId, EmpresaId, Tipo, Titulo, Detalle, EstadoAnterior, EstadoNuevo, UsuarioId, UsuarioNombre)
    SELECT c.ClienteId, @EmpresaId, N'cambio_estado', N'Alta en CRM', N'Cliente de ejemplo (seed servidor).', NULL, c.EstadoPipeline, @NeryId, N'Nery Chavez'
    FROM dbo.CrmClientes c WHERE c.EmpresaId = @EmpresaId;

    PRINT 'Clientes ejemplo OK';
END

DECLARE @Fontana INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Fontana Di Luque');
DECLARE @Agro INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Agro Norte PY');
DECLARE @Hotel INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Hotel Paraguay Plaza');
DECLARE @Clinica INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Clinica Santa Rita');

-- Contratos / pagos
IF NOT EXISTS (SELECT 1 FROM dbo.CrmContratos WHERE EmpresaId = @EmpresaId)
BEGIN
    IF @Fontana IS NOT NULL
    BEGIN
        INSERT INTO dbo.CrmContratos (
            EmpresaId, ClienteId, SistemaServicio, FeeAcordado, FeeMensual, FeeAnual,
            FechaInicio, ProximoVencimiento, EstadoPago, EstadoContrato, EsRecurrente, Periodicidad, Notas
        ) VALUES (
            @EmpresaId, @Fontana, N'Sistema de hospedaje / reservas',
            2500000, 2500000, 30000000,
            DATEADD(MONTH, -6, CAST(GETDATE() AS DATE)),
            DATEADD(MONTH, 1, CAST(GETDATE() AS DATE)),
            N'Al dia', N'Activo', 1, N'Mensual', N'Contrato recurrente de referencia.'
        );
        DECLARE @CFontana INT = SCOPE_IDENTITY();
        INSERT INTO dbo.CrmPagos (EmpresaId, ContratoId, ClienteId, Monto, FechaPago, Periodo, MetodoPago, Concepto, UsuarioId, UsuarioNombre)
        VALUES
        (@EmpresaId, @CFontana, @Fontana, 2500000, DATEADD(MONTH,-2,CAST(GETDATE() AS DATE)), FORMAT(DATEADD(MONTH,-2,GETDATE()),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez'),
        (@EmpresaId, @CFontana, @Fontana, 2500000, DATEADD(MONTH,-1,CAST(GETDATE() AS DATE)), FORMAT(DATEADD(MONTH,-1,GETDATE()),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez'),
        (@EmpresaId, @CFontana, @Fontana, 2500000, CAST(GETDATE() AS DATE), FORMAT(GETDATE(),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez');
    END

    IF @Agro IS NOT NULL
        INSERT INTO dbo.CrmContratos (
            EmpresaId, ClienteId, SistemaServicio, FeeAcordado, FeeMensual, FeeAnual,
            FechaInicio, ProximoVencimiento, EstadoPago, EstadoContrato, EsRecurrente, Periodicidad, Notas
        ) VALUES (
            @EmpresaId, @Agro, N'Contabilidad + reportes',
            1500000, 1500000, 18000000,
            DATEADD(DAY, -10, CAST(GETDATE() AS DATE)),
            DATEADD(DAY, 20, CAST(GETDATE() AS DATE)),
            N'Pendiente', N'Activo', 1, N'Mensual', N'Primer pago pendiente.'
        );

    IF @Hotel IS NOT NULL
        INSERT INTO dbo.CrmContratos (
            EmpresaId, ClienteId, SistemaServicio, FeeAcordado, FeeMensual, FeeAnual,
            FechaInicio, ProximoVencimiento, EstadoPago, EstadoContrato, EsRecurrente, Periodicidad, Notas
        ) VALUES (
            @EmpresaId, @Hotel, N'PMS / reservas multi-propiedad',
            3200000, 3200000, 38400000,
            DATEADD(DAY, 15, CAST(GETDATE() AS DATE)),
            DATEADD(DAY, 45, CAST(GETDATE() AS DATE)),
            N'Pendiente', N'Borrador', 1, N'Mensual', N'Propuesta enviada; borrador.'
        );

    PRINT 'Contratos/pagos ejemplo OK';
END

-- Socios
IF NOT EXISTS (SELECT 1 FROM dbo.CrmSocios WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.CrmSocios (EmpresaId, Nombre, Email, Porcentaje, UsuarioId) VALUES
    (@EmpresaId, N'Eli Ortigoza', N'eli.ortigoza@vordia.com.py', 50.00, @EliId),
    (@EmpresaId, N'Nery Chavez', N'nery.chavez@vordia.com.py', 50.00, @NeryId);
    PRINT 'Socios 50/50 OK';
END

-- Tareas
IF NOT EXISTS (SELECT 1 FROM dbo.CrmTareas WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.CrmTareas (EmpresaId, Titulo, Descripcion, FechaVencimiento, Estado, Prioridad, ClienteId, AsignadoUsuarioId, AsignadoNombre)
    VALUES
    (@EmpresaId, N'Enviar demo Clinica Santa Rita', N'Preparar demo de turnos y WhatsApp.', DATEADD(DAY, 3, CAST(GETDATE() AS DATE)), N'En curso', N'Alta', @Clinica, @EliId, N'Eli Ortigoza'),
    (@EmpresaId, N'Seguimiento propuesta Hotel Paraguay', N'Llamar para feedback de la propuesta PMS.', DATEADD(DAY, 2, CAST(GETDATE() AS DATE)), N'Pendiente', N'Alta', @Hotel, @NeryId, N'Nery Chavez'),
    (@EmpresaId, N'Cobrar fee Agro Norte (primer mes)', N'Registrar pago pendiente en Ingresos.', DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), N'Pendiente', N'Media', @Agro, @NeryId, N'Nery Chavez'),
    (@EmpresaId, N'Revision renovacion Fontana Q4', N'Revisar alcance y fee para renovacion.', DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), N'Pendiente', N'Baja', @Fontana, @EliId, N'Eli Ortigoza'),
    (@EmpresaId, N'Definir % reparto socios 2026', N'Confirmar 50/50 o ajustar en Flujo de caja.', DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), N'Hecha', N'Media', NULL, @NeryId, N'Nery Chavez');

    UPDATE dbo.CrmTareas SET FechaCompletada = SYSUTCDATETIME()
    WHERE EmpresaId=@EmpresaId AND Titulo LIKE N'Definir % reparto%';
    PRINT 'Tareas ejemplo OK';
END

PRINT '==== FIN Seed Vordia Completo ====';
PRINT 'Login: eli.ortigoza@vordia.com.py / eli12345';
PRINT 'Login: nery.chavez@vordia.com.py / nery12345';

SELECT EmpresaId, RazonSocial, Rubro FROM dbo.Empresas WHERE EmpresaId = @EmpresaId;
SELECT UsuarioId, Nombre, Email, Rol FROM dbo.Usuarios WHERE EmpresaId = @EmpresaId;
SELECT COUNT(*) AS ModulosVordia FROM dbo.Modulos WHERE RubroAsociado = N'VORDIA';
SELECT COUNT(*) AS Clientes FROM dbo.CrmClientes WHERE EmpresaId = @EmpresaId;
SELECT COUNT(*) AS Contratos FROM dbo.CrmContratos WHERE EmpresaId = @EmpresaId;
SELECT COUNT(*) AS Pagos FROM dbo.CrmPagos WHERE EmpresaId = @EmpresaId;
SELECT COUNT(*) AS Tareas FROM dbo.CrmTareas WHERE EmpresaId = @EmpresaId;
SELECT Nombre, Porcentaje FROM dbo.CrmSocios WHERE EmpresaId = @EmpresaId;
GO
