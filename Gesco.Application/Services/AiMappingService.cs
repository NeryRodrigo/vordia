using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Gesco.Application.Interfaces;

namespace Gesco.Application.Services
{
    public class AiMappingService : IAiMappingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;

        public AiMappingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Se espera que la clave esté en appsettings.json bajo "OpenAI:ApiKey"
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        }

        public async Task<Dictionary<string, string>> GenerarMapeoSemanticoAsync(string rawDataOrigen, string tablaDestinoGesco)
        {
            // Validamos que exista una llave de API. Si no hay, devolvemos un Mock inteligente para modo de desarrollo.
            if (string.IsNullOrEmpty(_openAiApiKey) || _openAiApiKey == "TU_API_KEY_AQUI")
            {
                Console.WriteLine("⚠️ No se detectó API Key de OpenAI. Usando Mapeo Simulado (Mock).");
                return GenerarMapeoSimulado(tablaDestinoGesco);
            }

            // Si el texto es muy largo, lo recortamos para ahorrar tokens.
            string dataMuestra = rawDataOrigen.Length > 2000 ? rawDataOrigen.Substring(0, 2000) : rawDataOrigen;

            var prompt = $@"
Eres un experto Ingeniero de Datos trabajando en la migración hacia el sistema SaaS 'GESCO'.
He aquí una muestra de datos extraída (JSON o DDL) de un sistema de la competencia:
{dataMuestra}

Tu objetivo es analizar los nombres de los campos de origen y mapearlos a las propiedades de la entidad '{tablaDestinoGesco}' de GESCO.
Devuelve ÚNICAMENTE un objeto JSON válido donde las claves (keys) sean los nombres de las columnas originales, y los valores (values) sean los nombres de las propiedades en GESCO a las que corresponden.
Ejemplo: {{ ""CLI_NOM"": ""NombreCompleto"", ""STATUS"": ""EstadoActivo"" }}
No incluyas texto adicional ni formato Markdown, solo el JSON.";

            var requestBody = new
            {
                model = "gpt-4-turbo-preview", // O "gpt-3.5-turbo" si se prefiere costo menor
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente técnico que solo responde en formato JSON puro." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1 // Baja temperatura para respuestas deterministas
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en la API de IA: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            // Extraer la respuesta del modelo
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content)) return new Dictionary<string, string>();

            // Limpiar posibles bloques de markdown
            content = content.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                return dictionary ?? new Dictionary<string, string>();
            }
            catch
            {
                Console.WriteLine("⚠️ La IA no devolvió un JSON válido. Respuesta: " + content);
                return new Dictionary<string, string>(); // Fallback seguro
            }
        }

        private Dictionary<string, string> GenerarMapeoSimulado(string tablaDestinoGesco)
        {
            // Diccionarios simulados asumiendo que el origen traía ciertas columnas típicas de Ábaco/Century
            if (tablaDestinoGesco.Equals("Personas", StringComparison.OrdinalIgnoreCase) || 
                tablaDestinoGesco.Equals("Clientes", StringComparison.OrdinalIgnoreCase))
            {
                return new Dictionary<string, string>
                {
                    { "RUC_CLIENTE", "Documento" },
                    { "RAZON_SOCIAL", "NombreCompleto" },
                    { "DIRECCION_CLI", "Direccion" },
                    { "TEL_CLI", "Telefono" },
                    { "EMAIL_CLI", "Email" }
                };
            }
            
            return new Dictionary<string, string>
            {
                { "COLUMNA_DESCONOCIDA", "CampoGenerico" }
            };
        }
    }
}
