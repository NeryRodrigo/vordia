/*
  VORDIA CONTA · Estudios contables: clientes (RUC), comprobantes, exportaciones RG90
  y el estudio de demostración (rubro CONTA). Se puede correr más de una vez.
*/
USE GESCO_DB;
GO
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ContaClientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaClientes (
        ClienteId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaClientes PRIMARY KEY,
        EmpresaId        INT NOT NULL,
        RazonSocial      NVARCHAR(250) NOT NULL,
        Ruc              NVARCHAR(20) NOT NULL,
        Dv               NVARCHAR(1) NOT NULL,
        Regimen          NVARCHAR(30) NULL,
        ObligacionIva    BIT NOT NULL CONSTRAINT DF_ContaCli_Iva DEFAULT (1),
        ObligacionIre    BIT NOT NULL CONSTRAINT DF_ContaCli_Ire DEFAULT (1),
        ObligacionIrp    BIT NOT NULL CONSTRAINT DF_ContaCli_Irp DEFAULT (0),
        ContactoNombre   NVARCHAR(150) NULL,
        ContactoWhatsapp NVARCHAR(40) NULL,
        ContactoEmail    NVARCHAR(150) NULL,
        TokenPortal      NVARCHAR(40) NOT NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_ContaCli_Activo DEFAULT (1),
        FechaAlta        DATETIME2 NOT NULL CONSTRAINT DF_ContaCli_Alta DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_ContaClientes_Empresa ON dbo.ContaClientes (EmpresaId, Ruc);
    CREATE UNIQUE INDEX UX_ContaClientes_Token ON dbo.ContaClientes (TokenPortal);
    PRINT 'Tabla ContaClientes creada';
END
GO

IF OBJECT_ID(N'dbo.ContaComprobantes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaComprobantes (
        ComprobanteId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaComprobantes PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        ClienteId         INT NOT NULL CONSTRAINT FK_ContaComp_Cliente REFERENCES dbo.ContaClientes(ClienteId),
        Tipo              NVARCHAR(10) NOT NULL,
        Origen            NVARCHAR(20) NOT NULL,
        Electronico       BIT NOT NULL CONSTRAINT DF_ContaComp_Elec DEFAULT (0),
        Cdc               NVARCHAR(44) NULL,
        TipoComprobante   INT NOT NULL CONSTRAINT DF_ContaComp_TipoComp DEFAULT (109),
        Timbrado          NVARCHAR(8) NOT NULL CONSTRAINT DF_ContaComp_Timb DEFAULT (N''),
        Numero            NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Num DEFAULT (N''),
        Fecha             DATETIME2 NOT NULL,
        Credito           BIT NOT NULL CONSTRAINT DF_ContaComp_Cred DEFAULT (0),
        ContraparteTipoId INT NOT NULL CONSTRAINT DF_ContaComp_TipoId DEFAULT (11),
        ContraparteDoc    NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Doc DEFAULT (N''),
        ContraparteDv     NVARCHAR(1) NULL,
        ContraparteNombre NVARCHAR(250) NULL,
        Gravado10         DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_G10 DEFAULT (0),
        Gravado5          DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_G5 DEFAULT (0),
        Exento            DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_Exe DEFAULT (0),
        Total             DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_Tot DEFAULT (0),
        Iva10             DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_I10 DEFAULT (0),
        Iva5              DECIMAL(18,0) NOT NULL CONSTRAINT DF_ContaComp_I5 DEFAULT (0),
        NumeroAsociado    NVARCHAR(20) NULL,
        TimbradoAsociado  NVARCHAR(8) NULL,
        ImputaIva         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIva DEFAULT (1),
        ImputaIre         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIre DEFAULT (0),
        ImputaIrp         BIT NOT NULL CONSTRAINT DF_ContaComp_ImpIrp DEFAULT (0),
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaComp_Estado DEFAULT (N'Pendiente'),
        Observaciones     NVARCHAR(1000) NULL,
        Periodo           INT NOT NULL,
        ArchivoNombre     NVARCHAR(150) NULL,
        CargadoPor        NVARCHAR(150) NULL,
        FechaCarga        DATETIME2 NOT NULL CONSTRAINT DF_ContaComp_Carga DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Tabla ContaComprobantes creada';
END
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_ClientePeriodo')
    CREATE INDEX IX_ContaComp_ClientePeriodo ON dbo.ContaComprobantes (ClienteId, Periodo, Tipo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_EmpresaPeriodo')
    CREATE INDEX IX_ContaComp_EmpresaPeriodo ON dbo.ContaComprobantes (EmpresaId, Periodo, Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaComp_Cdc')
    CREATE INDEX IX_ContaComp_Cdc ON dbo.ContaComprobantes (ClienteId, Cdc) WHERE Cdc IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.ContaExportaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaExportaciones (
        ExportacionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContaExportaciones PRIMARY KEY,
        EmpresaId     INT NOT NULL,
        ClienteId     INT NOT NULL,
        Periodo       INT NOT NULL,
        Archivo       NVARCHAR(60) NOT NULL,
        Filas         INT NOT NULL,
        GeneradoPor   NVARCHAR(150) NULL,
        Fecha         DATETIME2 NOT NULL CONSTRAINT DF_ContaExp_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_ContaExp_Cliente ON dbo.ContaExportaciones (ClienteId, Periodo);
    PRINT 'Tabla ContaExportaciones creada';
END
GO

/* Estudio de demostración: el usuario se crea desde HQ → Usuarios con el entorno "Estudio Contable Demo" */
IF NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE Rubro = N'CONTA')
BEGIN
    INSERT INTO dbo.Empresas (RazonSocial, RUC, Direccion, Rubro)
    VALUES (N'Estudio Contable Demo', N'80000000-0', N'Paraguay', N'CONTA');
    PRINT 'Empresa demo CONTA creada';
END
GO

PRINT '=== VORDIA CONTA listo ===';
GO
