using Gesco.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gesco.API.Services
{
    /// <summary>
    /// Aislamiento entre empresas: la API recibe el empresaId por query, ruta o cuerpo, y antes no se
    /// comparaba con la empresa del usuario logueado. Solo el equipo de VORDIA (empresa operadora,
    /// rubro VORDIA) puede operar sobre otras empresas.
    /// </summary>
    public interface IEmpresaScope
    {
        Task<bool> EsOperadorVordiaAsync(int empresaId);
    }

    public class EmpresaScope : IEmpresaScope
    {
        private static readonly ConcurrentDictionary<int, (bool operador, DateTime vence)> Cache = new();
        private readonly IApplicationDbContext _db;

        public EmpresaScope(IApplicationDbContext db) => _db = db;

        public async Task<bool> EsOperadorVordiaAsync(int empresaId)
        {
            if (Cache.TryGetValue(empresaId, out var c) && c.vence > DateTime.UtcNow) return c.operador;
            var operador = await _db.Empresas.AsNoTracking().AnyAsync(e => e.EmpresaId == empresaId && e.Rubro == "VORDIA");
            Cache[empresaId] = (operador, DateTime.UtcNow.AddMinutes(10));
            return operador;
        }
    }

    /// <summary>Rechaza con 403 cualquier pedido autenticado que apunte a otra empresa distinta de la del token.</summary>
    public class EmpresaScopeFilter : IAsyncActionFilter
    {
        private readonly IEmpresaScope _scope;

        public EmpresaScopeFilter(IEmpresaScope scope) => _scope = scope;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true || !int.TryParse(user.FindFirst("empresaId")?.Value, out var empresaToken))
            {
                await next();
                return;
            }

            var pedidas = context.ActionArguments
                .SelectMany(a => EmpresasPedidas(a.Key, a.Value))
                .Where(id => id > 0 && id != empresaToken)
                .ToList();

            if (pedidas.Count > 0 && !await _scope.EsOperadorVordiaAsync(empresaToken))
            {
                context.Result = new ObjectResult(new { mensaje = "No tenés acceso a los datos de esa empresa." }) { StatusCode = 403 };
                return;
            }
            await next();
        }

        private static int[] EmpresasPedidas(string nombre, object? valor)
        {
            if (valor == null) return Array.Empty<int>();
            if (valor is int i) return nombre.Equals("empresaId", StringComparison.OrdinalIgnoreCase) ? new[] { i } : Array.Empty<int>();
            var t = valor.GetType();
            if (t.IsPrimitive || valor is string || valor is decimal || valor is DateTime) return Array.Empty<int>();
            var prop = t.GetProperty("EmpresaId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop?.GetValue(valor) switch
            {
                int v => new[] { v },
                _ => Array.Empty<int>()
            };
        }
    }
}
