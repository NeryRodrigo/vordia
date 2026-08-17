using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Gesco.Application.Services
{
    /// <summary>
    /// Arranque robusto de Chrome para ANDE (PDF / portal).
    /// Prioriza Chrome/Edge del sistema; si el perfil está trabado, reintenta con otro.
    /// </summary>
    internal static class AndeChromeLauncher
    {
        private static readonly string[] LaunchArgs =
        {
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--disable-blink-features=AutomationControlled",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-extensions",
            "--window-size=1366,900"
        };

        public static string? FindSystemBrowser()
        {
            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("CHROME_PATH"),
                Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH"),
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\Application\chrome.exe"),
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            };

            return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p!));
        }

        public static void ClearProfileLocks(string profileDir)
        {
            if (string.IsNullOrWhiteSpace(profileDir) || !Directory.Exists(profileDir))
                return;

            foreach (var name in new[] { "SingletonLock", "SingletonCookie", "SingletonSocket", "lockfile" })
            {
                try
                {
                    var path = Path.Combine(profileDir, name);
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    /* perfil en uso: el siguiente intento usará otro dir */
                }
            }
        }

        public static async Task<IBrowser> LaunchAsync(
            string? preferredProfileDir,
            bool headless,
            ILogger? logger = null)
        {
            var exe = FindSystemBrowser();
            if (string.IsNullOrWhiteSpace(exe))
            {
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();
                logger?.LogInformation("ANDE Chrome: usando Chromium descargado por PuppeteerSharp");
            }
            else
            {
                logger?.LogInformation("ANDE Chrome: usando navegador del sistema {Exe}", exe);
            }

            var attempts = new List<string?>();
            if (!string.IsNullOrWhiteSpace(preferredProfileDir))
                attempts.Add(preferredProfileDir);
            attempts.Add(Path.Combine(
                Path.GetTempPath(),
                "Gesco",
                "ande-chrome-" + Guid.NewGuid().ToString("N")[..10]));
            attempts.Add(null); // sin UserDataDir

            Exception? last = null;
            foreach (var profile in attempts)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(profile))
                    {
                        Directory.CreateDirectory(profile);
                        ClearProfileLocks(profile);
                    }

                    var opts = new LaunchOptions
                    {
                        Headless = headless,
                        DefaultViewport = new ViewPortOptions { Width = 1366, Height = 900 },
                        Args = LaunchArgs,
                        Timeout = 60000
                    };
                    if (!string.IsNullOrWhiteSpace(exe))
                        opts.ExecutablePath = exe;
                    if (!string.IsNullOrWhiteSpace(profile))
                        opts.UserDataDir = profile;

                    var browser = await Puppeteer.LaunchAsync(opts);
                    logger?.LogInformation(
                        "ANDE Chrome iniciado headless={Headless} profile={Profile}",
                        headless,
                        profile ?? "(sin perfil)");
                    return browser;
                }
                catch (Exception ex)
                {
                    last = ex;
                    logger?.LogWarning(
                        "ANDE Chrome launch falló (profile={Profile}): {Msg}",
                        profile ?? "(sin perfil)",
                        ex.Message);
                }
            }

            throw last ?? new ProcessException("Failed to launch browser!");
        }
    }
}
