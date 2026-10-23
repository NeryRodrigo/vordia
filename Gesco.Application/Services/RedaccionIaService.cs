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
        Task<CorreoRedactado> RedactarAsync(DatosRedaccion d, CancellationToken ct = default);
    }

    /// <summary>
    /// Redacta correos comerciales con Gemini (plan gratuito de Google AI Studio). Clave en "Ia:GeminiApiKey"
    /// y modelo en "Ia:Modelo". Sin clave, o si la IA falla, usa una plantilla fija para no frenar la secuencia.
    /// Solo se envían datos de negocio (empresa, rubro, lo que pidió): nunca contraseñas ni datos sensibles.
    /// </summary>
    public class GeminiRedaccionService : IRedaccionIaService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _modelo;

        public GeminiRedaccionService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Ia:GeminiApiKey"] ?? "";
            _modelo = string.IsNullOrWhiteSpace(config["Ia:Modelo"]) ? "gemini-2.5-flash" : config["Ia:Modelo"]!;
        }

        public bool Configurada => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<CorreoRedactado> RedactarAsync(DatosRedaccion d, CancellationToken ct = default)
        {
            if (!Configurada) return Plantilla(d);
            try
            {
                var cuerpo = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = Prompt(d) } } } },
                    generationConfig = new { responseMimeType = "application/json", temperature = 0.7 }
                };
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_modelo)}:generateContent")
                { Content = JsonContent.Create(cuerpo) };
                req.Headers.Add("x-goog-api-key", _apiKey);
                using var res = await _http.SendAsync(req, ct);
                if (!res.IsSuccessStatusCode) return Plantilla(d);

                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
                var texto = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                using var json = JsonDocument.Parse(texto);
                var asunto = json.RootElement.GetProperty("asunto").GetString()?.Trim();
                var texto2 = json.RootElement.GetProperty("cuerpo").GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(texto2)) return Plantilla(d);
                return new CorreoRedactado(Recortar(asunto, 190), texto2, true);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or IndexOutOfRangeException or System.Collections.Generic.KeyNotFoundException)
            {
                return Plantilla(d);
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
