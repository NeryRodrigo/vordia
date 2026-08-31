using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Gesco.Application.Helpers
{
    public static class PermisosJsonHelper
    {
        public static List<string> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static string Serialize(IEnumerable<string>? permisos)
        {
            var list = permisos?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                       ?? new List<string>();
            return JsonSerializer.Serialize(list);
        }
    }
}
