using System;
using System.Text.Json;
using System.Threading.Tasks;
using Gesco.Application.Interfaces;
using PuppeteerSharp;

namespace Gesco.Application.Services
{
    public class CloudExtractionService : ICloudExtractionService
    {
        public async Task<string> ExtraerDatosJsonNubeAsync(string loginUrl, string dataUrl, string username, string password)
        {
            // 1. Descargar el navegador si no existe
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // 2. Iniciar navegador (invisible)
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" }
            };

            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();

            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // 3. Interceptar respuestas JSON de red
            string capturedJson = string.Empty;

            page.Response += async (_, e) =>
            {
                try
                {
                    if (!e.Response.Ok) return;

                    // Nos interesa capturar respuestas JSON (AJAX/Fetch)
                    if (e.Response.Headers.TryGetValue("content-type", out var ct) && ct.Contains("application/json"))
                    {
                        // Heurística simple: Si la respuesta es grande o contiene arrays, probablemente es la data de la grilla
                        string responseText = await e.Response.TextAsync();
                        if (responseText.Length > 500 && (responseText.StartsWith("[") || responseText.Contains("\"data\":[")))
                        {
                            capturedJson = responseText;
                        }
                    }
                }
                catch
                {
                    // Ignoramos errores de intercepción rápida
                }
            };

            // 4. Navegar al Login y Autenticarse
            await page.GoToAsync(loginUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

            // Detectamos si hay campos de login comunes (heurística básica)
            try
            {
                // Esperar a que el input exista
                await page.WaitForSelectorAsync("input[type='text'], input[type='email'], input[name*='user']", new WaitForSelectorOptions { Timeout = 5000 });
                
                // Llenar inputs - Esta es una implementación genérica que buscará los primeros campos que parezcan de login
                await page.EvaluateExpressionAsync($@"
                    const userInputs = document.querySelectorAll('input[type=""text""], input[type=""email""], input[name*=""user""]');
                    if(userInputs.length > 0) userInputs[0].value = '{username}';

                    const passInputs = document.querySelectorAll('input[type=""password""]');
                    if(passInputs.length > 0) passInputs[0].value = '{password}';
                ");

                // Click en botón submit / login
                await page.EvaluateExpressionAsync(@"
                    const buttons = Array.from(document.querySelectorAll('button, input[type=""submit""]'));
                    const loginBtn = buttons.find(b => b.innerText.toLowerCase().includes('iniciar') || b.innerText.toLowerCase().includes('login') || b.value?.toLowerCase().includes('login'));
                    if(loginBtn) loginBtn.click();
                    else if(buttons.length > 0) buttons[0].click(); // Fallback
                ");

                // Esperar a que cargue el dashboard
                await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 15000 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login genérico: {ex.Message}. Intentando ir directo a la data url de todas formas.");
            }

            // 5. Navegar a la URL de los datos (Ej: abaco.com.py/api/clientes)
            await page.GoToAsync(dataUrl, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

            // Esperamos unos segundos para que las peticiones AJAX terminen de capturarse
            await Task.Delay(5000);

            // Si interceptamos un JSON, lo devolvemos
            if (!string.IsNullOrEmpty(capturedJson))
            {
                return capturedJson;
            }

            // Fallback: Si no era una API JSON, raspar el DOM de la tabla HTML
            try
            {
                string tableData = await page.EvaluateExpressionAsync<string>(@"
                    JSON.stringify(
                        Array.from(document.querySelectorAll('table tr')).map(tr => 
                            Array.from(tr.querySelectorAll('td, th')).map(td => td.innerText.trim())
                        ).filter(row => row.length > 0)
                    )
                ");
                return tableData;
            }
            catch
            {
                return "{\"error\": \"No se pudo extraer data ni de la red ni del DOM\"}";
            }
        }
    }
}
