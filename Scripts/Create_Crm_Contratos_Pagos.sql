/*
  CRM Vordia — Contratos / Fees / Pagos / Ingresos recurrentes
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

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
    PRINT 'Tabla CrmContratos creada';
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
    PRINT 'Tabla CrmPagos creada';
END
GO

DECLARE @EmpresaId INT;
SELECT @EmpresaId = EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' AND RazonSocial = N'Vordia';

IF @EmpresaId IS NULL
BEGIN
    PRINT 'ERROR: Empresa Vordia no encontrada.';
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.CrmContratos WHERE EmpresaId = @EmpresaId)
BEGIN
    DECLARE @Fontana INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Fontana Di Luque');
    DECLARE @Agro INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Agro Norte PY');
    DECLARE @Hotel INT = (SELECT TOP 1 ClienteId FROM dbo.CrmClientes WHERE EmpresaId=@EmpresaId AND RazonSocial=N'Hotel Paraguay Plaza');
    DECLARE @NeryId INT = (SELECT TOP 1 UsuarioId FROM dbo.Usuarios WHERE Email=N'nery.chavez@vordia.com.py');

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
            N'Al dia', N'Activo', 1, N'Mensual',
            N'Contrato recurrente de referencia.'
        );

        DECLARE @CFontana INT = SCOPE_IDENTITY();

        INSERT INTO dbo.CrmPagos (EmpresaId, ContratoId, ClienteId, Monto, FechaPago, Periodo, MetodoPago, Concepto, UsuarioId, UsuarioNombre)
        VALUES
        (@EmpresaId, @CFontana, @Fontana, 2500000, DATEADD(MONTH, -2, CAST(GETDATE() AS DATE)), FORMAT(DATEADD(MONTH,-2,GETDATE()),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez'),
        (@EmpresaId, @CFontana, @Fontana, 2500000, DATEADD(MONTH, -1, CAST(GETDATE() AS DATE)), FORMAT(DATEADD(MONTH,-1,GETDATE()),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez'),
        (@EmpresaId, @CFontana, @Fontana, 2500000, CAST(GETDATE() AS DATE), FORMAT(GETDATE(),'yyyy-MM'), N'Transferencia', N'Fee mensual', @NeryId, N'Nery Chavez');
    END

    IF @Agro IS NOT NULL
    BEGIN
        INSERT INTO dbo.CrmContratos (
            EmpresaId, ClienteId, SistemaServicio, FeeAcordado, FeeMensual, FeeAnual,
            FechaInicio, ProximoVencimiento, EstadoPago, EstadoContrato, EsRecurrente, Periodicidad, Notas
        ) VALUES (
            @EmpresaId, @Agro, N'Contabilidad + reportes',
            1500000, 1500000, 18000000,
            DATEADD(DAY, -10, CAST(GETDATE() AS DATE)),
            DATEADD(DAY, 20, CAST(GETDATE() AS DATE)),
            N'Pendiente', N'Activo', 1, N'Mensual',
            N'Cerrado; primer pago pendiente de cobro.'
        );
    END

    IF @Hotel IS NOT NULL
    BEGIN
        INSERT INTO dbo.CrmContratos (
            EmpresaId, ClienteId, SistemaServicio, FeeAcordado, FeeMensual, FeeAnual,
            FechaInicio, ProximoVencimiento, EstadoPago, EstadoContrato, EsRecurrente, Periodicidad, Notas
        ) VALUES (
            @EmpresaId, @Hotel, N'PMS / reservas multi-propiedad',
            3200000, 3200000, 38400000,
            DATEADD(DAY, 15, CAST(GETDATE() AS DATE)),
            DATEADD(DAY, 45, CAST(GETDATE() AS DATE)),
            N'Pendiente', N'Borrador', 1, N'Mensual',
            N'Propuesta enviada; contrato en borrador.'
        );
    END

    PRINT 'Seed contratos/pagos cargado.';
END
ELSE
    PRINT 'CrmContratos ya tiene datos; seed omitido.';

SELECT c.ContratoId, cl.RazonSocial, c.SistemaServicio, c.FeeMensual, c.EstadoPago, c.EstadoContrato, c.ProximoVencimiento
FROM dbo.CrmContratos c
JOIN dbo.CrmClientes cl ON cl.ClienteId = c.ClienteId
WHERE c.EmpresaId = @EmpresaId;

SELECT COUNT(*) AS Pagos FROM dbo.CrmPagos WHERE EmpresaId = @EmpresaId;
GO
