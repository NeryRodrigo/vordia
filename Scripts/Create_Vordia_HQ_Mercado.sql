/*
  VORDIA HQ — Necesidades del Mercado
  Radar de pedidos públicos de sistemas y aplicaciones para convertirlos en ventas y microapps:
  DNCP Paraguay, SECOP II Colombia, Banco Mundial, Freelancer.com (es/en/pt), feeds RSS
  (Google Alerts) y carga manual / captura rápida (Workana, Facebook, LinkedIn).
  Idempotente. Guardar/ejecutar como UTF-8.
*/

USE GESCO_DB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ========== FUENTES ========== */
IF OBJECT_ID(N'dbo.HqMercadoFuentes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoFuentes (
        FuenteId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercFuente PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Tipo              NVARCHAR(20) NOT NULL,   -- Freelancer | DNCP | SECOP | BancoMundial | RSS
        Nombre            NVARCHAR(120) NOT NULL,
        Consulta          NVARCHAR(500) NULL,      -- palabras clave (Freelancer) o URL del feed (RSS)
        Activa            BIT NOT NULL CONSTRAINT DF_HqMercFuente_Activa DEFAULT (1),
        UltimaSync        DATETIME2 NULL,
        UltimoResultado   NVARCHAR(300) NULL,
        FechaCreacion     DATETIME2 NOT NULL CONSTRAINT DF_HqMercFuente_Cre DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HqMercFuente_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_HqMercFuente_Tipo CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'RSS'))
    );
    PRINT 'HqMercadoFuentes creada';
END
ELSE PRINT 'HqMercadoFuentes ya existe';
GO

/* ========== NECESIDADES DETECTADAS ========== */
IF OBJECT_ID(N'dbo.HqMercadoNecesidades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoNecesidades (
        NecesidadId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercNec PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Fuente            NVARCHAR(20) NOT NULL,       -- Freelancer | DNCP | RSS | Manual
        ExternoId         NVARCHAR(150) NOT NULL,      -- id en la fuente (deduplicación)
        Titulo            NVARCHAR(400) NOT NULL,
        Descripcion       NVARCHAR(MAX) NULL,
        Url               NVARCHAR(600) NULL,
        Solicitante       NVARCHAR(250) NULL,
        Pais              NVARCHAR(60) NULL,
        PresupuestoMin    DECIMAL(18,2) NULL,
        PresupuestoMax    DECIMAL(18,2) NULL,
        Moneda            NVARCHAR(10) NULL,
        FechaPublicacion  DATETIME2 NULL,
        FechaCierre       DATETIME2 NULL,
        Habilidades       NVARCHAR(400) NULL,
        Vertical          NVARCHAR(30) NULL,           -- HABITAT | STAY | DINE | RETAIL | BOUTIQUE | OPS | AGRO | HQ | A_MEDIDA
        Categoria         NVARCHAR(60) NULL,           -- Punto de venta, Reservas, App móvil, ...
        ProductoSugerido  NVARCHAR(300) NULL,
        Puntaje           INT NOT NULL CONSTRAINT DF_HqMercNec_Puntaje DEFAULT (0),
        MotivoPuntaje     NVARCHAR(500) NULL,
        Confianza         INT NOT NULL CONSTRAINT DF_HqMercNec_Confianza DEFAULT (0),  -- 0-100: qué tan seguro es el comprador / el cobro
        ConfianzaMotivo   NVARCHAR(300) NULL,
        Competidores      INT NULL,                    -- propuestas ya enviadas (Freelancer)
        Alcance           NVARCHAR(20) NULL,           -- Paraguay | LatAm | Internacional
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercNec_Estado DEFAULT (N'Nueva'), -- Nueva | Evaluando | Contactada | Propuesta | Ganada | Descartada
        ResponsableNombre NVARCHAR(150) NULL,
        Notas             NVARCHAR(MAX) NULL,
        ClienteId         INT NULL,                    -- lead creado en CRM
        RoadmapId         INT NULL,                    -- idea de producto rápido
        FechaCaptura      DATETIME2 NOT NULL CONSTRAINT DF_HqMercNec_Cap DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2 NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_HqMercNec_Activo DEFAULT (1),
        CONSTRAINT FK_HqMercNec_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT CK_HqMercNec_Estado CHECK (Estado IN (N'Nueva', N'Evaluando', N'Contactada', N'Propuesta', N'Ganada', N'Descartada'))
    );
    CREATE UNIQUE INDEX UX_HqMercNec_Externo ON dbo.HqMercadoNecesidades (EmpresaId, Fuente, ExternoId);
    CREATE INDEX IX_HqMercNec_Lista ON dbo.HqMercadoNecesidades (EmpresaId, Activo, Estado, Puntaje DESC);
    PRINT 'HqMercadoNecesidades creada';
END
ELSE PRINT 'HqMercadoNecesidades ya existe';
GO

/* ========== ACTUALIZACIÓN v2 (bases creadas con la primera versión) ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Confianza') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Confianza INT NOT NULL CONSTRAINT DF_HqMercNec_Confianza DEFAULT (0);
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ConfianzaMotivo') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ConfianzaMotivo NVARCHAR(300) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Competidores') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Competidores INT NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'Alcance') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD Alcance NVARCHAR(20) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo'
           AND definition NOT LIKE N'%BancoMundial%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'RSS'));
    PRINT 'Tipos de fuente ampliados';
END
GO

/* ========== FUENTES INICIALES PARA VORDIA HQ ========== */
DECLARE @EmpresaId INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);

IF @EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    VALUES
    (@EmpresaId, N'DNCP', N'Licitaciones públicas Paraguay (DNCP)', NULL),
    (@EmpresaId, N'Freelancer', N'Freelancer · sistemas de gestión', N'sistema de gestion'),
    (@EmpresaId, N'Freelancer', N'Freelancer · software / ERP', N'software'),
    (@EmpresaId, N'Freelancer', N'Freelancer · aplicaciones móviles', N'app movil'),
    (@EmpresaId, N'Freelancer', N'Freelancer · punto de venta / inventario', N'inventario'),
    (@EmpresaId, N'Freelancer', N'Freelancer · reservas / turnos', N'reservas'),
    (@EmpresaId, N'Freelancer', N'Freelancer · sitio web para negocio', N'pagina web');
    PRINT CONCAT('Fuentes iniciales creadas para EmpresaId=', @EmpresaId);
END
ELSE PRINT 'Fuentes iniciales omitidas (ya existen o no hay empresa VORDIA)';
GO

/* ========== FUENTES INTERNACIONALES (v2) ========== */
DECLARE @Emp INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    SELECT @Emp, v.Tipo, v.Nombre, v.Consulta
    FROM (VALUES
        (N'SECOP', N'Licitaciones públicas Colombia (SECOP II)', NULL),
        (N'BancoMundial', N'Licitaciones Banco Mundial (internacional)', N'software information system digital platform'),
        (N'Freelancer', N'Freelancer · software (inglés, internacional)', N'software development|en'),
        (N'Freelancer', N'Freelancer · web app / SaaS (inglés)', N'web app|en'),
        (N'Freelancer', N'Freelancer · plataforma web (español)', N'plataforma web')
    ) AS v(Tipo, Nombre, Consulta)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp AND f.Tipo = v.Tipo AND ISNULL(f.Consulta, N'') = ISNULL(v.Consulta, N''));
    PRINT CONCAT('Fuentes internacionales agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v3: CONTACTO Y PEDIDOS RÁPIDOS ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoNombre') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoNombre NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoEmail') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoEmail NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoTelefono') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoTelefono NVARCHAR(40) NULL;
GO

/* Historial de contactos (WhatsApp, correo, plataforma, llamada) */
IF OBJECT_ID(N'dbo.HqMercadoContactos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoContactos (
        ContactoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercContacto PRIMARY KEY,
        EmpresaId      INT NOT NULL,
        NecesidadId    INT NOT NULL,
        Canal          NVARCHAR(20) NOT NULL,           -- WhatsApp | Correo | Plataforma | Llamada
        Destino        NVARCHAR(150) NULL,
        Asunto         NVARCHAR(200) NULL,
        Mensaje        NVARCHAR(MAX) NULL,
        Resultado      NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercContacto_Res DEFAULT (N'Registrado'), -- Registrado | Enviado | Error
        Detalle        NVARCHAR(300) NULL,
        UsuarioNombre  NVARCHAR(150) NULL,
        Fecha          DATETIME2 NOT NULL CONSTRAINT DF_HqMercContacto_Fecha DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_HqMercContacto_Nec FOREIGN KEY (NecesidadId) REFERENCES dbo.HqMercadoNecesidades(NecesidadId)
    );
    CREATE INDEX IX_HqMercContacto_Nec ON dbo.HqMercadoContactos (NecesidadId, Fecha DESC);
    PRINT 'HqMercadoContactos creada';
END
GO

/* Pedidos / propuestas rápidas con link público para aceptar */
IF OBJECT_ID(N'dbo.HqMercadoPedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqMercadoPedidos (
        PedidoId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqMercPedido PRIMARY KEY,
        EmpresaId           INT NOT NULL,
        NecesidadId         INT NULL,
        Token               NVARCHAR(40) NOT NULL,
        Numero              NVARCHAR(20) NOT NULL,
        ClienteNombre       NVARCHAR(200) NOT NULL,
        ClienteEmail        NVARCHAR(150) NULL,
        ClienteTelefono     NVARCHAR(40) NULL,
        Producto            NVARCHAR(200) NOT NULL,
        Incluye             NVARCHAR(MAX) NULL,
        Moneda              NVARCHAR(10) NOT NULL CONSTRAINT DF_HqMercPedido_Moneda DEFAULT (N'USD'),
        PrecioSetup         DECIMAL(18,2) NOT NULL CONSTRAINT DF_HqMercPedido_Setup DEFAULT (0),
        PrecioMensual       DECIMAL(18,2) NOT NULL CONSTRAINT DF_HqMercPedido_Mensual DEFAULT (0),
        PlazoDias           INT NOT NULL CONSTRAINT DF_HqMercPedido_Plazo DEFAULT (7),
        ValidoHasta         DATETIME2 NOT NULL,
        CondicionesPago     NVARCHAR(500) NULL,
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_HqMercPedido_Estado DEFAULT (N'Borrador'),
        FechaEnvio          DATETIME2 NULL,
        FechaVisto          DATETIME2 NULL,
        FechaRespuesta      DATETIME2 NULL,
        RespuestaNombre     NVARCHAR(150) NULL,
        RespuestaComentario NVARCHAR(1000) NULL,
        FechaPago           DATETIME2 NULL,
        UsuarioNombre       NVARCHAR(150) NULL,
        FechaCreacion       DATETIME2 NOT NULL CONSTRAINT DF_HqMercPedido_Cre DEFAULT (SYSUTCDATETIME()),
        Activo              BIT NOT NULL CONSTRAINT DF_HqMercPedido_Activo DEFAULT (1),
        CONSTRAINT FK_HqMercPedido_Empresa FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas(EmpresaId),
        CONSTRAINT FK_HqMercPedido_Nec FOREIGN KEY (NecesidadId) REFERENCES dbo.HqMercadoNecesidades(NecesidadId),
        CONSTRAINT CK_HqMercPedido_Estado CHECK (Estado IN (N'Borrador', N'Enviado', N'Visto', N'Aceptado', N'Rechazado', N'Pagado', N'Vencido'))
    );
    CREATE UNIQUE INDEX UX_HqMercPedido_Token ON dbo.HqMercadoPedidos (Token);
    CREATE INDEX IX_HqMercPedido_Empresa ON dbo.HqMercadoPedidos (EmpresaId, Activo, Estado);
    PRINT 'HqMercadoPedidos creada';
END
GO

/* ========== v4: CONTACTO OFICIAL Y FUENTES BRASIL / REINO UNIDO ========== */
IF COL_LENGTH(N'dbo.HqMercadoNecesidades', N'ContactoOrigen') IS NULL
    ALTER TABLE dbo.HqMercadoNecesidades ADD ContactoOrigen NVARCHAR(60) NULL;   -- de dónde salió el contacto (publicado por la entidad, cargado a mano)
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo' AND definition NOT LIKE N'%PNCP%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'PNCP', N'ContractsFinder', N'RSS'));
    PRINT 'Tipos de fuente: PNCP y ContractsFinder habilitados';
END
GO

DECLARE @Emp4 INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp4 IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta)
    SELECT @Emp4, v.Tipo, v.Nombre, v.Consulta
    FROM (VALUES
        (N'PNCP', N'Licitações Brasil (PNCP) · pregão eletrônico', N'6'),
        (N'PNCP', N'Licitações Brasil (PNCP) · compra direta (dispensa)', N'8'),
        (N'ContractsFinder', N'Public tenders UK (Contracts Finder) · software', N'software'),
        (N'ContractsFinder', N'Public tenders UK (Contracts Finder) · digital platform', N'digital platform')
    ) AS v(Tipo, Nombre, Consulta)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp4 AND f.Tipo = v.Tipo AND ISNULL(f.Consulta, N'') = ISNULL(v.Consulta, N''));
    PRINT CONCAT('Fuentes Brasil / Reino Unido agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v5: SUDAMÉRICA (PERÚ, URUGUAY, CHILE, PNUD) ========== */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_HqMercFuente_Tipo' AND definition NOT LIKE N'%SEACE%')
BEGIN
    ALTER TABLE dbo.HqMercadoFuentes DROP CONSTRAINT CK_HqMercFuente_Tipo;
    ALTER TABLE dbo.HqMercadoFuentes ADD CONSTRAINT CK_HqMercFuente_Tipo
        CHECK (Tipo IN (N'Freelancer', N'DNCP', N'SECOP', N'BancoMundial', N'PNCP', N'ContractsFinder',
                        N'SEACE', N'ARCE', N'PNUD', N'MercadoPublico', N'RSS'));
    PRINT 'Tipos de fuente: SEACE, ARCE, PNUD y MercadoPublico habilitados';
END
GO

DECLARE @Emp5 INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);
IF @Emp5 IS NOT NULL
BEGIN
    INSERT INTO dbo.HqMercadoFuentes (EmpresaId, Tipo, Nombre, Consulta, Activa)
    SELECT @Emp5, v.Tipo, v.Nombre, v.Consulta, v.Activa
    FROM (VALUES
        (N'SEACE', N'Licitaciones Perú (SEACE · OECE)', N'software,sistema,aplicativo,plataforma', 1),
        (N'ARCE', N'Licitaciones Uruguay (ARCE · compras estatales)', NULL, 1),
        (N'PNUD', N'PNUD Naciones Unidas · Sudamérica (incluye Paraguay, Argentina, Bolivia, Venezuela)', NULL, 1),
        -- Chile necesita un ticket gratuito de api.mercadopublico.cl: se activa al cargarlo en la consulta
        (N'MercadoPublico', N'Licitaciones Chile (Mercado Público)', NULL, 0)
    ) AS v(Tipo, Nombre, Consulta, Activa)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HqMercadoFuentes f WHERE f.EmpresaId = @Emp5 AND f.Tipo = v.Tipo);
    PRINT CONCAT('Fuentes de Sudamérica agregadas: ', @@ROWCOUNT);
END
GO

/* ========== v7: ESTUDIO DE PRECIOS (Van Westendorp + Gabor-Granger) ========== */
IF OBJECT_ID(N'dbo.HqEstudioPreciosRespuestas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HqEstudioPreciosRespuestas (
        RespuestaId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HqEstudioPrecios PRIMARY KEY,
        EmpresaId         INT NOT NULL,
        Producto          NVARCHAR(20) NOT NULL,
        Origen            NVARCHAR(60) NULL,
        Tamano            INT NULL,
        Ciudad            NVARCHAR(80) NULL,
        HerramientaActual NVARCHAR(40) NULL,
        GastoActualGs     DECIMAL(18,2) NULL,
        ProblemaPrincipal NVARCHAR(80) NULL,
        Decisor           NVARCHAR(20) NULL,
        MuyBaratoGs       DECIMAL(18,2) NOT NULL,
        BaratoGs          DECIMAL(18,2) NOT NULL,
        CaroGs            DECIMAL(18,2) NOT NULL,
        MuyCaroGs         DECIMAL(18,2) NOT NULL,
        PrecioMostradoGs  DECIMAL(18,2) NOT NULL,
        IntencionCompra   INT NOT NULL,
        Comentario        NVARCHAR(1000) NULL,
        ContactoNombre    NVARCHAR(150) NULL,
        ContactoEmpresa   NVARCHAR(150) NULL,
        ContactoDato      NVARCHAR(150) NULL,
        AceptaContacto    BIT NOT NULL CONSTRAINT DF_HqEstudioPrecios_Acepta DEFAULT (0),
        Fecha             DATETIME2 NOT NULL CONSTRAINT DF_HqEstudioPrecios_Fecha DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_HqEstudioPrecios_Producto ON dbo.HqEstudioPreciosRespuestas (EmpresaId, Producto, Fecha);
    PRINT 'Tabla HqEstudioPreciosRespuestas creada';
END
GO

PRINT '=== VORDIA HQ · Necesidades del Mercado listo ===';
