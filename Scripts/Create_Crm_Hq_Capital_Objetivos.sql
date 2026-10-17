/*

  Vordia HQ — Capital empresa, Empleados (finanzas) y Objetivos (CRM/Tareas)

  Idempotente. No toca Habitat/Stay/Fontana.

  Compatible con sqlcmd (requiere QUOTED_IDENTIFIER ON por índices filtrados).

*/



USE GESCO_DB;

GO



SET NOCOUNT ON;

SET XACT_ABORT ON;

SET ANSI_NULLS ON;

SET QUOTED_IDENTIFIER ON;

SET ANSI_PADDING ON;

SET ANSI_WARNINGS ON;

SET CONCAT_NULL_YIELDS_NULL ON;

GO



IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmCapitalMovimientos (

        CapitalId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmCapitalMovimientos PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Tipo             NVARCHAR(30) NOT NULL, -- Aporte | Retiro | Ajuste | Valuacion

        MontoGs          DECIMAL(18,2) NOT NULL,

        Fecha            DATE NOT NULL,

        Concepto         NVARCHAR(300) NOT NULL,

        SocioId          INT NULL,

        UsuarioNombre    NVARCHAR(150) NULL,

        Notas            NVARCHAR(500) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmCapital_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmCapital_Creacion DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmCapital_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmCapital_Tipo CHECK (Tipo IN (N'Aporte', N'Retiro', N'Ajuste', N'Valuacion'))

    );

    PRINT 'CrmCapitalMovimientos creada';

END

ELSE PRINT 'CrmCapitalMovimientos ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmCapitalMovimientos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmCapital_EmpresaFecha' AND object_id = OBJECT_ID(N'dbo.CrmCapitalMovimientos'))

BEGIN

    CREATE INDEX IX_CrmCapital_EmpresaFecha ON dbo.CrmCapitalMovimientos (EmpresaId, Activo, Fecha DESC);

    PRINT 'IX_CrmCapital_EmpresaFecha creado';

END

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmEmpleadosHq (

        EmpleadoHqId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmEmpleadosHq PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Nombre           NVARCHAR(150) NOT NULL,

        RolCargo         NVARCHAR(120) NOT NULL,

        Email            NVARCHAR(150) NULL,

        Telefono         NVARCHAR(50) NULL,

        SalarioMensualGs DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmEmp_Salario DEFAULT (0),

        FechaIngreso     DATE NULL,

        Estado           NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmEmp_Estado DEFAULT (N'Activo'),

        Notas            NVARCHAR(500) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmEmp_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmEmp_Creacion DEFAULT (SYSUTCDATETIME()),

        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmEmp_Update DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmEmp_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmEmp_Estado CHECK (Estado IN (N'Activo', N'Inactivo', N'Licencia'))

    );

    PRINT 'CrmEmpleadosHq creada';

END

ELSE PRINT 'CrmEmpleadosHq ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmEmpleadosHq', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmEmp_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmEmpleadosHq'))

BEGIN

    CREATE INDEX IX_CrmEmp_Empresa ON dbo.CrmEmpleadosHq (EmpresaId, Activo, Estado);

    PRINT 'IX_CrmEmp_Empresa creado';

END

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NULL

BEGIN

    CREATE TABLE dbo.CrmObjetivos (

        ObjetivoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CrmObjetivos PRIMARY KEY,

        EmpresaId        INT NOT NULL,

        Ambito           NVARCHAR(20) NOT NULL, -- Empresa | Crm | Tarea

        Titulo           NVARCHAR(200) NOT NULL,

        Descripcion      NVARCHAR(MAX) NULL,

        NotasReunion     NVARCHAR(MAX) NULL, -- para compartir con socia

        Estado           NVARCHAR(30) NOT NULL CONSTRAINT DF_CrmObj_Estado DEFAULT (N'Pendiente'),

        Prioridad        NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmObj_Pri DEFAULT (N'Media'),

        FechaObjetivo    DATE NULL,

        ClienteId        INT NULL,

        TareaId          INT NULL,

        CreadoPor        NVARCHAR(150) NULL,

        Activo           BIT NOT NULL CONSTRAINT DF_CrmObj_Activo DEFAULT (1),

        FechaCreacion    DATETIME2 NOT NULL CONSTRAINT DF_CrmObj_Creacion DEFAULT (SYSUTCDATETIME()),

        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CrmObj_Update DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_CrmObj_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),

        CONSTRAINT CK_CrmObj_Ambito CHECK (Ambito IN (N'Empresa', N'Crm', N'Tarea')),

        CONSTRAINT CK_CrmObj_Estado CHECK (Estado IN (N'Pendiente', N'En curso', N'Logrado', N'Pausado', N'Cancelado')),

        CONSTRAINT CK_CrmObj_Pri CHECK (Prioridad IN (N'Baja', N'Media', N'Alta'))

    );

    PRINT 'CrmObjetivos creada';

END

ELSE PRINT 'CrmObjetivos ya existe';

GO



SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmObj_Empresa' AND object_id = OBJECT_ID(N'dbo.CrmObjetivos'))

BEGIN

    CREATE INDEX IX_CrmObj_Empresa ON dbo.CrmObjetivos (EmpresaId, Activo, Ambito, Estado);

    PRINT 'IX_CrmObj_Empresa creado';

END

GO



-- Índice filtrado: requiere QUOTED_IDENTIFIER ON (sqlcmd suele venir OFF)

SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CrmObj_Tarea' AND object_id = OBJECT_ID(N'dbo.CrmObjetivos'))

BEGIN

    CREATE INDEX IX_CrmObj_Tarea ON dbo.CrmObjetivos (TareaId) WHERE TareaId IS NOT NULL;

    PRINT 'IX_CrmObj_Tarea creado';

END

GO



-- Seed objetivos generales si empresa Vordia existe y no hay ninguno

SET QUOTED_IDENTIFIER ON;

DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA');

IF @EmpresaId IS NOT NULL

   AND ISNULL(CAST(SESSION_CONTEXT(N'vordia_sin_demo') AS INT), 0) = 0

   AND OBJECT_ID(N'dbo.CrmObjetivos', N'U') IS NOT NULL

   AND NOT EXISTS (SELECT 1 FROM dbo.CrmObjetivos WHERE EmpresaId = @EmpresaId)

BEGIN

    INSERT INTO dbo.CrmObjetivos (EmpresaId, Ambito, Titulo, Descripcion, NotasReunion, Estado, Prioridad, FechaObjetivo, CreadoPor)

    VALUES

    (@EmpresaId, N'Empresa', N'Capital operativo inicial documentado', N'Registrar aportes de capital y mantener el libro al día.', N'Reunión socios: validar montos y %.', N'En curso', N'Alta', DATEADD(MONTH, 1, CAST(GETDATE() AS DATE)), N'Sistema'),

    (@EmpresaId, N'Crm', N'Cerrar 2 clientes Habitat/Stay este trimestre', N'Pipeline enfocado en condominios y alojamiento.', N'Hablar con socia: priorizar Fontana-like vs Stay.', N'Pendiente', N'Alta', DATEADD(MONTH, 3, CAST(GETDATE() AS DATE)), N'Sistema'),

    (@EmpresaId, N'Empresa', N'Equipo HQ con roles claros', N'Cargar empleados y costos mensuales en Finanzas.', NULL, N'Pendiente', N'Media', DATEADD(MONTH, 2, CAST(GETDATE() AS DATE)), N'Sistema');

    PRINT 'Objetivos seed insertados';

END

ELSE PRINT 'Seed objetivos omitido (ya hay datos o sin empresa VORDIA)';

GO



PRINT '=== HQ Capital / Empleados / Objetivos listo ===';

GO

