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
    public class CrmService : ICrmService
    {
        public static readonly string[] EstadosPipeline =
        {
            "Prospecto",
            "Contactado",
            "En negociacion",
            "Propuesta enviada",
            "Cerrado",
            "Cliente activo"
        };

        private readonly IApplicationDbContext _context;

        public CrmService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CrmClienteDto>> ListarClientesAsync(int empresaId, string? estado = null, string? q = null)
        {
            var query = _context.CrmClientes
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && c.Activo);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.EstadoPipeline == estado.Trim());

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    c.RazonSocial.ToLower().Contains(term) ||
                    (c.NombreComercial != null && c.NombreComercial.ToLower().Contains(term)) ||
                    (c.ContactoNombre != null && c.ContactoNombre.ToLower().Contains(term)) ||
                    (c.SistemaServicio != null && c.SistemaServicio.ToLower().Contains(term)) ||
                    (c.Rubro != null && c.Rubro.ToLower().Contains(term)));
            }

            var list = await query
                .OrderByDescending(c => c.FechaActualizacion ?? c.FechaCreacion)
                .ToListAsync();

            var ids = list.Select(c => c.ClienteId).ToList();
            var counts = await _context.CrmHistorial
                .AsNoTracking()
                .Where(h => ids.Contains(h.ClienteId))
                .GroupBy(h => h.ClienteId)
                .Select(g => new { ClienteId = g.Key, Cant = g.Count() })
                .ToDictionaryAsync(x => x.ClienteId, x => x.Cant);

            return list.Select(c => Map(c, counts.GetValueOrDefault(c.ClienteId))).ToList();
        }

        public async Task<CrmClienteDto?> ObtenerClienteAsync(int clienteId, int empresaId)
        {
            var c = await _context.CrmClientes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClienteId == clienteId && x.EmpresaId == empresaId && x.Activo);
            if (c == null) return null;
            var cant = await _context.CrmHistorial.CountAsync(h => h.ClienteId == clienteId);
            return Map(c, cant);
        }

        public async Task<CrmClienteDto> CrearClienteAsync(CrmClienteUpsertRequest request)
        {
            var estado = NormalizarEstado(request.EstadoPipeline) ?? "Prospecto";

            var entity = new CrmCliente
            {
                EmpresaId = request.EmpresaId,
                RazonSocial = request.RazonSocial.Trim(),
                NombreComercial = request.NombreComercial?.Trim(),
                Rubro = request.Rubro?.Trim(),
                Documento = request.Documento?.Trim(),
                Email = request.Email?.Trim(),
                Telefono = request.Telefono?.Trim(),
                ContactoNombre = request.ContactoNombre?.Trim(),
                ContactoCargo = request.ContactoCargo?.Trim(),
                ContactoEmail = request.ContactoEmail?.Trim(),
                ContactoTelefono = request.ContactoTelefono?.Trim(),
                EstadoPipeline = estado,
                SistemaServicio = request.SistemaServicio?.Trim(),
                ProximosPasos = request.ProximosPasos?.Trim(),
                FechaEstimadaCierre = request.FechaEstimadaCierre,
                FeeEstimadoMensual = request.FeeEstimadoMensual,
                Notas = request.Notas,
                Origen = request.Origen?.Trim() ?? "manual",
                OrigenDetalle = request.OrigenDetalle?.Trim(),
                MensajeInicial = request.MensajeInicial,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                UsuarioResponsableId = request.UsuarioResponsableId ?? request.UsuarioId
            };

            _context.CrmClientes.Add(entity);
            await _context.SaveChangesAsync();

            _context.CrmHistorial.Add(new CrmHistorial
            {
                ClienteId = entity.ClienteId,
                EmpresaId = entity.EmpresaId,
                Tipo = "cambio_estado",
                Titulo = "Cliente creado",
                Detalle = "Alta en CRM Vordia.",
                EstadoNuevo = entity.EstadoPipeline,
                Fecha = DateTime.UtcNow,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre
            });
            await _context.SaveChangesAsync();

            return Map(entity, 1);
        }

        public async Task<CrmClienteDto> CrearLeadWebAsync(CrmLeadWebRequest request)
        {
            var empresaId = await _context.Empresas.AsNoTracking()
                .Where(e => e.Rubro == "VORDIA")
                .OrderBy(e => e.EmpresaId)
                .Select(e => e.EmpresaId)
                .FirstOrDefaultAsync();

            if (empresaId == 0)
                throw new InvalidOperationException("Empresa Vordia no encontrada.");

            var nombre = request.Nombre.Trim();
            var email = request.Email.Trim();
            var telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
            var cargo = string.IsNullOrWhiteSpace(request.Cargo) ? null : request.Cargo.Trim();
            var ciudad = string.IsNullOrWhiteSpace(request.Ciudad) ? null : request.Ciudad.Trim();
            var codigo = NormalizarSolucion(request.Sector);
            var solucion = codigo != null ? SolucionesWeb.GetValueOrDefault(codigo) : null;
            var tamano = EtiquetaTamano(request.Tamano);
            var gestion = EtiquetaGestion(request.GestionActual);
            var horario = string.IsNullOrWhiteSpace(request.HorarioContacto) ? null : request.HorarioContacto.Trim();
            var necesidad = string.IsNullOrWhiteSpace(request.Necesidad) ? null : request.Necesidad.Trim();

            var detalle = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"Nombre: {nombre}",
                    cargo != null ? $"Cargo: {cargo}" : null,
                    $"Empresa: {request.Empresa.Trim()}",
                    ciudad != null ? $"Ciudad: {ciudad}" : null,
                    $"Email: {email}",
                    telefono != null ? $"Teléfono: {telefono}" : null,
                    $"Interés: {solucion ?? "Todavía no lo definió"}",
                    tamano != null ? $"Tamaño de operación: {tamano}" : null,
                    gestion != null ? $"Cómo gestiona hoy: {gestion}" : null,
                    horario != null ? $"Mejor horario: {horario}" : null,
                    necesidad != null ? $"Necesidad: {necesidad}" : null
                }.Where(l => l != null));

            var notasFicha = string.Join(
                Environment.NewLine,
                new[]
                {
                    ciudad != null ? $"Ciudad: {ciudad}" : null,
                    tamano != null ? $"Operación: {tamano}" : null,
                    gestion != null ? $"Hoy: {gestion}" : null,
                    horario != null ? $"Contactar: {horario}" : null
                }.Where(l => l != null));

            // Si la persona vuelve a enviar el formulario, se suma al historial en lugar de duplicar el lead
            var existente = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Activo && c.ContactoEmail == email);

            if (existente != null)
            {
                existente.FechaActualizacion = DateTime.UtcNow;
                if (cargo != null) existente.ContactoCargo = cargo;
                if (telefono != null) existente.ContactoTelefono = telefono;
                if (ciudad != null) existente.Rubro = ciudad;
                if (solucion != null) existente.SistemaServicio = solucion;
                if (codigo != null) existente.OrigenDetalle = codigo;
                if (necesidad != null) existente.MensajeInicial = necesidad;
                if (!string.IsNullOrWhiteSpace(notasFicha))
                    existente.Notas = string.IsNullOrWhiteSpace(existente.Notas)
                        ? notasFicha
                        : existente.Notas + Environment.NewLine + notasFicha;
                _context.CrmHistorial.Add(new CrmHistorial
                {
                    ClienteId = existente.ClienteId,
                    EmpresaId = empresaId,
                    Tipo = "nota",
                    Titulo = "Nueva solicitud desde la web",
                    Detalle = detalle,
                    Fecha = DateTime.UtcNow,
                    UsuarioNombre = "Formulario web"
                });
                await _context.SaveChangesAsync();
                return Map(existente, await _context.CrmHistorial.CountAsync(h => h.ClienteId == existente.ClienteId));
            }

            var entity = new CrmCliente
            {
                EmpresaId = empresaId,
                RazonSocial = request.Empresa.Trim(),
                Rubro = ciudad,
                Email = email,
                Telefono = telefono,
                ContactoNombre = nombre,
                ContactoCargo = cargo,
                ContactoEmail = email,
                ContactoTelefono = telefono,
                EstadoPipeline = "Prospecto",
                SistemaServicio = solucion,
                ProximosPasos = horario != null
                    ? $"Contactar — {horario}"
                    : "Contactar — solicitud entrante desde la web",
                Notas = string.IsNullOrWhiteSpace(notasFicha) ? null : notasFicha,
                Origen = "web",
                OrigenDetalle = codigo,
                MensajeInicial = necesidad,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmClientes.Add(entity);
            await _context.SaveChangesAsync();

            _context.CrmHistorial.Add(new CrmHistorial
            {
                ClienteId = entity.ClienteId,
                EmpresaId = empresaId,
                Tipo = "cambio_estado",
                Titulo = "Solicitud de demostración desde la web",
                Detalle = detalle,
                EstadoNuevo = entity.EstadoPipeline,
                Fecha = DateTime.UtcNow,
                UsuarioNombre = "Formulario web"
            });
            await _context.SaveChangesAsync();

            return Map(entity, 1);
        }

        public async Task<CrmClienteDto?> ActualizarClienteAsync(int clienteId, CrmClienteUpsertRequest request)
        {
            var entity = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.EmpresaId == request.EmpresaId && c.Activo);
            if (entity == null) return null;

            var estadoAnterior = entity.EstadoPipeline;
            var estadoNuevo = NormalizarEstado(request.EstadoPipeline) ?? entity.EstadoPipeline;

            entity.RazonSocial = request.RazonSocial.Trim();
            entity.NombreComercial = request.NombreComercial?.Trim();
            entity.Rubro = request.Rubro?.Trim();
            entity.Documento = request.Documento?.Trim();
            entity.Email = request.Email?.Trim();
            entity.Telefono = request.Telefono?.Trim();
            entity.ContactoNombre = request.ContactoNombre?.Trim();
            entity.ContactoCargo = request.ContactoCargo?.Trim();
            entity.ContactoEmail = request.ContactoEmail?.Trim();
            entity.ContactoTelefono = request.ContactoTelefono?.Trim();
            entity.EstadoPipeline = estadoNuevo;
            entity.SistemaServicio = request.SistemaServicio?.Trim();
            entity.ProximosPasos = request.ProximosPasos?.Trim();
            entity.FechaEstimadaCierre = request.FechaEstimadaCierre;
            entity.FeeEstimadoMensual = request.FeeEstimadoMensual;
            entity.Notas = request.Notas;
            if (!string.IsNullOrWhiteSpace(request.Origen))
                entity.Origen = request.Origen.Trim();
            if (!string.IsNullOrWhiteSpace(request.OrigenDetalle))
                entity.OrigenDetalle = request.OrigenDetalle.Trim();
            entity.FechaActualizacion = DateTime.UtcNow;
            if (request.UsuarioResponsableId.HasValue)
                entity.UsuarioResponsableId = request.UsuarioResponsableId;

            if (!string.Equals(estadoAnterior, estadoNuevo, StringComparison.OrdinalIgnoreCase))
            {
                _context.CrmHistorial.Add(new CrmHistorial
                {
                    ClienteId = entity.ClienteId,
                    EmpresaId = entity.EmpresaId,
                    Tipo = "cambio_estado",
                    Titulo = $"Estado: {estadoAnterior} → {estadoNuevo}",
                    Detalle = "Cambio de etapa en edicion de cliente.",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = estadoNuevo,
                    Fecha = DateTime.UtcNow,
                    UsuarioId = request.UsuarioId,
                    UsuarioNombre = request.UsuarioNombre
                });
            }

            await _context.SaveChangesAsync();
            var cant = await _context.CrmHistorial.CountAsync(h => h.ClienteId == entity.ClienteId);
            return Map(entity, cant);
        }

        public async Task<bool> EliminarClienteAsync(int clienteId, int empresaId)
        {
            var entity = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.EmpresaId == empresaId && c.Activo);
            if (entity == null) return false;
            entity.Activo = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CrmClienteDto?> CambiarEstadoAsync(int clienteId, int empresaId, CrmCambiarEstadoRequest request)
        {
            var entity = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.EmpresaId == empresaId && c.Activo);
            if (entity == null) return null;

            var estadoNuevo = NormalizarEstado(request.EstadoPipeline);
            if (estadoNuevo == null)
                throw new ArgumentException("Estado de pipeline inválido.");

            var estadoAnterior = entity.EstadoPipeline;
            if (string.Equals(estadoAnterior, estadoNuevo, StringComparison.OrdinalIgnoreCase))
                return Map(entity, await _context.CrmHistorial.CountAsync(h => h.ClienteId == clienteId));

            entity.EstadoPipeline = estadoNuevo;
            entity.FechaActualizacion = DateTime.UtcNow;

            _context.CrmHistorial.Add(new CrmHistorial
            {
                ClienteId = entity.ClienteId,
                EmpresaId = entity.EmpresaId,
                Tipo = "cambio_estado",
                Titulo = $"Pipeline: {estadoAnterior} → {estadoNuevo}",
                Detalle = request.Nota,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                Fecha = DateTime.UtcNow,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre
            });

            await _context.SaveChangesAsync();
            var cant = await _context.CrmHistorial.CountAsync(h => h.ClienteId == entity.ClienteId);
            return Map(entity, cant);
        }

        public async Task<List<CrmHistorialDto>> ListarHistorialAsync(int clienteId, int empresaId)
        {
            var ok = await _context.CrmClientes.AnyAsync(c => c.ClienteId == clienteId && c.EmpresaId == empresaId);
            if (!ok) return new List<CrmHistorialDto>();

            return await _context.CrmHistorial.AsNoTracking()
                .Where(h => h.ClienteId == clienteId && h.EmpresaId == empresaId)
                .OrderByDescending(h => h.Fecha)
                .Select(h => new CrmHistorialDto
                {
                    HistorialId = h.HistorialId,
                    ClienteId = h.ClienteId,
                    Tipo = h.Tipo,
                    Titulo = h.Titulo,
                    Detalle = h.Detalle,
                    EstadoAnterior = h.EstadoAnterior,
                    EstadoNuevo = h.EstadoNuevo,
                    Fecha = h.Fecha,
                    UsuarioId = h.UsuarioId,
                    UsuarioNombre = h.UsuarioNombre
                })
                .ToListAsync();
        }

        public async Task<CrmHistorialDto> AgregarHistorialAsync(int clienteId, CrmHistorialCreateRequest request)
        {
            var cliente = await _context.CrmClientes
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.EmpresaId == request.EmpresaId && c.Activo);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            var h = new CrmHistorial
            {
                ClienteId = clienteId,
                EmpresaId = request.EmpresaId,
                Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "nota" : request.Tipo.Trim().ToLower(),
                Titulo = request.Titulo.Trim(),
                Detalle = request.Detalle,
                Fecha = DateTime.UtcNow,
                UsuarioId = request.UsuarioId,
                UsuarioNombre = request.UsuarioNombre
            };

            _context.CrmHistorial.Add(h);
            cliente.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new CrmHistorialDto
            {
                HistorialId = h.HistorialId,
                ClienteId = h.ClienteId,
                Tipo = h.Tipo,
                Titulo = h.Titulo,
                Detalle = h.Detalle,
                Fecha = h.Fecha,
                UsuarioId = h.UsuarioId,
                UsuarioNombre = h.UsuarioNombre
            };
        }

        public async Task<List<CrmPipelineResumenDto>> ObtenerPipelineAsync(int empresaId)
        {
            var clientes = await ListarClientesAsync(empresaId);
            return EstadosPipeline.Select(estado => new CrmPipelineResumenDto
            {
                Estado = estado,
                Cantidad = clientes.Count(c => c.EstadoPipeline == estado),
                Clientes = clientes.Where(c => c.EstadoPipeline == estado).ToList()
            }).ToList();
        }

        /// <summary>Códigos que ofrece el formulario de vordia.com.py → servicio en el CRM.</summary>
        private static readonly Dictionary<string, string> SolucionesWeb = new(StringComparer.OrdinalIgnoreCase)
        {
            ["HQ"] = "Vordia HQ — dirección y control",
            ["HABITAT"] = "Vordia Habitat — condominios y edificios",
            ["STAY"] = "Vordia Stay — hospedaje y reservas",
            ["DINE"] = "Vordia Dine — gastronomía",
            ["RETAIL"] = "Vordia Retail — comercio y stock",
            ["BOUTIQUE"] = "Vordia Boutique — moda",
            ["OPS"] = "Vordia Ops — operaciones y logística",
            ["AGRO"] = "Vordia Agro — campo y campañas",
            ["HABLAR"] = "Todavía no lo definió"
        };

        private static string? NormalizarSolucion(string? sector)
        {
            if (string.IsNullOrWhiteSpace(sector)) return null;
            var raw = sector.Trim().ToUpperInvariant();
            return SolucionesWeb.ContainsKey(raw) ? raw : null;
        }

        private static string? EtiquetaTamano(string? tamano) => (tamano ?? "").Trim().ToLowerInvariant() switch
        {
            "1-10" => "1 a 10 personas / unidades",
            "11-50" => "11 a 50",
            "51-200" => "51 a 200",
            "200+" => "Más de 200",
            _ => string.IsNullOrWhiteSpace(tamano) ? null : tamano.Trim()
        };

        private static string? EtiquetaGestion(string? gestion) => (gestion ?? "").Trim().ToLowerInvariant() switch
        {
            "planillas" => "Planillas y WhatsApp",
            "otro_sistema" => "Ya usa otro sistema",
            "mixto" => "Mixto (planillas + sistema)",
            "nada" => "Todavía no tiene un método claro",
            _ => string.IsNullOrWhiteSpace(gestion) ? null : gestion.Trim()
        };

        private static string? NormalizarEstado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return null;
            var raw = estado.Trim();
            var match = EstadosPipeline.FirstOrDefault(e =>
                string.Equals(e, raw, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Alias sin tilde / variantes
            var alias = raw.ToLowerInvariant()
                .Replace("ó", "o").Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ú", "u");
            return EstadosPipeline.FirstOrDefault(e =>
                e.ToLowerInvariant().Replace("ó", "o") == alias);
        }

        private static CrmClienteDto Map(CrmCliente c, int cantidadHistorial) => new()
        {
            ClienteId = c.ClienteId,
            EmpresaId = c.EmpresaId,
            RazonSocial = c.RazonSocial,
            NombreComercial = c.NombreComercial,
            Rubro = c.Rubro,
            Documento = c.Documento,
            Email = c.Email,
            Telefono = c.Telefono,
            ContactoNombre = c.ContactoNombre,
            ContactoCargo = c.ContactoCargo,
            ContactoEmail = c.ContactoEmail,
            ContactoTelefono = c.ContactoTelefono,
            EstadoPipeline = c.EstadoPipeline,
            SistemaServicio = c.SistemaServicio,
            ProximosPasos = c.ProximosPasos,
            FechaEstimadaCierre = c.FechaEstimadaCierre,
            FeeEstimadoMensual = c.FeeEstimadoMensual,
            Notas = c.Notas,
            Origen = c.Origen,
            OrigenDetalle = c.OrigenDetalle,
            MensajeInicial = c.MensajeInicial,
            Activo = c.Activo,
            FechaCreacion = c.FechaCreacion,
            FechaActualizacion = c.FechaActualizacion,
            UsuarioResponsableId = c.UsuarioResponsableId,
            CantidadHistorial = cantidadHistorial
        };
    }
}
