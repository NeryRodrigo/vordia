/*
  ============================================================================
  VORDIA HQ · Bitácora de trabajo
  Semana pasada : lunes 19/10/2026 al domingo 25/10/2026  (lo realizado)
  Esta semana   : lunes 26/10/2026 al domingo 01/11/2026  (lo planificado)
  ============================================================================
  Qué hace:
    - Registra los proyectos en HQ → Proyectos (estado y % de avance).
    - Registra cada tarea en HQ → Tareas, con su día, estado y prioridad:
        Estados de tarea   : Pendiente | En curso | Hecha | Cancelada
        Prioridades        : Alta | Media | Baja
        Estados de proyecto: Planificado | En curso | Pausado | Hecho | Cancelado
    - Muestra al final un informe por semana, por día y por proyecto.

  Se puede correr más de una vez: si la tarea ya existe (mismo título y día) actualiza
  su estado; si no, la crea. Para marcar algo como hecho, cambiá el estado acá y volvé a correr.

  Cómo correrlo en el servidor (contenedor gesco-db):
    docker cp 04_Bitacora_Semanas_19oct_1nov_2026.sql gesco-db:/tmp/
    docker exec gesco-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$P" -C -W -s"|" -i /tmp/04_Bitacora_Semanas_19oct_1nov_2026.sql
  ============================================================================
*/
USE GESCO_DB;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LANGUAGE Spanish;
GO

DECLARE @Responsable NVARCHAR(150) = N'Rody Moran';   -- cambiá por el nombre de quien figura como responsable
DECLARE @Emp INT = (SELECT TOP 1 EmpresaId FROM dbo.Empresas WHERE Rubro = N'VORDIA' ORDER BY EmpresaId);

IF @Emp IS NULL
BEGIN
    RAISERROR(N'No se encontró la empresa VORDIA (Rubro = VORDIA).', 16, 1);
    RETURN;
END

/* ───────────────────────── Proyectos ───────────────────────── */
DECLARE @Proyectos TABLE (
    Clave NVARCHAR(20) PRIMARY KEY, Titulo NVARCHAR(200), Producto NVARCHAR(30), Estado NVARCHAR(30),
    Prioridad NVARCHAR(10), Avance INT, Inicio DATE, Fin DATE, Descripcion NVARCHAR(600));

INSERT INTO @Proyectos VALUES
 (N'CONTA',     N'VORDIA CONTA · producto para estudios contables', N'CONTA', N'En curso',    N'P0', 70, '2026-10-24', '2026-11-14',
  N'Lectura de XML y QR del SIFEN, validaciones, libros, IVA, vencimientos, archivo RG90 oficial de la DNIT y portal del cliente.'),
 (N'PRECIOS',   N'Planes, precios y estudio de precios 2026',      N'HQ',    N'En curso',    N'P0', 60, '2026-10-22', '2026-11-12',
  N'Propuesta de planes por producto y validación con prospectos (Van Westendorp + Gabor-Granger).'),
 (N'CAPTACION', N'Captación de leads: calculadoras y secuencias',  N'HQ',    N'En curso',    N'P1', 80, '2026-10-22', '2026-11-07',
  N'Calculadoras gratuitas en el sitio y secuencias de correo con IA gratuita (Groq / OpenRouter).'),
 (N'SEGURIDAD', N'Seguridad y cumplimiento legal',                 N'HQ',    N'En curso',    N'P0', 75, '2026-10-17', '2026-10-31',
  N'Accesos entre empresas, IP real detrás de Nginx, bajas de correos, páginas legales y comprobantes POS.'),
 (N'INFRA',     N'Despliegue e infraestructura del servidor',      N'HQ',    N'En curso',    N'P1', 65, '2026-10-19', '2026-11-07',
  N'Claves de despliegue, disco, scripts de base de datos, IP fija y verificación del esquema.'),
 (N'COBROS',    N'Cobro real de suscripciones',                    N'HQ',    N'Planificado', N'P0',  0, '2026-10-29', '2026-11-21',
  N'Bancard o Pagopar, suscripción mensual/anual, gracia y paso a solo lectura.');

UPDATE p SET
    p.Descripcion = x.Descripcion, p.ProductoCodigo = x.Producto, p.Estado = x.Estado, p.Prioridad = x.Prioridad,
    p.AvancePct = x.Avance, p.FechaInicio = x.Inicio, p.FechaFin = x.Fin, p.ResponsableNombre = @Responsable,
    p.FechaActualizacion = SYSUTCDATETIME(), p.Activo = 1
FROM dbo.CrmProyectos p
JOIN @Proyectos x ON x.Titulo = p.Titulo
WHERE p.EmpresaId = @Emp;

INSERT INTO dbo.CrmProyectos (EmpresaId, Titulo, Descripcion, ProductoCodigo, Estado, Prioridad, ResponsableNombre, AvancePct, FechaInicio, FechaFin, Activo, FechaCreacion, FechaActualizacion)
SELECT @Emp, x.Titulo, x.Descripcion, x.Producto, x.Estado, x.Prioridad, @Responsable, x.Avance, x.Inicio, x.Fin, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @Proyectos x
WHERE NOT EXISTS (SELECT 1 FROM dbo.CrmProyectos p WHERE p.EmpresaId = @Emp AND p.Titulo = x.Titulo);

/* ───────────────────────── Tareas ───────────────────────── */
DECLARE @Tareas TABLE (
    Fecha DATE, Proyecto NVARCHAR(20), Titulo NVARCHAR(200), Estado NVARCHAR(20), Prioridad NVARCHAR(10), Detalle NVARCHAR(1000));

INSERT INTO @Tareas (Fecha, Proyecto, Titulo, Estado, Prioridad, Detalle) VALUES
/* ===== SEMANA PASADA · 19/10 al 25/10 ===== */
-- Lunes 19
 ('2026-10-19', N'INFRA',     N'Claves SSH de despliegue para vordia y gesco-web en el servidor', N'Hecha', N'Alta',  N'Deploy keys de solo lectura por repositorio; el servidor baja el código sin tokens.'),
 ('2026-10-19', N'INFRA',     N'Bajar master al servidor (backend Agro y Necesidades del Mercado)', N'Hecha', N'Alta',  N'git pull origin master en /home/rody_moran_py/vordia.'),
-- Martes 20
 ('2026-10-20', N'INFRA',     N'Liberar espacio en disco: caché e imágenes de Docker',           N'Hecha', N'Alta',  N'De 2,3 GB a 8,7 GB libres; el build fallaba por "no space left on device".'),
 ('2026-10-20', N'INFRA',     N'Ejecutar 03_Actualizacion_Octubre_2026 en el contenedor gesco-db', N'Hecha', N'Alta', N'Backup previo; sin datos demo; tablas de Agro, Mercado, HQ, Stay y Fontana.'),
 ('2026-10-20', N'INFRA',     N'Revisar base sobrante GescoDb creada por EF el 17/08',            N'Hecha', N'Baja',  N'La API usa GESCO_DB. GescoDb queda para revisar antes de borrar.'),
-- Miércoles 21
 ('2026-10-21', N'SEGURIDAD', N'Corregir 401 en la lista de entornos del login (PR #40)',         N'Hecha', N'Alta',  N'El atributo AllowAnonymous había quedado en un método auxiliar.'),
 ('2026-10-21', N'INFRA',     N'Verificar radar de Necesidades del Mercado en producción',        N'Hecha', N'Media', N'524 necesidades nuevas en la primera pasada.'),
 ('2026-10-21', N'INFRA',     N'Desactivar fuentes Perú y Uruguay (bloquean la IP del servidor)', N'Hecha', N'Media', N'Perú responde 403 y Uruguay no responde desde Google Cloud.'),
 ('2026-10-21', N'INFRA',     N'Verificar dominios y proxy: todo apunta a 35.224.14.51',          N'Hecha', N'Media', N'Un solo servidor y un solo backend; la IP de Proxy-pass.txt estaba vieja.'),
-- Jueves 22
 ('2026-10-22', N'PRECIOS',   N'Diagnóstico CEO/CTO: qué falta para que VORDIA se venda sola',   N'Hecha', N'Alta',  N'Cobro real, prueba gratis, precios públicos, onboarding, socios y base técnica.'),
 ('2026-10-22', N'PRECIOS',   N'Plan de ejecución en 4 fases y página para compartir',            N'Hecha', N'Alta',  N'Fase 1 cobrar, fase 2 activar, fase 3 retener, fase 4 diferenciarse.'),
 ('2026-10-22', N'PRECIOS',   N'Propuesta de planes y precios por producto (vordiaPlanes)',       N'Hecha', N'Alta',  N'Tres planes por producto, en guaraníes con IVA incluido, con referencias de mercado.'),
 ('2026-10-22', N'PRECIOS',   N'Estudio de precios: encuesta pública y análisis en HQ',           N'Hecha', N'Alta',  N'/estudio/[producto] y HQ → Estudio de precios con rango aceptable.'),
 ('2026-10-22', N'CAPTACION', N'Calculadoras gratuitas: morosidad, costo por kilo y facturación', N'Hecha', N'Alta',  N'/herramientas; quien pide el informe entra como lead al CRM.'),
 ('2026-10-22', N'SEGURIDAD', N'IP real detrás de Nginx: límites de pedidos por visitante',       N'Hecha', N'Alta',  N'X-Forwarded-For solo desde redes privadas de Docker.'),
-- Viernes 23
 ('2026-10-23', N'INFRA',     N'Desplegar estudio de precios y calculadoras (PR #41)',            N'Hecha', N'Alta',  N'Pull, script 03 y reconstrucción de backend y frontend.'),
 ('2026-10-23', N'CAPTACION', N'Evaluar Instantly.ai para prospección por correo',                N'Hecha', N'Media', N'Descartado por costo y riesgo de bloqueo de la casilla; se hace propio en HQ.'),
 ('2026-10-23', N'CAPTACION', N'Secuencias de correo en HQ: IA redacta, persona aprueba',         N'Hecha', N'Alta',  N'Envío automático en horario hábil, límite diario, bajas y registro en el CRM.'),
-- Sábado 24
 ('2026-10-24', N'CAPTACION', N'Cambiar a IA gratuita sin tarjeta: Groq + OpenRouter',            N'Hecha', N'Alta',  N'Se quita Gemini; si no hay cupo se usa una plantilla fija.'),
 ('2026-10-24', N'CONTA',     N'Análisis del producto para contadores y competencia',             N'Hecha', N'Alta',  N'Starsoft, Enplazo, ZetaSoftware, Ábaco; viabilidad sobre la base de VORDIA.'),
 ('2026-10-24', N'CONTA',     N'Backend CONTA: XML y QR del SIFEN, validaciones y RG90 DNIT',     N'Hecha', N'Alta',  N'RUC y CDC con dígito verificador; RG90 en TXT/ZIP sin comprobantes electrónicos.'),
 ('2026-10-24', N'CONTA',     N'App del estudio, portal del cliente (/subir) y página /conta',    N'Hecha', N'Alta',  N'Panel con semáforo, bandeja, clientes, libros; QR leído desde la foto en el celular.'),
 ('2026-10-24', N'CONTA',     N'Pruebas de punta a punta y ramas Vordia_Conta en GitHub',         N'Hecha', N'Alta',  N'Esquema verificado: 91 tablas, 0 faltantes.'),
 ('2026-10-24', N'SEGURIDAD', N'Detectar CDC y sello SIFEN simulados en el ticket POS',           N'Hecha', N'Alta',  N'Riesgo tributario: se corrige esta semana antes de vender CONTA.'),

/* ===== ESTA SEMANA · 26/10 al 01/11 ===== */
-- Lunes 26
 ('2026-10-26', N'INFRA',     N'Mergear PR Vordia_Conta (vordia y gesco-web) y desplegar',        N'En curso',  N'Alta',  N'Incluye secuencias de correo e IA gratuita.'),
 ('2026-10-26', N'INFRA',     N'Correr 03 y verificar con 98 en el servidor (91 tablas)',         N'Pendiente', N'Alta',  N'Crea tablas de CONTA, secuencias y el Estudio Contable Demo.'),
 ('2026-10-26', N'CONTA',     N'Crear usuario del Estudio Contable Demo desde HQ → Usuarios',     N'Pendiente', N'Alta',  N'Entorno "Estudio Contable Demo"; probar "Cargar estudio de ejemplo".'),
 ('2026-10-26', N'CAPTACION', N'Cargar claves de Groq y OpenRouter en el servidor (.env)',        N'Pendiente', N'Media', N'Ia__Groq__ApiKey e Ia__OpenRouter__ApiKey en el servicio backend.'),
-- Martes 27
 ('2026-10-27', N'SEGURIDAD', N'Quitar CDC y sello SIFEN simulados del ticket POS',               N'Pendiente', N'Alta',  N'Reemplazar por "Comprobante interno — no válido como factura electrónica".'),
 ('2026-10-27', N'SEGURIDAD', N'Cerrar acceso entre empresas en endpoints que reciben solo un ID', N'Pendiente', N'Alta',  N'Usuarios, CRM, condominios, reservas, Stay, Agro y ANDE.'),
 ('2026-10-27', N'SEGURIDAD', N'Revocar tokens expuestos y rotar contraseñas (SA, panel Nginx)',  N'Pendiente', N'Alta',  N'Sacar la contraseña SA de docker-compose.yml y pasarla a .env.'),
-- Miércoles 28
 ('2026-10-28', N'CONTA',     N'Demo de CONTA con 3 a 5 estudios contables (Caacupé y Asunción)', N'Pendiente', N'Alta',  N'Guion de 10 minutos: panel, bandeja, QR con el celular y RG90.'),
 ('2026-10-28', N'PRECIOS',   N'Enviar encuesta de precios: CONTA, HABITAT y AGRO',               N'Pendiente', N'Alta',  N'Meta: 30 respuestas por producto antes de publicar precios.'),
-- Jueves 29
 ('2026-10-29', N'COBROS',    N'Solicitar cuenta de comercio en Bancard o Pagopar',               N'Pendiente', N'Alta',  N'Trámite que tarda: sin esto no hay cobro con tarjeta ni QR.'),
 ('2026-10-29', N'COBROS',    N'Iniciar cobro real de suscripciones con los planes 2026',         N'Pendiente', N'Alta',  N'Tablas de planes, suscripciones y cobros; corte a solo lectura.'),
-- Viernes 30
 ('2026-10-30', N'CAPTACION', N'Primera secuencia piloto con 5 contactos y revisión de respuestas', N'Pendiente', N'Media', N'Máximo 20 a 30 correos por día por casilla.'),
 ('2026-10-30', N'INFRA',     N'Reservar la IP fija del servidor en Google Cloud',                N'Pendiente', N'Media', N'Evita que vordia.com.py deje de funcionar si la VM se reinicia.'),
 ('2026-10-30', N'SEGURIDAD', N'Completar RUC, razón social y correo legal del sitio',            N'Pendiente', N'Media', N'Variables NEXT_PUBLIC_VORDIA_* antes del build del frontend.'),
-- Sábado 31
 ('2026-10-31', N'PRECIOS',   N'Revisión semanal: leads, encuestas, demos y ajuste del plan',     N'Pendiente', N'Media', N'Comparar contra metas: 3-5 % visitas a prueba, 15-25 % prueba a pago.'),
 ('2026-10-31', N'INFRA',     N'Ampliar el disco del servidor a 50 GB',                           N'Pendiente', N'Baja',  N'El disco de 29 GB llega al 92 % con cada build.');

BEGIN TRAN;

-- Actualiza las que ya existen (mismo título y día)
UPDATE t SET
    t.Estado = x.Estado,
    t.Prioridad = x.Prioridad,
    t.Descripcion = CONCAT(N'[', p.Titulo, N'] ', x.Detalle),
    t.AsignadoNombre = @Responsable,
    t.FechaCompletada = CASE WHEN x.Estado = N'Hecha' THEN COALESCE(t.FechaCompletada, DATEADD(HOUR, 21, CAST(x.Fecha AS DATETIME2))) ELSE NULL END,
    t.Activo = 1
FROM dbo.CrmTareas t
JOIN @Tareas x ON t.Titulo = CONCAT(N'[', x.Proyecto, N'] ', x.Titulo) AND t.FechaVencimiento = x.Fecha
JOIN @Proyectos p ON p.Clave = x.Proyecto
WHERE t.EmpresaId = @Emp;
DECLARE @Actualizadas INT = @@ROWCOUNT;

-- Crea las nuevas (FechaCreacion = el día de la tarea, 9:00 hora de Paraguay)
INSERT INTO dbo.CrmTareas (EmpresaId, Titulo, Descripcion, FechaVencimiento, Estado, Prioridad, AsignadoNombre, Activo, FechaCreacion, FechaCompletada)
SELECT @Emp,
       CONCAT(N'[', x.Proyecto, N'] ', x.Titulo),
       CONCAT(N'[', p.Titulo, N'] ', x.Detalle),
       x.Fecha, x.Estado, x.Prioridad, @Responsable, 1,
       DATEADD(HOUR, 12, CAST(x.Fecha AS DATETIME2)),
       CASE WHEN x.Estado = N'Hecha' THEN DATEADD(HOUR, 21, CAST(x.Fecha AS DATETIME2)) END
FROM @Tareas x
JOIN @Proyectos p ON p.Clave = x.Proyecto
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CrmTareas t
    WHERE t.EmpresaId = @Emp AND t.Titulo = CONCAT(N'[', x.Proyecto, N'] ', x.Titulo) AND t.FechaVencimiento = x.Fecha);
DECLARE @Creadas INT = @@ROWCOUNT;

COMMIT;

PRINT N'';
PRINT N'================================================================';
PRINT N'  VORDIA HQ · Bitácora registrada';
PRINT CONCAT(N'  Tareas creadas: ', @Creadas, N' · actualizadas: ', @Actualizadas);
PRINT N'  Se ven en HQ → Tareas y HQ → Proyectos';
PRINT N'================================================================';
PRINT N'';

/* ───────────────────────── Informe ───────────────────────── */
DECLARE @Rango TABLE (Semana NVARCHAR(40), Desde DATE, Hasta DATE);
INSERT INTO @Rango VALUES (N'Semana pasada (19/10 - 25/10)', '2026-10-19', '2026-10-25'),
                          (N'Esta semana (26/10 - 01/11)',   '2026-10-26', '2026-11-01');

PRINT N'1) RESUMEN POR SEMANA';
SELECT r.Semana,
       COUNT(t.TareaId)                                         AS Total,
       SUM(CASE WHEN t.Estado = N'Hecha'     THEN 1 ELSE 0 END) AS Hechas,
       SUM(CASE WHEN t.Estado = N'En curso'  THEN 1 ELSE 0 END) AS [En curso],
       SUM(CASE WHEN t.Estado = N'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
       SUM(CASE WHEN t.Estado = N'Cancelada' THEN 1 ELSE 0 END) AS Canceladas,
       CONCAT(CAST(ROUND(100.0 * SUM(CASE WHEN t.Estado = N'Hecha' THEN 1 ELSE 0 END) / NULLIF(COUNT(t.TareaId), 0), 0) AS INT), N' %') AS Cumplimiento
FROM @Rango r
LEFT JOIN dbo.CrmTareas t ON t.EmpresaId = @Emp AND t.Activo = 1 AND t.FechaVencimiento BETWEEN r.Desde AND r.Hasta AND t.Titulo LIKE N'[[]%]%'
GROUP BY r.Semana, r.Desde
ORDER BY r.Desde;

PRINT N'2) DETALLE POR DÍA';
SELECT CASE WHEN t.FechaVencimiento <= '2026-10-25' THEN N'Pasada' ELSE N'Actual' END AS Semana,
       CONCAT(UPPER(LEFT(DATENAME(WEEKDAY, t.FechaVencimiento), 1)), SUBSTRING(DATENAME(WEEKDAY, t.FechaVencimiento), 2, 20), N' ', FORMAT(t.FechaVencimiento, 'dd/MM')) AS Dia,
       SUBSTRING(t.Titulo, 2, CHARINDEX(N']', t.Titulo) - 2)                     AS Proyecto,
       LTRIM(SUBSTRING(t.Titulo, CHARINDEX(N']', t.Titulo) + 1, 200))            AS Tarea,
       t.Estado,
       t.Prioridad
FROM dbo.CrmTareas t
WHERE t.EmpresaId = @Emp AND t.Activo = 1 AND t.FechaVencimiento BETWEEN '2026-10-19' AND '2026-11-01' AND t.Titulo LIKE N'[[]%]%'
ORDER BY t.FechaVencimiento,
         CASE t.Estado WHEN N'En curso' THEN 0 WHEN N'Pendiente' THEN 1 WHEN N'Hecha' THEN 2 ELSE 3 END,
         CASE t.Prioridad WHEN N'Alta' THEN 0 WHEN N'Media' THEN 1 ELSE 2 END;

PRINT N'3) PROYECTOS';
SELECT p.Titulo AS Proyecto, p.Estado, p.Prioridad, CONCAT(p.AvancePct, N' %') AS Avance,
       FORMAT(p.FechaInicio, 'dd/MM/yyyy') AS Inicio, FORMAT(p.FechaFin, 'dd/MM/yyyy') AS [Fin estimado],
       (SELECT COUNT(*) FROM dbo.CrmTareas t WHERE t.EmpresaId = @Emp AND t.Activo = 1 AND t.Titulo LIKE CONCAT(N'[[]', x.Clave, N']%') AND t.Estado <> N'Hecha' AND t.Estado <> N'Cancelada') AS [Tareas abiertas]
FROM dbo.CrmProyectos p
JOIN @Proyectos x ON x.Titulo = p.Titulo
WHERE p.EmpresaId = @Emp
ORDER BY CASE p.Prioridad WHEN N'P0' THEN 0 WHEN N'P1' THEN 1 ELSE 2 END, p.AvancePct DESC;
GO
