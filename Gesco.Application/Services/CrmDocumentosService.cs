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
    public class CrmDocumentosService : ICrmDocumentosService
    {
        /// <summary>Un documento entra en "por vencer" cuando le faltan 30 días o menos.</summary>
        private const int DiasAvisoVencimiento = 30;

        private static readonly string[] CategoriasValidas =
            { "Contrato", "Propuesta", "Acta", "Legal", "Factura", "Tecnico", "Otro" };

        private readonly IApplicationDbContext _context;

        public CrmDocumentosService(IApplicationDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<string> Categorias => CategoriasValidas;

        public async Task<List<CrmDocumentoDto>> ListarAsync(
            int empresaId,
            int? clienteId = null,
            string? categoria = null,
            string? q = null,
            bool soloPorVencer = false)
        {
            var query = _context.CrmDocumentos.AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && d.Activo);

            if (clienteId.HasValue && clienteId.Value > 0)
                query = query.Where(d => d.ClienteId == clienteId.Value);

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cat = NormalizarCategoria(categoria);
                query = query.Where(d => d.Categoria == cat);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                query = query.Where(d =>
                    d.Titulo.Contains(texto) ||
                    (d.Notas != null && d.Notas.Contains(texto)) ||
                    (d.NombreArchivo != null && d.NombreArchivo.Contains(texto)) ||
                    (d.Cliente != null && d.Cliente.RazonSocial.Contains(texto)));
            }

            if (soloPorVencer)
            {
                var limite = DateTime.UtcNow.Date.AddDays(DiasAvisoVencimiento);
                query = query.Where(d => d.FechaVencimiento != null && d.FechaVencimiento <= limite);
            }

            var list = await query
                .OrderByDescending(d => d.FechaCreacion)
                .Select(d => new
                {
                    Doc = d,
                    ClienteNombre = d.Cliente != null ? d.Cliente.RazonSocial : null
                })
                .ToListAsync();

            return list.Select(x =>
            {
                var dto = Map(x.Doc);
                dto.ClienteNombre = x.ClienteNombre;
                return dto;
            }).ToList();
        }

        public async Task<CrmDocumentoDto?> ObtenerAsync(int documentoId, int empresaId)
        {
            var row = await _context.CrmDocumentos.AsNoTracking()
                .Where(d => d.DocumentoId == documentoId && d.EmpresaId == empresaId && d.Activo)
                .Select(d => new
                {
                    Doc = d,
                    ClienteNombre = d.Cliente != null ? d.Cliente.RazonSocial : null
                })
                .FirstOrDefaultAsync();

            if (row == null) return null;
            var dto = Map(row.Doc);
            dto.ClienteNombre = row.ClienteNombre;
            return dto;
        }

        public async Task<CrmDocumentosResumenDto> ResumenAsync(int empresaId)
        {
            var hoy = DateTime.UtcNow.Date;
            var limite = hoy.AddDays(DiasAvisoVencimiento);

            var docs = await _context.CrmDocumentos.AsNoTracking()
                .Where(d => d.EmpresaId == empresaId && d.Activo)
                .Select(d => new { d.Categoria, d.FechaVencimiento, d.TamanoBytes })
                .ToListAsync();

            return new CrmDocumentosResumenDto
            {
                Total = docs.Count,
                Vencidos = docs.Count(d => d.FechaVencimiento != null && d.FechaVencimiento < hoy),
                PorVencer = docs.Count(d => d.FechaVencimiento != null && d.FechaVencimiento >= hoy && d.FechaVencimiento <= limite),
                SinVencimiento = docs.Count(d => d.FechaVencimiento == null),
                TamanoTotalMb = Math.Round(docs.Sum(d => d.TamanoBytes) / 1024d / 1024d, 2),
                PorCategoria = docs
                    .GroupBy(d => d.Categoria)
                    .Select(g => new CrmDocumentoCategoriaDto { Categoria = g.Key, Cantidad = g.Count() })
                    .OrderByDescending(c => c.Cantidad)
                    .ToList()
            };
        }

        public async Task<CrmDocumentoDto> CrearConArchivoAsync(CrmDocumentoArchivoRequest request)
        {
            var entity = new CrmDocumento
            {
                EmpresaId = request.EmpresaId,
                ClienteId = await ClienteValidoAsync(request.ClienteId, request.EmpresaId),
                Titulo = string.IsNullOrWhiteSpace(request.Titulo)
                    ? request.NombreArchivo
                    : request.Titulo.Trim(),
                Categoria = NormalizarCategoria(request.Categoria),
                NombreArchivo = request.NombreArchivo,
                Url = request.Url,
                EsEnlaceExterno = false,
                Extension = request.Extension,
                TamanoBytes = request.TamanoBytes,
                FechaVencimiento = request.FechaVencimiento?.Date,
                Notas = Limpiar(request.Notas),
                SubidoPor = Limpiar(request.SubidoPor),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmDocumentos.Add(entity);
            await _context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<CrmDocumentoDto> CrearEnlaceAsync(CrmDocumentoEnlaceRequest request)
        {
            var url = request.Url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El enlace debe empezar con http:// o https://");
            }

            var entity = new CrmDocumento
            {
                EmpresaId = request.EmpresaId,
                ClienteId = await ClienteValidoAsync(request.ClienteId, request.EmpresaId),
                Titulo = request.Titulo.Trim(),
                Categoria = NormalizarCategoria(request.Categoria),
                Url = url,
                EsEnlaceExterno = true,
                FechaVencimiento = request.FechaVencimiento?.Date,
                Notas = Limpiar(request.Notas),
                SubidoPor = Limpiar(request.SubidoPor),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.CrmDocumentos.Add(entity);
            await _context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<CrmDocumentoDto?> ActualizarMetadatosAsync(int documentoId, CrmDocumentoMetadatosRequest request)
        {
            var entity = await _context.CrmDocumentos
                .FirstOrDefaultAsync(d => d.DocumentoId == documentoId && d.EmpresaId == request.EmpresaId && d.Activo);

            if (entity == null) return null;

            entity.Titulo = request.Titulo.Trim();
            entity.Categoria = NormalizarCategoria(request.Categoria);
            entity.ClienteId = await ClienteValidoAsync(request.ClienteId, request.EmpresaId);
            entity.FechaVencimiento = request.FechaVencimiento?.Date;
            entity.Notas = Limpiar(request.Notas);
            entity.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var cliente = entity.ClienteId.HasValue
                ? await _context.CrmClientes.AsNoTracking()
                    .Where(c => c.ClienteId == entity.ClienteId.Value)
                    .Select(c => c.RazonSocial)
                    .FirstOrDefaultAsync()
                : null;

            var dto = Map(entity);
            dto.ClienteNombre = cliente;
            return dto;
        }

        /// <summary>
        /// Baja lógica. El archivo físico se conserva a propósito: permite recuperar
        /// un documento borrado por error sin depender de un respaldo.
        /// </summary>
        public async Task<bool> EliminarAsync(int documentoId, int empresaId)
        {
            var entity = await _context.CrmDocumentos
                .FirstOrDefaultAsync(d => d.DocumentoId == documentoId && d.EmpresaId == empresaId && d.Activo);

            if (entity == null) return false;

            entity.Activo = false;
            entity.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Evita colgar el documento de un cliente de otra empresa.</summary>
        private async Task<int?> ClienteValidoAsync(int? clienteId, int empresaId)
        {
            if (!clienteId.HasValue || clienteId.Value <= 0) return null;

            var existe = await _context.CrmClientes.AsNoTracking()
                .AnyAsync(c => c.ClienteId == clienteId.Value && c.EmpresaId == empresaId);

            return existe ? clienteId : null;
        }

        private static string NormalizarCategoria(string? categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return "Otro";
            var match = CategoriasValidas.FirstOrDefault(c =>
                string.Equals(c, categoria.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? "Otro";
        }

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private static CrmDocumentoDto Map(CrmDocumento d)
        {
            var hoy = DateTime.UtcNow.Date;
            int? dias = d.FechaVencimiento.HasValue
                ? (int)(d.FechaVencimiento.Value.Date - hoy).TotalDays
                : null;

            var estado = dias switch
            {
                null => "sin_vencimiento",
                < 0 => "vencido",
                <= DiasAvisoVencimiento => "por_vencer",
                _ => "vigente"
            };

            return new CrmDocumentoDto
            {
                DocumentoId = d.DocumentoId,
                EmpresaId = d.EmpresaId,
                ClienteId = d.ClienteId,
                ClienteNombre = d.Cliente?.RazonSocial,
                Titulo = d.Titulo,
                Categoria = d.Categoria,
                NombreArchivo = d.NombreArchivo,
                Url = d.Url,
                EsEnlaceExterno = d.EsEnlaceExterno,
                Extension = d.Extension,
                TamanoBytes = d.TamanoBytes,
                TamanoTexto = FormatearTamano(d.TamanoBytes),
                FechaVencimiento = d.FechaVencimiento,
                DiasParaVencer = dias,
                EstadoVencimiento = estado,
                Notas = d.Notas,
                SubidoPor = d.SubidoPor,
                FechaCreacion = d.FechaCreacion
            };
        }

        private static string FormatearTamano(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024d:N0} KB";
            return $"{bytes / 1024d / 1024d:N1} MB";
        }
    }
}
