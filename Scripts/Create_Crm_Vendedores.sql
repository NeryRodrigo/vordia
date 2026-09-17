/*
  VORDIA HQ — Vendedores
  =================================================================
  Crea dbo.CrmVendedores y vincula las propuestas con su vendedor.

  IMPORTANTE — vendedores NO son socios:
    - Socios (dbo.CrmSocios) son solo los duenos de VORDIA, y su
      Porcentaje es la participacion en las ganancias.
    - Vendedores (dbo.CrmVendedores) son personas de confianza que
      venden para VORDIA y cobran una comision sobre lo que cierran.
    - Los duenos pueden decidir que un vendedor pase a ser socio: en
      ese caso la API le crea su fila en CrmSocios y la deja
      referenciada en CrmVendedores.SocioId. El vendedor sigue
      vendiendo; ser socio se suma, no reemplaza.

  Las ventas de cada vendedor NO se cargan a mano: salen de las
  propuestas ganadas que tienen su VendedorId.

  Script idempotente: se puede correr varias veces sin efecto adverso.
  Requiere haber ejecutado antes Create_Crm_Propuestas.sql.

  Uso: SSMS o sqlcmd sobre GESCO_DB.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-------------------------------------------------------------
-- 1) Tabla de vendedores
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CrmVendedores
    (
        VendedorId      INT IDENTITY(1, 1) NOT NULL,
        EmpresaId       INT            NOT NULL,
        Nombre          NVARCHAR(150)  NOT NULL,
        Email           NVARCHAR(150)  NULL,
        Telefono        NVARCHAR(50)   NULL,
        ComisionPct     DECIMAL(5, 2)  NOT NULL CONSTRAINT DF_CrmVendedores_Comision DEFAULT (0),
        MetaMensualGs   DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CrmVendedores_Meta DEFAULT (0),
        Estado          NVARCHAR(20)   NOT NULL CONSTRAINT DF_CrmVendedores_Estado DEFAULT (N'Activo'),
        SocioId         INT            NULL,
        FechaSocioDesde DATETIME2(7)   NULL,
        FechaIngreso    DATETIME2(7)   NULL,
        Notas           NVARCHAR(500)  NULL,
        Activo          BIT            NOT NULL CONSTRAINT DF_CrmVendedores_Activo DEFAULT (1),
        FechaCreacion   DATETIME2(7)   NOT NULL CONSTRAINT DF_CrmVendedores_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CrmVendedores PRIMARY KEY CLUSTERED (VendedorId)
    );
    PRINT 'dbo.CrmVendedores creada';
END
ELSE PRINT 'dbo.CrmVendedores ya existe';
GO

-------------------------------------------------------------
-- 2) Reglas de dominio
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmVendedores_Estado')
BEGIN
    ALTER TABLE dbo.CrmVendedores
        ADD CONSTRAINT CK_CrmVendedores_Estado CHECK (Estado IN (N'Activo', N'Inactivo'));
    PRINT 'CK_CrmVendedores_Estado creado';
END
ELSE PRINT 'CK_CrmVendedores_Estado ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CrmVendedores_Comision')
BEGIN
    ALTER TABLE dbo.CrmVendedores
        ADD CONSTRAINT CK_CrmVendedores_Comision CHECK (ComisionPct >= 0 AND ComisionPct <= 100);
    PRINT 'CK_CrmVendedores_Comision creado';
END
ELSE PRINT 'CK_CrmVendedores_Comision ya existe u omitido';
GO

-- Vinculo opcional con la ficha de socio, cuando los duenos lo promueven.
IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CrmSocios', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CrmVendedores_Socio')
BEGIN
    ALTER TABLE dbo.CrmVendedores
        ADD CONSTRAINT FK_CrmVendedores_Socio
            FOREIGN KEY (SocioId) REFERENCES dbo.CrmSocios (SocioId);
    PRINT 'FK_CrmVendedores_Socio creado';
END
ELSE PRINT 'FK_CrmVendedores_Socio ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmVendedores_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmVendedores'))
BEGIN
    CREATE INDEX IX_CrmVendedores_Empresa
        ON dbo.CrmVendedores (EmpresaId, Activo, Estado)
        INCLUDE (Nombre, ComisionPct, MetaMensualGs, SocioId);
    PRINT 'IX_CrmVendedores_Empresa creado';
END
ELSE PRINT 'IX_CrmVendedores_Empresa ya existe';
GO

-------------------------------------------------------------
-- 3) Atribucion: que vendedor trabajo cada propuesta
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NULL
BEGIN
    RAISERROR(N'No existe dbo.CrmPropuestas. Ejecutar antes Create_Crm_Propuestas.sql', 16, 1);
END
GO

IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmPropuestas', N'VendedorId') IS NULL
BEGIN
    ALTER TABLE dbo.CrmPropuestas ADD VendedorId INT NULL;
    PRINT 'CrmPropuestas.VendedorId agregada';
END
ELSE PRINT 'CrmPropuestas.VendedorId ya existe u omitida';
GO

IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmPropuestas', N'VendedorId') IS NOT NULL
   AND OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CrmPropuestas_Vendedor')
BEGIN
    ALTER TABLE dbo.CrmPropuestas
        ADD CONSTRAINT FK_CrmPropuestas_Vendedor
            FOREIGN KEY (VendedorId) REFERENCES dbo.CrmVendedores (VendedorId);
    PRINT 'FK_CrmPropuestas_Vendedor creado';
END
ELSE PRINT 'FK_CrmPropuestas_Vendedor ya existe u omitido';
GO

IF OBJECT_ID(N'dbo.CrmPropuestas', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CrmPropuestas', N'VendedorId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmPropuestas_Vendedor' AND object_id = OBJECT_ID(N'dbo.CrmPropuestas'))
BEGIN
    CREATE INDEX IX_CrmPropuestas_Vendedor
        ON dbo.CrmPropuestas (VendedorId, Estado)
        INCLUDE (MontoGs, FechaCierre)
        WHERE Activo = 1;
    PRINT 'IX_CrmPropuestas_Vendedor creado';
END
ELSE PRINT 'IX_CrmPropuestas_Vendedor ya existe';
GO

-------------------------------------------------------------
-- 4) Verificacion
-------------------------------------------------------------
IF OBJECT_ID(N'dbo.CrmVendedores', N'U') IS NOT NULL
BEGIN
    SELECT
        Columna      = c.name,
        Tipo         = t.name,
        Permite_Null = c.is_nullable
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CrmVendedores')
    ORDER BY c.column_id;

    SELECT Socios_Actuales = COUNT(*) FROM dbo.CrmSocios WHERE Activo = 1;
END
GO

PRINT '=== CrmVendedores lista. Reiniciar la API para que EF tome la tabla. ===';
GO
