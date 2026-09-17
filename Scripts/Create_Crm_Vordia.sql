/*
  CRM Vordia — Clientes + Pipeline + Historial
  Base: GESCO_DB | Empresa Rubro = VORDIA
  Idempotente.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------
-- 1) Tablas
------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmHistorial', N'U') IS NULL
BEGIN
    -- se crea después de CrmClientes
    PRINT 'CrmHistorial pendiente (después de CrmClientes)';
END

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
    PRINT 'Tabla CrmClientes creada';
END
GO

IF OBJECT_ID(N'dbo.CrmHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmHistorial (
        HistorialId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmHistorial PRIMARY KEY,
        ClienteId       INT NOT NULL,
        EmpresaId       INT NOT NULL,
        Tipo            NVARCHAR(40) NOT NULL, -- nota | llamada | reunion | email | cambio_estado
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
    PRINT 'Tabla CrmHistorial creada';
END
GO

------------------------------------------------------------
-- 2) Datos de ejemplo (solo si la empresa Vordia no tiene clientes)
------------------------------------------------------------
DECLARE @EmpresaId INT;
SELECT @EmpresaId = EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' AND RazonSocial = N'Vordia';

IF @EmpresaId IS NULL
BEGIN
    PRINT 'ERROR: Empresa Vordia no encontrada. Ejecutá primero Seed_Vordia_Interno.sql';
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.CrmClientes WHERE EmpresaId = @EmpresaId)
BEGIN
    DECLARE @EliId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'eli.ortigoza@vordia.com.py');
    DECLARE @NeryId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email = N'nery.chavez@vordia.com.py');

    INSERT INTO dbo.CrmClientes (
        EmpresaId, RazonSocial, NombreComercial, Rubro, Documento, Email, Telefono,
        ContactoNombre, ContactoCargo, ContactoEmail, ContactoTelefono,
        EstadoPipeline, SistemaServicio, ProximosPasos, FechaEstimadaCierre,
        FeeEstimadoMensual, Notas, Activo, UsuarioResponsableId
    ) VALUES
    (@EmpresaId, N'Fontana Di Luque', N'Fontana', N'Hospedaje', N'80000001-1',
     N'admin@fontana.com.py', N'0981 111 111',
     N'Maria Lopez', N'Administracion', N'maria@fontana.com.py', N'0981 111 112',
     N'Cliente activo', N'Sistema de hospedaje / reservas', N'Renovacion anual Q4', DATEADD(MONTH, 4, CAST(GETDATE() AS DATE)),
     2500000, N'Cliente activo de referencia Vordia.', 1, @EliId),

    (@EmpresaId, N'Clinica Santa Rita', N'Santa Rita', N'Salud', N'80000002-2',
     N'gerencia@santarita.com.py', N'0982 222 222',
     N'Dr. Juan Perez', N'Director', N'jperez@santarita.com.py', N'0982 222 223',
     N'En negociacion', N'Sistema de turnos + ficha clinica', N'Enviar demo personalizada', DATEADD(DAY, 20, CAST(GETDATE() AS DATE)),
     1800000, N'Interesados en agenda y recordatorios WhatsApp.', 1, @EliId),

    (@EmpresaId, N'Constructora del Este SA', N'CDE Construcciones', N'Construccion', N'80000003-3',
     N'info@cde.com.py', N'0971 333 333',
     N'Ana Gomez', N'Gerente comercial', N'ana@cde.com.py', N'0971 333 334',
     N'Prospecto', N'CRM + facturacion', N'Primera llamada de discovery', DATEADD(DAY, 45, CAST(GETDATE() AS DATE)),
     1200000, N'Lead de LinkedIn.', 1, @NeryId),

    (@EmpresaId, N'Hotel Paraguay Plaza', N'HPP', N'Hoteleria', N'80000004-4',
     N'ops@hpp.com.py', N'0961 444 444',
     N'Carlos Ruiz', N'Gerente ops', N'cruiz@hpp.com.py', N'0961 444 445',
     N'Propuesta enviada', N'PMS / reservas multi-propiedad', N'Seguimiento propuesta lunes', DATEADD(DAY, 10, CAST(GETDATE() AS DATE)),
     3200000, N'Propuesta enviada por mail el viernes.', 1, @NeryId),

    (@EmpresaId, N'Retail Sur SRL', N'Retail Sur', N'Retail', N'80000005-5',
     N'compras@retailsur.com.py', N'0972 555 555',
     N'Lucia Vera', N'TI', N'lucia@retailsur.com.py', N'0972 555 556',
     N'Contactado', N'Inventario + caja POS', N'Agendar segunda reunion', DATEADD(DAY, 30, CAST(GETDATE() AS DATE)),
     900000, N'Respondieron al correo inicial.', 1, @EliId),

    (@EmpresaId, N'Agro Norte PY', N'Agro Norte', N'Agro', N'80000006-6',
     N'admin@agronorte.com.py', N'0983 666 666',
     N'Roberto Diaz', N'CFO', N'rdiaz@agronorte.com.py', N'0983 666 667',
     N'Cerrado', N'Contabilidad + reportes', N'Kickoff implementacion', DATEADD(DAY, 5, CAST(GETDATE() AS DATE)),
     1500000, N'Cerrado verbal; falta contrato formal.', 1, @NeryId);

    -- Historial de ejemplo
    INSERT INTO dbo.CrmHistorial (ClienteId, EmpresaId, Tipo, Titulo, Detalle, EstadoAnterior, EstadoNuevo, UsuarioId, UsuarioNombre)
    SELECT c.ClienteId, @EmpresaId, N'cambio_estado', N'Alta en CRM', N'Cliente de ejemplo cargado en seed.', NULL, c.EstadoPipeline, @NeryId, N'Nery Chavez'
    FROM dbo.CrmClientes c
    WHERE c.EmpresaId = @EmpresaId;

    INSERT INTO dbo.CrmHistorial (ClienteId, EmpresaId, Tipo, Titulo, Detalle, UsuarioId, UsuarioNombre)
    SELECT c.ClienteId, @EmpresaId, N'reunion', N'Reunion inicial', N'Se presentaron necesidades y alcance preliminar.', @EliId, N'Eli Ortigoza'
    FROM dbo.CrmClientes c
    WHERE c.EmpresaId = @EmpresaId AND c.RazonSocial = N'Clinica Santa Rita';

    INSERT INTO dbo.CrmHistorial (ClienteId, EmpresaId, Tipo, Titulo, Detalle, UsuarioId, UsuarioNombre)
    SELECT c.ClienteId, @EmpresaId, N'email', N'Propuesta comercial enviada', N'Se envio PDF con fee mensual y alcance PMS.', @NeryId, N'Nery Chavez'
    FROM dbo.CrmClientes c
    WHERE c.EmpresaId = @EmpresaId AND c.RazonSocial = N'Hotel Paraguay Plaza';

    PRINT CONCAT('Seed CRM: 6 clientes de ejemplo para EmpresaId=', @EmpresaId);
END
ELSE
BEGIN
    PRINT 'CrmClientes ya tiene datos para Vordia; seed omitido.';
END

SELECT EstadoPipeline, COUNT(*) AS Cantidad
FROM dbo.CrmClientes
WHERE EmpresaId = @EmpresaId AND Activo = 1
GROUP BY EstadoPipeline
ORDER BY EstadoPipeline;

SELECT TOP 10 ClienteId, RazonSocial, EstadoPipeline, SistemaServicio, FechaEstimadaCierre
FROM dbo.CrmClientes
WHERE EmpresaId = @EmpresaId
ORDER BY ClienteId;

GO
