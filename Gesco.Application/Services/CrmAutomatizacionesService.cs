using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public class CrmAutomatizacionesService : ICrmAutomatizacionesService
    {
        /// <summary>
        /// Cuánto hacia atrás mira el motor. Evita que al encender una regla nueva
        /// salgan avisos de hechos viejos que ya nadie va a atender.
        /// </summary>
        private const int VentanaDiasAtras = 60;

        private static readonly string[] AccionesValidas = { "email", "tarea", "email_y_tarea" };

        private static readonly Dictionary<string, (string Nombre, string Descripcion, string EtiquetaDias)> DisparadoresValidos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["documento_por_vencer"] = (
                    "Documento por vencer",
                    "Revisa el repositorio y avisa por los documentos que están cerca de su fecha de vencimiento.",
                    "Días de anticipación del aviso"),
                ["contrato_por_cobrar"] = (
                    "Contrato por cobrar",
                    "Avisa antes del próximo vencimiento de cada contrato activo que todavía no está pagado.",
                    "Días antes del cobro"),
                ["contrato_vencido"] = (
                    "Contrato vencido sin pagar",
                    "Avisa por los contratos cuyo vencimiento ya pasó y siguen sin registrar el pago.",
                    "No aplica: mira hacia atrás"),
                ["tarea_por_vencer"] = (
                    "Tarea por vencer",
                    "Recuerda las tareas pendientes o en curso que están por llegar a su fecha límite.",
                    "Días antes del vencimiento"),
                ["propuesta_sin_respuesta"] = (
                    "Propuesta sin respuesta",
                    "Avisa cuando una propuesta enviada lleva demasiados días sin ganarse ni perderse.",
                    "Días de espera sin respuesta"),
                ["lead_web_sin_contactar"] = (
                    "Lead web sin contactar",
                    "Avisa cuando una solicitud entrante de la web sigue en Prospecto sin que nadie la trabaje.",
                    "Días de espera sin contacto")
            };

        private readonly IApplicationDbContext _context;
        private readonly IEmailService _email;

        public CrmAutomatizacionesService(IApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        // ─────────────────────────────────────────────────────────
        // Administración de reglas
        // ─────────────────────────────────────────────────────────

        public async Task<List<CrmAutomatizacionDto>> ListarAsync(int empresaId, bool? soloActivas = null)
        {
            var query = _context.CrmAutomatizaciones.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.Activo);

            if (soloActivas.HasValue)
                query = query.Where(a => a.Activa == soloActivas.Value);

            var list = await query
                .OrderByDescending(a => a.Activa)
                .ThenBy(a => a.Nombre)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async Task<CrmAutomatizacionDto?> ObtenerAsync(int automatizacionId, int empresaId)
        {
            var entity = await _context.CrmAutomatizaciones.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AutomatizacionId == automatizacionId && a.EmpresaId == empresaId && a.Activo);

            return entity == null ? null : Map(entity);
        }

        public async Task<CrmAutomatizacionDto> CrearAsync(CrmAutomatizacionUpsertRequest request)
        {
            var entity = new CrmAutomatizacion
            {
                EmpresaId = request.EmpresaId,
                Nombre = request.Nombre.Trim(),
                Descripcion = Limpiar(request.Descripcion),
                Disparador = NormalizarDisparador(request.Disparador),
                DiasAnticipacion = Math.Clamp(request.DiasAnticipacion, 0, 365),
                Accion = NormalizarAccion(request.Accion),
                EmailDestino = Limpiar(request.EmailDestino),
                AsuntoPlantilla = Limpiar(request.AsuntoPlantilla),
                MensajePlantilla = Limpiar(request.MensajePlantilla),
                Activa = request.Activa,
                Activo = true,
                CreadoPor = Limpiar(request.CreadoPor),
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmAutomatizaciones.Add(entity);
            await _context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<CrmAutomatizacionDto?> ActualizarAsync(int automatizacionId, CrmAutomatizacionUpsertRequest request)
        {
            var entity = await _context.CrmAutomatizaciones
                .FirstOrDefaultAsync(a => a.AutomatizacionId == automatizacionId && a.EmpresaId == request.EmpresaId && a.Activo);

            if (entity == null) return null;

            entity.Nombre = request.Nombre.Trim();
            entity.Descripcion = Limpiar(request.Descripcion);
            entity.Disparador = NormalizarDisparador(request.Disparador);
            entity.DiasAnticipacion = Math.Clamp(request.DiasAnticipacion, 0, 365);
            entity.Accion = NormalizarAccion(request.Accion);
            entity.EmailDestino = Limpiar(request.EmailDestino);
            entity.AsuntoPlantilla = Limpiar(request.AsuntoPlantilla);
            entity.MensajePlantilla = Limpiar(request.MensajePlantilla);
            entity.Activa = request.Activa;
            entity.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<CrmAutomatizacionDto?> CambiarEstadoAsync(int automatizacionId, CrmAutomatizacionEstadoRequest request)
        {
            var entity = await _context.CrmAutomatizaciones
                .FirstOrDefaultAsync(a => a.AutomatizacionId == automatizacionId && a.EmpresaId == request.EmpresaId && a.Activo);

            if (entity == null) return null;

            entity.Activa = request.Activa;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<bool> EliminarAsync(int automatizacionId, int empresaId)
        {
            var entity = await _context.CrmAutomatizaciones
                .FirstOrDefaultAsync(a => a.AutomatizacionId == automatizacionId && a.EmpresaId == empresaId && a.Activo);

            if (entity == null) return false;

            entity.Activo = false;
            entity.Activa = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CrmAutomatizacionEjecucionDto>> ListarEjecucionesAsync(
            int empresaId,
            int? automatizacionId = null,
            int top = 50)
        {
            var query = _context.CrmAutomatizacionEjecuciones.AsNoTracking()
                .Include(e => e.Automatizacion)
                .Where(e => e.EmpresaId == empresaId);

            if (automatizacionId.HasValue && automatizacionId.Value > 0)
                query = query.Where(e => e.AutomatizacionId == automatizacionId.Value);

            var list = await query
                .OrderByDescending(e => e.Fecha)
                .Take(Math.Clamp(top, 1, 300))
                .ToListAsync();

            return list.Select(e => new CrmAutomatizacionEjecucionDto
            {
                EjecucionId = e.EjecucionId,
                AutomatizacionId = e.AutomatizacionId,
                AutomatizacionNombre = e.Automatizacion?.Nombre ?? "(regla eliminada)",
                Resultado = e.Resultado,
                Referencia = e.Referencia,
                Detalle = e.Detalle,
                Fecha = e.Fecha
            }).ToList();
        }

        public CrmAutomatizacionCatalogoDto Catalogo() => new()
        {
            Disparadores = DisparadoresValidos.Select(d => new CrmAutomatizacionOpcionDto
            {
                Codigo = d.Key,
                Nombre = d.Value.Nombre,
                Descripcion = d.Value.Descripcion,
                EtiquetaDias = d.Value.EtiquetaDias
            }).ToList(),
            Acciones = new List<CrmAutomatizacionOpcionDto>
            {
                new() { Codigo = "email", Nombre = "Enviar correo", Descripcion = "Manda un aviso al destinatario configurado." },
                new() { Codigo = "tarea", Nombre = "Crear tarea", Descripcion = "Deja una tarea pendiente en el tablero de HQ." },
                new() { Codigo = "email_y_tarea", Nombre = "Correo y tarea", Descripcion = "Avisa por correo y además deja la tarea." }
            },
            CategoriasDocumento = new List<string> { "Contrato", "Propuesta", "Acta", "Legal", "Factura", "Tecnico", "Otro" }
        };

        // ─────────────────────────────────────────────────────────
        // Motor
        // ─────────────────────────────────────────────────────────

        /// <summary>Un hecho concreto que hace saltar una regla.</summary>
        private sealed record Hallazgo(
            string Clave,
            string Referencia,
            DateTime? Fecha,
            int Dias,
            decimal? Monto,
            int? ClienteId);

        public async Task<CrmAutomatizacionCorridaDto> EjecutarAsync(
            int empresaId,
            int? automatizacionId = null,
            bool simular = false)
        {
            var query = _context.CrmAutomatizaciones
                .Where(a => a.EmpresaId == empresaId && a.Activo);

            // Una regla puntual se puede probar aunque esté pausada; la corrida general solo toma las encendidas
            if (automatizacionId.HasValue && automatizacionId.Value > 0)
                query = query.Where(a => a.AutomatizacionId == automatizacionId.Value);
            else
                query = query.Where(a => a.Activa);

            var reglas = await query.ToListAsync();
            var corrida = new CrmAutomatizacionCorridaDto { ReglasEvaluadas = reglas.Count };

            foreach (var regla in reglas)
            {
                List<Hallazgo> hallazgos;
                try
                {
                    hallazgos = await BuscarHallazgosAsync(regla);
                }
                catch (Exception ex)
                {
                    corrida.Errores++;
                    corrida.Detalle.Add($"{regla.Nombre}: no se pudo evaluar ({ex.Message})");
                    continue;
                }

                corrida.Coincidencias += hallazgos.Count;

                var claves = hallazgos.Select(h => h.Clave).ToList();
                var yaAvisadas = claves.Count == 0
                    ? new List<string>()
                    : await _context.CrmAutomatizacionEjecuciones.AsNoTracking()
                        .Where(e => e.AutomatizacionId == regla.AutomatizacionId
                                 && e.Resultado != "error"
                                 && claves.Contains(e.ClaveUnica))
                        .Select(e => e.ClaveUnica)
                        .ToListAsync();

                var pendientes = hallazgos.Where(h => !yaAvisadas.Contains(h.Clave)).ToList();
                corrida.YaAvisados += hallazgos.Count - pendientes.Count;

                if (simular)
                {
                    foreach (var h in pendientes)
                        corrida.Detalle.Add($"{regla.Nombre}: avisaría por {h.Referencia}");

                    regla.UltimaEjecucion = DateTime.UtcNow;
                    regla.UltimoResultado = $"Simulación: {pendientes.Count} aviso(s) pendientes de {hallazgos.Count} coincidencia(s)";
                    continue;
                }

                var disparos = 0;
                var errores = 0;

                foreach (var h in pendientes)
                {
                    var asunto = Render(regla.AsuntoPlantilla, AsuntoPorDefecto(regla.Disparador), h);
                    var mensaje = Render(regla.MensajePlantilla, MensajePorDefecto(regla.Disparador), h);
                    var mandaEmail = regla.Accion is "email" or "email_y_tarea";
                    var creaTarea = regla.Accion is "tarea" or "email_y_tarea";
                    string? error = null;

                    if (mandaEmail)
                    {
                        var (enviado, errorEmail) = await _email.EnviarNotificacionHqAsync(regla.EmailDestino, asunto, mensaje);
                        if (!enviado) error = errorEmail;
                    }

                    if (creaTarea && error == null)
                    {
                        _context.CrmTareas.Add(new CrmTarea
                        {
                            EmpresaId = regla.EmpresaId,
                            Titulo = Recortar(asunto, 200),
                            Descripcion = mensaje,
                            FechaVencimiento = h.Fecha,
                            Estado = "Pendiente",
                            Prioridad = "Alta",
                            ClienteId = h.ClienteId,
                            AsignadoNombre = "Automatización",
                            Activo = true,
                            FechaCreacion = DateTime.UtcNow
                        });
                    }

                    _context.CrmAutomatizacionEjecuciones.Add(new CrmAutomatizacionEjecucion
                    {
                        AutomatizacionId = regla.AutomatizacionId,
                        EmpresaId = regla.EmpresaId,
                        ClaveUnica = Recortar(h.Clave, 200),
                        Resultado = error == null ? regla.Accion : "error",
                        Referencia = Recortar(h.Referencia, 200),
                        Detalle = Recortar(error ?? mensaje, 1000),
                        Fecha = DateTime.UtcNow
                    });

                    if (error == null)
                    {
                        disparos++;
                        corrida.Detalle.Add($"{regla.Nombre}: {h.Referencia}");
                    }
                    else
                    {
                        errores++;
                        corrida.Detalle.Add($"{regla.Nombre}: falló por {h.Referencia} — {error}");
                    }
                }

                regla.UltimaEjecucion = DateTime.UtcNow;
                regla.VecesDisparada += disparos;
                regla.UltimoResultado = errores > 0
                    ? $"{disparos} aviso(s) y {errores} error(es) sobre {hallazgos.Count} coincidencia(s)"
                    : hallazgos.Count == 0
                        ? "Sin coincidencias"
                        : $"{disparos} aviso(s) nuevo(s) de {hallazgos.Count} coincidencia(s)";

                corrida.Disparos += disparos;
                corrida.Errores += errores;

                // Se guarda por regla para que un fallo puntual no tire abajo el resto de la corrida
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    corrida.Errores++;
                    corrida.Detalle.Add($"{regla.Nombre}: no se pudo registrar la ejecución ({ex.Message})");
                }
            }

            if (simular)
            {
                try { await _context.SaveChangesAsync(); } catch { /* la simulación no debe romper nada */ }
            }

            corrida.Resumen = corrida.ReglasEvaluadas == 0
                ? "No hay reglas encendidas para evaluar."
                : simular
                    ? $"{corrida.ReglasEvaluadas} regla(s) evaluadas, {corrida.Coincidencias} coincidencia(s), {corrida.Coincidencias - corrida.YaAvisados} aviso(s) se enviarían."
                    : $"{corrida.ReglasEvaluadas} regla(s) evaluadas, {corrida.Disparos} aviso(s) nuevo(s), {corrida.YaAvisados} ya avisado(s), {corrida.Errores} error(es).";

            return corrida;
        }

        public async Task<CrmAutomatizacionCorridaDto> EjecutarTodasAsync()
        {
            var empresas = await _context.CrmAutomatizaciones.AsNoTracking()
                .Where(a => a.Activo && a.Activa)
                .Select(a => a.EmpresaId)
                .Distinct()
                .ToListAsync();

            var total = new CrmAutomatizacionCorridaDto();

            foreach (var empresaId in empresas)
            {
                var parcial = await EjecutarAsync(empresaId);
                total.ReglasEvaluadas += parcial.ReglasEvaluadas;
                total.Coincidencias += parcial.Coincidencias;
                total.Disparos += parcial.Disparos;
                total.YaAvisados += parcial.YaAvisados;
                total.Errores += parcial.Errores;
                total.Detalle.AddRange(parcial.Detalle);
            }

            total.Resumen = $"{empresas.Count} empresa(s), {total.ReglasEvaluadas} regla(s), " +
                            $"{total.Disparos} aviso(s) nuevo(s), {total.Errores} error(es).";
            return total;
        }

        private async Task<List<Hallazgo>> BuscarHallazgosAsync(CrmAutomatizacion regla)
        {
            var hoy = DateTime.UtcNow.Date;
            var desde = hoy.AddDays(-VentanaDiasAtras);
            var hasta = hoy.AddDays(regla.DiasAnticipacion);
            var empresaId = regla.EmpresaId;

            switch (regla.Disparador)
            {
                case "documento_por_vencer":
                {
                    var docs = await _context.CrmDocumentos.AsNoTracking()
                        .Include(d => d.Cliente)
                        .Where(d => d.EmpresaId == empresaId && d.Activo
                                 && d.FechaVencimiento != null
                                 && d.FechaVencimiento >= desde && d.FechaVencimiento <= hasta)
                        .ToListAsync();

                    return docs.Select(d => new Hallazgo(
                        $"documento:{d.DocumentoId}:{d.FechaVencimiento!.Value:yyyy-MM-dd}",
                        d.Cliente != null ? $"{d.Titulo} ({d.Cliente.RazonSocial})" : d.Titulo,
                        d.FechaVencimiento,
                        (int)(d.FechaVencimiento!.Value.Date - hoy).TotalDays,
                        null,
                        d.ClienteId)).ToList();
                }

                case "contrato_por_cobrar":
                {
                    var contratos = await _context.CrmContratos.AsNoTracking()
                        .Include(c => c.Cliente)
                        .Where(c => c.EmpresaId == empresaId && c.Activo
                                 && c.EstadoContrato == "Activo"
                                 && c.EstadoPago != "Pagado"
                                 && c.ProximoVencimiento != null
                                 && c.ProximoVencimiento >= hoy && c.ProximoVencimiento <= hasta)
                        .ToListAsync();

                    return contratos.Select(c => new Hallazgo(
                        $"contrato-cobro:{c.ContratoId}:{c.ProximoVencimiento!.Value:yyyy-MM-dd}",
                        $"{c.Cliente?.RazonSocial ?? "Cliente"} — {c.SistemaServicio}",
                        c.ProximoVencimiento,
                        (int)(c.ProximoVencimiento!.Value.Date - hoy).TotalDays,
                        c.FeeMensual > 0 ? c.FeeMensual : c.FeeAcordado,
                        c.ClienteId)).ToList();
                }

                case "contrato_vencido":
                {
                    var contratos = await _context.CrmContratos.AsNoTracking()
                        .Include(c => c.Cliente)
                        .Where(c => c.EmpresaId == empresaId && c.Activo
                                 && c.EstadoContrato == "Activo"
                                 && c.EstadoPago != "Pagado"
                                 && c.ProximoVencimiento != null
                                 && c.ProximoVencimiento < hoy && c.ProximoVencimiento >= desde)
                        .ToListAsync();

                    return contratos.Select(c => new Hallazgo(
                        $"contrato-vencido:{c.ContratoId}:{c.ProximoVencimiento!.Value:yyyy-MM-dd}",
                        $"{c.Cliente?.RazonSocial ?? "Cliente"} — {c.SistemaServicio}",
                        c.ProximoVencimiento,
                        (int)(c.ProximoVencimiento!.Value.Date - hoy).TotalDays,
                        c.FeeMensual > 0 ? c.FeeMensual : c.FeeAcordado,
                        c.ClienteId)).ToList();
                }

                case "tarea_por_vencer":
                {
                    var tareas = await _context.CrmTareas.AsNoTracking()
                        .Include(t => t.Cliente)
                        .Where(t => t.EmpresaId == empresaId && t.Activo
                                 && (t.Estado == "Pendiente" || t.Estado == "En curso")
                                 && t.FechaVencimiento != null
                                 && t.FechaVencimiento >= desde && t.FechaVencimiento <= hasta)
                        .ToListAsync();

                    return tareas.Select(t => new Hallazgo(
                        $"tarea:{t.TareaId}:{t.FechaVencimiento!.Value:yyyy-MM-dd}",
                        t.Cliente != null ? $"{t.Titulo} ({t.Cliente.RazonSocial})" : t.Titulo,
                        t.FechaVencimiento,
                        (int)(t.FechaVencimiento!.Value.Date - hoy).TotalDays,
                        null,
                        t.ClienteId)).ToList();
                }

                case "propuesta_sin_respuesta":
                {
                    var limite = hoy.AddDays(-regla.DiasAnticipacion);

                    var propuestas = await _context.CrmPropuestas.AsNoTracking()
                        .Where(p => p.EmpresaId == empresaId && p.Activo
                                 && p.Estado == "Enviada"
                                 && p.FechaEnvio <= limite)
                        .ToListAsync();

                    var clientes = await _context.CrmClientes.AsNoTracking()
                        .Where(c => c.EmpresaId == empresaId)
                        .Select(c => new { c.ClienteId, c.RazonSocial })
                        .ToListAsync();

                    return propuestas.Select(p => new Hallazgo(
                        $"propuesta:{p.PropuestaId}",
                        $"{clientes.FirstOrDefault(c => c.ClienteId == p.ClienteId)?.RazonSocial ?? "Cliente"} — {p.Descripcion}",
                        p.FechaEnvio.Date,
                        (int)(hoy - p.FechaEnvio.Date).TotalDays,
                        p.MontoGs,
                        p.ClienteId)).ToList();
                }

                case "lead_web_sin_contactar":
                {
                    var limite = DateTime.UtcNow.AddDays(-regla.DiasAnticipacion);

                    var leads = await _context.CrmClientes.AsNoTracking()
                        .Where(c => c.EmpresaId == empresaId && c.Activo
                                 && c.Origen == "web"
                                 && c.EstadoPipeline == "Prospecto"
                                 && c.FechaCreacion <= limite)
                        .ToListAsync();

                    return leads.Select(c => new Hallazgo(
                        $"lead:{c.ClienteId}",
                        $"{c.RazonSocial}{(string.IsNullOrWhiteSpace(c.ContactoNombre) ? "" : $" — {c.ContactoNombre}")}",
                        c.FechaCreacion.Date,
                        (int)(hoy - c.FechaCreacion.Date).TotalDays,
                        c.FeeEstimadoMensual,
                        c.ClienteId)).ToList();
                }

                default:
                    return new List<Hallazgo>();
            }
        }

        private static string Render(string? plantilla, string porDefecto, Hallazgo h)
        {
            var texto = string.IsNullOrWhiteSpace(plantilla) ? porDefecto : plantilla!;
            return texto
                .Replace("{referencia}", h.Referencia)
                .Replace("{fecha}", h.Fecha?.ToString("dd/MM/yyyy") ?? "sin fecha")
                .Replace("{dias}", Math.Abs(h.Dias).ToString())
                .Replace("{monto}", h.Monto.HasValue && h.Monto.Value > 0 ? $"{h.Monto.Value:N0} Gs." : "—");
        }

        private static string AsuntoPorDefecto(string disparador) => disparador switch
        {
            "documento_por_vencer" => "Documento por vencer: {referencia}",
            "contrato_por_cobrar" => "Cobro próximo: {referencia}",
            "contrato_vencido" => "Contrato vencido sin pagar: {referencia}",
            "tarea_por_vencer" => "Tarea por vencer: {referencia}",
            "propuesta_sin_respuesta" => "Propuesta sin respuesta: {referencia}",
            "lead_web_sin_contactar" => "Lead sin contactar: {referencia}",
            _ => "Aviso de Vordia HQ: {referencia}"
        };

        private static string MensajePorDefecto(string disparador) => disparador switch
        {
            "documento_por_vencer" => "El documento {referencia} vence el {fecha} ({dias} días).",
            "contrato_por_cobrar" => "El contrato {referencia} se cobra el {fecha} ({dias} días). Monto: {monto}.",
            "contrato_vencido" => "El contrato {referencia} venció el {fecha}, hace {dias} días, y sigue sin pago registrado. Monto: {monto}.",
            "tarea_por_vencer" => "La tarea {referencia} vence el {fecha} ({dias} días).",
            "propuesta_sin_respuesta" => "La propuesta {referencia} se envió el {fecha} y lleva {dias} días sin respuesta. Monto: {monto}.",
            "lead_web_sin_contactar" => "{referencia} dejó sus datos en la web hace {dias} días y sigue sin contacto.",
            _ => "{referencia} — {fecha}"
        };

        private static string NormalizarDisparador(string? disparador)
        {
            if (!string.IsNullOrWhiteSpace(disparador) && DisparadoresValidos.ContainsKey(disparador.Trim()))
                return disparador.Trim().ToLowerInvariant();

            throw new InvalidOperationException("El disparador indicado no existe.");
        }

        private static string NormalizarAccion(string? accion)
        {
            if (string.IsNullOrWhiteSpace(accion)) return "email";
            var match = AccionesValidas.FirstOrDefault(a =>
                string.Equals(a, accion.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? "email";
        }

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private static string Recortar(string texto, int largo) =>
            texto.Length <= largo ? texto : texto.Substring(0, largo);

        private static CrmAutomatizacionDto Map(CrmAutomatizacion a)
        {
            DisparadoresValidos.TryGetValue(a.Disparador, out var meta);

            return new CrmAutomatizacionDto
            {
                AutomatizacionId = a.AutomatizacionId,
                EmpresaId = a.EmpresaId,
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                Disparador = a.Disparador,
                DisparadorNombre = meta.Nombre ?? a.Disparador,
                DiasAnticipacion = a.DiasAnticipacion,
                Accion = a.Accion,
                AccionNombre = a.Accion switch
                {
                    "tarea" => "Crear tarea",
                    "email_y_tarea" => "Correo y tarea",
                    _ => "Enviar correo"
                },
                EmailDestino = a.EmailDestino,
                AsuntoPlantilla = a.AsuntoPlantilla,
                MensajePlantilla = a.MensajePlantilla,
                Activa = a.Activa,
                UltimaEjecucion = a.UltimaEjecucion,
                UltimoResultado = a.UltimoResultado,
                VecesDisparada = a.VecesDisparada,
                CreadoPor = a.CreadoPor,
                FechaCreacion = a.FechaCreacion
            };
        }
    }
}
