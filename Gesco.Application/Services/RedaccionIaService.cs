using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gesco.Application.Services
{
    public record DatosRedaccion(
        string Producto, string Oferta, string Firma, int Paso, int TotalPasos,
        string? Nombre, string? Empresa, string? Contexto, string? AsuntoAnterior);

    public record CorreoRedactado(string Asunto, string Cuerpo, bool ConIa);

    public interface IRedaccionIaService
    {
        bool Configurada { get; }
        /// <summary>Proveedores con clave cargada, en orden de uso (ej. "Groq, OpenRouter").</summary>
        string Proveedores { get; }
        Task<CorreoRedactado> RedactarAsync(DatosRedaccion d, CancellationToken ct = default);
    }

    /// <summary>
    /// Redacta correos comerciales con proveedores de IA en línea con plan gratuito que NO piden tarjeta
    /// (sin tarjeta cargada no pueden cobrar): primero Groq ("Ia:Groq:ApiKey"), si falla o se agotó el cupo
    /// del día, OpenRouter con modelos ":free" ("Ia:OpenRouter:ApiKey"), y si ninguno responde, una plantilla fija.
    /// Ambos usan la API de chat compatible con OpenAI. Solo se envían datos de negocio, nunca datos sensibles.
    /// </summary>
    public class RedaccionIaService : IRedaccionIaService
    {
        private record Proveedor(string Nombre, string Url, string ApiKey, string Modelo, bool JsonNativo);

        private readonly HttpClient _http;
        private readonly Proveedor[] _proveedores;

        public RedaccionIaService(HttpClient http, IConfiguration config)
        {
            _http = http;
            static string Val(IConfiguration c, string k, string def) => string.IsNullOrWhiteSpace(c[k]) ? def : c[k]!.Trim();
            _proveedores = new[]
            {
                new Proveedor("Groq", "https://api.groq.com/openai/v1/chat/completions",
                    config["Ia:Groq:ApiKey"] ?? "", Val(config, "Ia:Groq:Modelo", "llama-3.3-70b-versatile"), true),
                new Proveedor("OpenRouter", "https://openrouter.ai/api/v1/chat/completions",
                    config["Ia:OpenRouter:ApiKey"] ?? "", Val(config, "Ia:OpenRouter:Modelo", "meta-llama/llama-3.3-70b-instruct:free"), false),
            }.Where(p => !string.IsNullOrWhiteSpace(p.ApiKey)).ToArray();
        }

        public bool Configurada => _proveedores.Length > 0;
        public string Proveedores => string.Join(", ", _proveedores.Select(p => p.Nombre));

        public async Task<CorreoRedactado> RedactarAsync(DatosRedaccion d, CancellationToken ct = default)
        {
            foreach (var p in _proveedores)
            {
                var r = await IntentarAsync(p, Prompt(d), ct);
                if (r != null) return r;
            }
            return Plantilla(d);
        }

        private async Task<CorreoRedactado?> IntentarAsync(Proveedor p, string prompt, CancellationToken ct)
        {
            try
            {
                object cuerpo = p.JsonNativo
                    ? new { model = p.Modelo, temperature = 0.7, response_format = new { type = "json_object" }, messages = new[] { new { role = "user", content = prompt } } }
                    : new { model = p.Modelo, temperature = 0.7, messages = new[] { new { role = "user", content = prompt } } };
                using var req = new HttpRequestMessage(HttpMethod.Post, p.Url) { Content = JsonContent.Create(cuerpo) };
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", p.ApiKey);
                if (p.Nombre == "OpenRouter")
                {
                    req.Headers.Add("HTTP-Referer", "https://www.vordia.com.py");
                    req.Headers.Add("X-Title", "VORDIA HQ");
                }
                using var res = await _http.SendAsync(req, ct);
                // 429 = cupo gratuito agotado, 402 = el proveedor pide saldo: se pasa al siguiente sin pagar nada
                if (!res.IsSuccessStatusCode) return null;

                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                var texto = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                // Algunos modelos rodean el JSON con texto o ```: se toma del primer { al último }
                var ini = texto.IndexOf('{');
                var fin = texto.LastIndexOf('}');
                if (ini < 0 || fin <= ini) return null;
                using var json = JsonDocument.Parse(texto[ini..(fin + 1)]);
                var asunto = json.RootElement.GetProperty("asunto").GetString()?.Trim();
                var cuerpoCorreo = json.RootElement.GetProperty("cuerpo").GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(cuerpoCorreo)) return null;
                return new CorreoRedactado(Recortar(asunto, 190), cuerpoCorreo, true);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or IndexOutOfRangeException or System.Collections.Generic.KeyNotFoundException)
            {
                return null;
            }
        }

        private static string Prompt(DatosRedaccion d)
        {
            var tipo = d.Paso <= 1
                ? "el PRIMER correo de contacto"
                : d.Paso >= d.TotalPasos
                    ? "el ÚLTIMO seguimiento: breve, amable, dejando la puerta abierta sin insistir"
                    : $"el seguimiento número {d.Paso - 1}: más corto que el anterior, aportando un dato o beneficio nuevo";
            return $@"Sos un asesor comercial de VORDIA, empresa paraguaya de software de gestión.
Escribí {tipo} para ofrecer VORDIA {d.Producto}.

Destinatario: {(string.IsNullOrWhiteSpace(d.Nombre) ? "(sin nombre: saludá de forma neutra)" : d.Nombre)}
Empresa: {d.Empresa ?? "(sin dato)"}
Contexto real del destinatario (usalo, no inventes otros datos): {d.Contexto ?? "(sin contexto)"}
Oferta: {d.Oferta}
{(d.AsuntoAnterior != null ? $"Asunto del correo anterior: {d.AsuntoAnterior}" : "")}

Reglas:
- Español de Paraguay, con voseo, tono cercano y profesional.
- Máximo 110 palabras. Párrafos cortos. Una sola pregunta al final (por ejemplo, si le sirve una demo de 15 minutos).
- No inventes cifras, clientes, testimonios ni datos que no estén arriba.
- Sin emojis, sin mayúsculas exageradas, sin promesas que no se puedan cumplir.
- No incluyas firma ni pie legal: se agregan después.
- Asunto de menos de 60 caracteres, concreto, sin clickbait.

Respondé solo con JSON: {{""asunto"": ""..."", ""cuerpo"": ""...""}}";
        }

        /// <summary>Plantilla de respaldo cuando no hay IA disponible.</summary>
        public static CorreoRedactado Plantilla(DatosRedaccion d)
        {
            var saludo = PrimerNombre(d.Nombre) is { } nombre ? $"Hola {nombre}" : "Hola";
            var empresa = string.IsNullOrWhiteSpace(d.Empresa) ? "tu empresa" : d.Empresa;
            if (d.Paso <= 1)
                return new CorreoRedactado(
                    $"Una idea para {empresa}",
                    $"{saludo}, te escribe {d.Firma} de VORDIA.\n\nAyudamos a empresas como {empresa} a ordenar su gestión con VORDIA {d.Producto}, sin planillas y con soporte en Paraguay.\n\n{d.Oferta}\n\n¿Te sirve que te muestre en 15 minutos cómo quedaría con tus datos?",
                    false);
            if (d.Paso >= d.TotalPasos)
                return new CorreoRedactado(
                    $"Re: {d.AsuntoAnterior ?? $"VORDIA {d.Producto}"}",
                    $"{saludo}, no te quiero llenar la bandeja: este es mi último mensaje.\n\nSi más adelante querés ver VORDIA {d.Producto}, respondé este correo y lo coordinamos. ¿Te escribo en unos meses?",
                    false);
            return new CorreoRedactado(
                $"Re: {d.AsuntoAnterior ?? $"VORDIA {d.Producto}"}",
                $"{saludo}, retomo mi mensaje anterior por si se perdió.\n\n{d.Oferta}\n\n¿Coordinamos 15 minutos esta semana?",
                false);
        }

        private static string Recortar(string s, int max) => s.Length <= max ? s : s[..max];

        private static readonly string[] Titulos = { "lic", "ing", "dr", "dra", "abog", "arq", "cp", "c.p", "sr", "sra", "srta", "prof", "msc", "mg" };

        /// <summary>Primer nombre sin títulos ("Lic. Ana Pérez" → "Ana"); null si no queda un nombre usable.</summary>
        private static string? PrimerNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            foreach (var parte in nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var limpio = parte.Trim('.', ',').ToLowerInvariant();
                if (limpio.Length < 2 || parte.EndsWith('.') || Titulos.Contains(limpio)) continue;
                return char.ToUpperInvariant(parte[0]) + parte[1..].ToLowerInvariant();
            }
            return null;
        }
    }
}
