using Azure;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.DTOs;
using Gesco.Application.Interfaces;
using Gesco.Domain.Entities;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Page = PuppeteerSharp.Page;

namespace Gesco.Application.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly IApplicationDbContext _context;

        public MigrationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> MigrarDatosDesdeExcel(Stream archivoExcel, string tablaDestino, int empresaId)
        {
            using var workbook = new XLWorkbook(archivoExcel);
            var worksheet = workbook.Worksheet(1); // Tomamos la primera hoja
            var rows = worksheet.RowsUsed().Skip(1); // Saltamos la cabecera (fila 1)

            int cont = 0;
            
            // Lógica de "Modo Demo" SaaS
            bool hasLicense = false;
            if (empresaId == 7) {
                hasLicense = true; // Condominio Aguz bypass
            } else {
                hasLicense = await _context.LicenciasModulos
                    .Include(l => l.Modulo)
                    .AnyAsync(l => l.EmpresaId == empresaId && l.Modulo.NombreModulo.Contains("Intelligence") && l.Activo && (l.FechaExpiracion == null || l.FechaExpiracion > DateTime.Now));
            }

            foreach (var row in rows)
            {
                // Modo Demo: Limitar a 20 registros
                if (!hasLicense && cont >= 20)
                {
                    break;
                }
                if (tablaDestino.ToLower() == "persona")
                {
                    // Mapeo manual inicial (Luego lo haremos dinámico con tu tabla MapeoDetalle)
                    // Asumimos: Col A = Razon Social, Col B = RUC/CI, Col C = EsCliente (S/N)

                    var rucCompleto = row.Cell(2).GetValue<string>();
                    var (ruc, dv) = SepararRuc(rucCompleto);

                    var nuevaPersona = new Persona
                    {
                        EmpresaId = empresaId,
                        RazonSocial = row.Cell(1).GetValue<string>(),
                        Documento = ruc,
                        DV = dv,
                        EsCliente = row.Cell(3).GetValue<string>().ToUpper() == "S",
                        EsProveedor = row.Cell(4).GetValue<string>().ToUpper() == "S",
                        Telefono = row.Cell(5).GetValue<string>()
                    };

                    // Evitar duplicados por RUC dentro de la misma empresa
                    var existe = await _context.Personas
                        .AnyAsync(p => p.Documento == ruc && p.EmpresaId == empresaId);

                    if (!existe)
                    {
                        _context.Personas.Add(nuevaPersona);
                        cont++;
                    }
                }
                // Aquí puedes agregar: else if (tablaDestino == "producto") { ... }
            }

            await _context.SaveChangesAsync();
            return cont;
        }

        public async Task<int> MigrarContratosPdf(List<Microsoft.AspNetCore.Http.IFormFile> pdfs, int empresaId)
        {
            int contratosProcesados = 0;

            var unidadesEmpresa = await _context.UnidadesCondominio
                .Where(u => u.EmpresaId == empresaId)
                .ToListAsync();

            foreach (var file in pdfs)
            {
                using var stream = file.OpenReadStream();
                using var document = UglyToad.PdfPig.PdfDocument.Open(stream);

                foreach (var page in document.GetPages())
                {
                    string pageText = page.Text;
                    if (string.IsNullOrWhiteSpace(pageText)) continue;

                    // El PDF puede decir "Condominio Agus 1" o "Agus 2" o "Aguz 1".
                    string nombreCondominioEncontrado = null;
                    if (System.Text.RegularExpressions.Regex.IsMatch(pageText, @"(?i)Agus\s*1|Aguz\s*1"))
                    {
                        nombreCondominioEncontrado = "Aguz 1";
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(pageText, @"(?i)Agus\s*2|Aguz\s*2"))
                    {
                        nombreCondominioEncontrado = "Aguz 2";
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(pageText, @"(?i)Agus\s*3|Aguz\s*3"))
                    {
                        nombreCondominioEncontrado = "Aguz 3";
                    }

                    UnidadCondominio unidad = null;
                    
                    if (nombreCondominioEncontrado != null)
                    {
                        unidad = unidadesEmpresa.FirstOrDefault(u => 
                            u.NombreCondominio == nombreCondominioEncontrado && 
                            pageText.Contains(u.CodigoUnidad, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    // Fallback si no encontró el condominio explícito
                    if (unidad == null)
                    {
                        unidad = unidadesEmpresa.FirstOrDefault(u => pageText.Contains(u.CodigoUnidad, StringComparison.OrdinalIgnoreCase));
                    }

                    if (unidad != null)
                    {
                        // Extracción con Regex
                        string nombreCompleto = "Inquilino " + unidad.CodigoUnidad;
                        string cedula = "1.000.000";
                        string telefono = "0981000000";
                        string email = $"inquilino.{unidad.CodigoUnidad.ToLower()}@gmail.com";
                        decimal montoMensual = 1500000;
                        DateTime fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        DateTime fechaFin = fechaInicio.AddYears(1).AddDays(-1);

                        // Intentar extraer Cédula
                        var matchCedula = System.Text.RegularExpressions.Regex.Match(pageText, @"(?i)(?:C\.I\.|C\.I\. N°|C\.I\. N|cédula de identidad|N°|Nro\.)\s*([\d\.]+)");
                        if (matchCedula.Success)
                        {
                            cedula = matchCedula.Groups[1].Value.Trim();
                        }

                        // Intentar extraer Monto
                        var matchMonto = System.Text.RegularExpressions.Regex.Match(pageText, @"(?i)(?:Gs\.|Guaraníes|G\.)\s*([\d\.]+)");
                        if (matchMonto.Success)
                        {
                            if (decimal.TryParse(matchMonto.Groups[1].Value.Replace(".", ""), out decimal montoExtraido) && montoExtraido > 10000)
                            {
                                montoMensual = montoExtraido;
                            }
                        }

                        // Intentar extraer Nombre (buscando patrón de nombre en mayúsculas o después de Sr/Sra)
                        var matchNombre = System.Text.RegularExpressions.Regex.Match(pageText, @"(?i)(?:El Sr\.|La Sra\.|El Señor|La Señora|Don|Doña)\s+([A-ZÁÉÍÓÚÑ][a-záéíóúñA-ZÁÉÍÓÚÑ\s]+?)(?:,?\s+con|\s+de\s+nacionalidad)");
                        if (matchNombre.Success)
                        {
                            nombreCompleto = matchNombre.Groups[1].Value.Trim();
                        }

                        // 1. Crear o buscar inquilino (Persona)
                        var inquilino = await _context.Personas
                            .FirstOrDefaultAsync(p => p.Documento == cedula && p.EmpresaId == empresaId);

                        if (inquilino == null)
                    {
                        inquilino = new Persona
                        {
                            EmpresaId = empresaId,
                            RazonSocial = nombreCompleto,
                            Documento = cedula,
                            EsCliente = true,
                            Telefono = telefono,
                            Email = email,
                        };
                        _context.Personas.Add(inquilino);
                        await _context.SaveChangesAsync(); // Guardar para obtener el ID
                    }

                    // 2. Actualizar la Unidad con el nombre del inquilino
                    unidad.Estado = "Ocupado";
                    _context.UnidadesCondominio.Update(unidad);

                    // 3. Crear el Contrato
                    var contrato = new ContratoInquilino
                    {
                        InquilinoId = inquilino.PersonaId,
                        UnidadId = unidad.UnidadId,
                        MontoMensualAlquiler = montoMensual,
                        DepositoGarantia = montoMensual,
                        CantidadOcupantes = 1,
                        Estado = "Activo",
                        EstaActivo = true,
                        FechaIngreso = DateTime.UtcNow,
                        Observaciones = "Migrado desde OCR de PDF",
                        FechaInicioContrato = fechaInicio,
                        FechaFinContrato = fechaFin
                    };

                    _context.ContratosInquilinos.Add(contrato);
                    contratosProcesados++;
                }
            }
        }

        await _context.SaveChangesAsync();
            return contratosProcesados;
        }

        public async Task<List<Persona>> ObtenerPersonas(int empresaId)
        {
            var personas = await _context.Personas
                                .Where(p => p.EmpresaId == empresaId)
                                .ToListAsync();

            foreach (var persona in personas)
            {
                // Verificamos si existe al menos una factura para esta persona
                persona.TieneFacturas = await _context.VentaCabeceras
                    .AnyAsync(v => v.PersonaId == persona.PersonaId);
            }

            return personas;
        }

        public async Task<object> ObtenerAgentes(int empresaId)
        {
            return await _context.Personas
                .Where(p => p.EmpresaId == empresaId && p.Documento == "S/D")
                .Select(p => new {
                    personaId = p.PersonaId,
                    razonSocial = p.RazonSocial
                })
                .ToListAsync();
        }

        // Función útil para el mercado paraguayo (Separa RUC de DV)
        private (string ruc, int? dv) SepararRuc(string rucCompleto)
        {
            if (string.IsNullOrWhiteSpace(rucCompleto)) return ("", null);

            var partes = rucCompleto.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out int dv))
            {
                return (partes[0], dv);
            }
            return (rucCompleto, null);
        }

        public async Task<List<VentaCabecera>> ObtenerVentas(int empresaId)
        {
            return await _context.VentaCabeceras
                .Include(v => v.Cliente) // Traemos el nombre del cliente relacionado
                .Where(v => v.EmpresaId == empresaId)
                .OrderByDescending(v => v.Fecha) // Las más nuevas primero
                .ToListAsync();
        }

        // MÉTODO AUXILIAR: El cerebro de la secuencia alfanumérica
        private string IncrementarAlfanumerico(string cadena)
        {
            char[] caracteres = cadena.ToCharArray();

            for (int i = caracteres.Length - 1; i >= 0; i--)
            {
                char c = caracteres[i];

                if (c < '9') // Es número del 0 al 8
                {
                    caracteres[i]++;
                    return new string(caracteres);
                }
                else if (c == '9') // Salto de 9 a A (O a 0 si querés solo números)
                {
                    caracteres[i] = 'A';
                    return new string(caracteres);
                }
                else if (c < 'Z') // Es letra de A a Y
                {
                    caracteres[i]++;
                    return new string(caracteres);
                }
                else // Es 'Z', lo volvemos '0' y llevamos el acarreo a la izquierda
                {
                    caracteres[i] = '0';
                }
            }

            // Si salimos del loop es porque todo era 'ZZZZZZZ' -> se vuelve '10000000'
            return "1" + new string(caracteres);
        }

        private void AsignarValorDinamico(object objeto, string nombrePropiedad, string valor)
        {
            PropertyInfo propertyInfo = objeto.GetType().GetProperty(nombrePropiedad);
            if (propertyInfo != null && !string.IsNullOrEmpty(valor))
            {
                var tipoDestino = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                var valorConvertido = Convert.ChangeType(valor, tipoDestino);
                propertyInfo.SetValue(objeto, valorConvertido);
            }
        }

        private string CalcularMD5(string input)
        {
            // Convertimos el texto (JSON de la fila) en bytes
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            // Calculamos el Hash usando la versión optimizada de .NET 8
            byte[] hashBytes = MD5.HashData(inputBytes);

            // Lo convertimos a una cadena Hexadecimal (Ej: "A1B2C3...")
            return Convert.ToHexString(hashBytes);
        }

        private decimal ParsearDecimalSeguro(IXLCell celda)
        {
            try
            {
                // 1. Si la celda está vacía, retornamos 0
                if (celda.IsEmpty()) return 0;

                // 2. Intentamos obtener el valor directamente
                if (celda.DataType == XLDataType.Number)
                    return celda.GetValue<decimal>();

                // 3. Si falla, lo tratamos como texto y limpiamos (sacamos Gs, $, puntos de miles, etc)
                string valorTexto = celda.GetValue<string>()
                    .Replace("Gs.", "")
                    .Replace("$", "")
                    .Replace(".", "") // Quitamos puntos de miles
                    .Replace(",", ".") // Cambiamos coma decimal por punto para parseo estándar
                    .Trim();

                if (decimal.TryParse(valorTexto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal resultado))
                {
                    return resultado;
                }

                return 0;
            }
            catch
            {
                return 0; // Si todo falla, devolvemos 0 para que el sistema no explote
            }
        }

        public async Task<bool> ActualizarPropiedad(Propiedad propInput)
        {
            try
            {
                var propiedadExistente = await _context.Propiedades
                    .FirstOrDefaultAsync(p => p.PropiedadId == propInput.PropiedadId);

                if (propiedadExistente == null) return false;

                // --- LÓGICA PARA EL AGENTE ---
                // Si recibes el nombre del agente desde el frontend
                if (propInput.Agente != null && !string.IsNullOrEmpty(propInput.Agente.RazonSocial))
                {
                    // Buscamos en la tabla Persona (o Agentes) por el nombre
                    var personaEncontrada = await _context.Personas
                        .FirstOrDefaultAsync(p => p.RazonSocial.ToLower() == propInput.Agente.RazonSocial.ToLower());

                    if (personaEncontrada != null)
                    {
                        // Asignamos el ID real de la base de datos
                        propiedadExistente.AgenteId = personaEncontrada.PersonaId;
                    }
                }

                // --- ACTUALIZACIÓN DE CAMPOS BÁSICOS ---
                propiedadExistente.Titulo = propInput.Titulo;
                propiedadExistente.MLSID = propInput.MLSID;
                propiedadExistente.Ciudad = propInput.Ciudad;
                propiedadExistente.PrecioVentaGs = propInput.PrecioVentaGs;
                propiedadExistente.DescripcionRemax = propInput.DescripcionRemax ?? propiedadExistente.DescripcionRemax;

                // --- CAMPOS TÉCNICOS ---
                propiedadExistente.NroDormitorios = propInput.NroDormitorios;
                propiedadExistente.NroBaños = propInput.NroBaños;
                propiedadExistente.AreaConstruccionM2 = propInput.AreaConstruccionM2;
                // ... actualiza los demás campos técnicos igual que estos

                // --- GEOLOCALIZACIÓN ---
                propiedadExistente.Latitud = propInput.Latitud;
                propiedadExistente.Longitud = propInput.Longitud;

                // --- FECHAS ---
                if (propInput.FechaCaducidad.HasValue)
                    propiedadExistente.FechaCaducidad = propInput.FechaCaducidad;

                if (propInput.FechaFirmaContrato.HasValue)
                    propiedadExistente.FechaFirmaContrato = propInput.FechaFirmaContrato;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error crítico en SQL: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
        }

        // âœ… 2. ELIMINAR PROPIEDAD
        public async Task<bool> EliminarPropiedad(int id)
        {
            try
            {
                var prop = await _context.Propiedades.FindAsync(id);
                if (prop == null) return false;

                _context.Propiedades.Remove(prop);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando propiedad: {ex.Message}");
                return false;
            }
        }

        // âœ… 3. ELIMINAR ANÁLISIS (ACM)
        /*public async Task<bool> EliminarAnalisis(int id)
        {
            try
            {
                // Asumiendo que tu DbSet se llama 'AnalisisMercado' o similar
                // Ajusta el nombre de la tabla según tu DbContext
                var analisis = await _context.AnalisisMercados.FindAsync(id);

                if (analisis == null) return false;

                _context.AnalisisMercados.Remove(analisis);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando análisis: {ex.Message}");
                return false;
            }
        }*/


        private void ConfigurarLecturaJson(IPage page, ConcurrentDictionary<string, PropiedadGescoDto> props, ConcurrentDictionary<int, string> agentes)
        {
            int agentId = 0;
            string agentName = "";
            page.Response += async (_, e) =>
            {
                try
                {
                    if (!e.Response.Ok) return;

                    if (!e.Response.Headers.TryGetValue("content-type", out var ct) || !ct.Contains("application/json"))
                        return;

                    var url = e.Response.Url;
                    bool esAgente = url.Contains("/search/agent-search/docs/search");
                    bool esPropiedad = url.Contains("/search/listing-search/docs/search");

                    if (!esAgente && !esPropiedad) return;

                    // LEEMOS EL CUERPO UNA SOLA VEZ Y LO GUARDAMOS
                    string responseText = await e.Response.TextAsync();
                    if (string.IsNullOrWhiteSpace(responseText)) return;

                    using var doc = JsonDocument.Parse(responseText);

                    // ðŸ”¹ PROCESAR AGENTES
                    if (esAgente)
                    {
                        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
                        {
                            var src = item.GetProperty("content");
                            // Guardamos el nombre para que esté disponible cuando llegue la respuesta de la propiedad
                            agentName = src.GetProperty("AgentName").ToString();
                            int id = src.GetProperty("AgentId").GetInt32();
                            agentes.TryAdd(id, agentName);
                        }
                    }

                    // ðŸ”¹ PROCESAR PROPIEDADES
                    if (esPropiedad)
                    {
                        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
                        {
                            var c = item.GetProperty("content");
                            var mls = c.GetProperty("MLSID").ToString();

                            var dto = props.GetOrAdd(mls, _ => new PropiedadGescoDto { MLSID = mls });

                            dto.AgentId = c.GetProperty("AgentId").GetInt32();
                            dto.NombreAgente = agentName; // Usamos el nombre capturado arriba
                            dto.Ciudad = c.GetProperty("Province").ToString();
                            dto.Precio = c.GetProperty("ListingPrice").GetDecimal();

                            if (c.TryGetProperty("Location", out var loc))
                            {
                                dto.Longitud = loc.GetProperty("coordinates")[0].GetDouble();
                                dto.Latitud = loc.GetProperty("coordinates")[1].GetDouble();
                            }

                            if (c.TryGetProperty("ListingMetaTags", out var tags) && tags.GetArrayLength() > 0)
                                dto.Titulo = tags[0].GetProperty("MetaTitle").GetString();

                            if (c.TryGetProperty("ListingDescriptions", out var d) && d.GetArrayLength() > 0)
                                dto.Descripcion = d[0].GetProperty("Description").GetString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si falla una lectura (por Target Closed), no pasa nada, 
                    // el catch evita que la app se detenga y permite que la siguiente respuesta se procese.
                    Console.WriteLine($"Aviso: No se pudo leer una respuesta (probablemente se cerró rápido).");
                }
                
            };
        }

        private async Task<SyncEstado> ObtenerEstado(int empresaId)
        {
            return await _context.SyncEstado
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId)
                ?? new SyncEstado
                {
                    EmpresaId = empresaId,
                    Estado = "RUNNING",
                    TotalInsertados = 0
                };
        }

        public async Task PausarSync(int empresaId)
        {
            var estado = await _context.SyncEstado
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (estado == null) return;

            estado.Estado = "PAUSED";
            estado.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<SyncEstadoDto> ObtenerSyncStatus(int empresaId)
        {
            var estado = await _context.SyncEstado
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (estado == null)
            {
                return new SyncEstadoDto
                {
                    Total = 0,
                    Estado = "COMPLETED"
                };
            }

            return new SyncEstadoDto
            {
                Total = estado.TotalInsertados,
                UltimoMLSID = estado.UltimoMLSIDProcesado,
                Estado = estado.Estado
            };
        }

        private bool ExisteMLSID(HashSet<string> cache, string mls)
        {
            return cache.Contains(mls);
        }

        private async Task GuardarEstado(SyncEstado estado)
        {
            estado.FechaActualizacion = DateTime.Now;
            _context.SyncEstado.Update(estado);
            await _context.SaveChangesAsync();
        }

        public async Task EjecutarAsync(int empresaId, string? urlManual)
        {
            var estado = await ObtenerEstado(empresaId);

            if (estado.Estado == "PAUSED")
                return;

            estado.Estado = "RUNNING";
            await GuardarEstado(estado);

            var totalInsertados = await ScrappingMasivoRemax(empresaId, urlManual);

            //estado.TotalInsertados += totalInsertados;
            estado.Estado = "FINISHED";

            await GuardarEstado(estado);
        }

        private async Task GuardarBuffer(List<Propiedad> buffer, SyncEstado estado)
        {
            int cantidad = buffer.Count;
            string ultimoMls = buffer.Last().MLSID;

            await _context.BulkInsertAsync(buffer);

            estado.TotalInsertados += cantidad;
            estado.UltimoMLSIDProcesado = ultimoMls;
            estado.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            buffer.Clear(); // Limpiamos la lista para la siguiente tanda
            Console.WriteLine($"âœ… Guardado bloque de {cantidad} propiedades en la BD.");
        }

        private async Task<SyncEstado> PrepararEstado(int empresaId)
        {
            var estado = await _context.SyncEstado
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (estado == null)
            {
                estado = new SyncEstado
                {
                    EmpresaId = empresaId,
                    Estado = "RUNNING",
                    TotalInsertados = 0,
                    UltimoMLSIDProcesado = "",
                    FechaActualizacion = DateTime.UtcNow
                };

                _context.SyncEstado.Add(estado);
            }
            else
            {
                estado.Estado = "RUNNING";
                estado.TotalInsertados = 0;
                estado.UltimoMLSIDProcesado = "";
                estado.FechaActualizacion = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return estado;
        }

        private async Task<int> ObtenerAgente(
                    PropiedadGescoDto dto,
                    int empresaId,
                    Dictionary<string, int> agentesCache,
                    ConcurrentDictionary<int, string> agentesJson)
        {
            var nombreAgente =
            agentesJson.TryGetValue(dto.AgentId, out var nombreJson)
            ? nombreJson?.Trim()
            : dto.NombreAgente?.Trim();

            if (string.IsNullOrWhiteSpace(nombreAgente))
                nombreAgente = "Agente Remax";

            // ðŸ”Ž Cache en memoria
            if (agentesCache.TryGetValue(nombreAgente, out var agenteId))
                return agenteId;

            // ðŸ”Ž Buscar en BD (doble seguridad)
            var agenteDb = await _context.Personas
                .Where(p => p.EmpresaId == empresaId && !p.EsCliente)
                .FirstOrDefaultAsync(p => p.RazonSocial == nombreAgente);

            if (agenteDb != null)
            {
                agentesCache[nombreAgente] = agenteDb.PersonaId;
                return agenteDb.PersonaId;
            }

            // âž• Crear nuevo agente
            var nuevo = new Persona
            {
                EmpresaId = empresaId,
                RazonSocial = nombreAgente,
                EsCliente = false,
                Documento = "S/D"
            };

            _context.Personas.Add(nuevo);
            await _context.SaveChangesAsync(); // ðŸ” GARANTIZA EXISTENCIA REAL

            agentesCache[nombreAgente] = nuevo.PersonaId;
            return nuevo.PersonaId;
        }

        private async Task<Dictionary<string, int>> CargarAgentes(int empresaId)
        {
            return await _context.Personas
                .Where(p => p.EmpresaId == empresaId && !p.EsCliente)
                .ToDictionaryAsync(
                    p => p.RazonSocial.Trim(),
                    p => p.PersonaId
                );
        }

        private async Task<bool> ProcesarPropiedad(
                                            string url,
                                            HashSet<string> mlsPermitidos,
                                            Browser browser,
                                            List<Page> pages,
                                            string userAgent,
                                            int empresaId,
                                            List<Propiedad> bufferInsert,
                                            HashSet<string> mlsProcesadosOk,          // ðŸ‘ˆ renombralo mentalmente: OK de esta corrida
                                            HashSet<string> mlsRegistrados,
                                            Dictionary<string, int> agentesCache,
                                            ConcurrentDictionary<string, PropiedadGescoDto> tempData,
                                            ConcurrentDictionary<int, string> agentesJson,
                                            SyncEstado estado,
                                            HashSet<string> mlsFallidos,              // ðŸ‘ˆ ACÁ se guardan fallidos
                                            int maxRetries = 3,
                                            bool fuerzaBrowserNuevo = false
                                        )
        {
            string mls = url.Split('/').Last();

            if (!mlsPermitidos.Contains(mls))
                return false;

            // âœ… Si ya existe en BD o ya se procesó OK, no hagas nada (pero cuenta como OK)
            if (mlsRegistrados.Contains(mls) || mlsProcesadosOk.Contains(mls))
                return true;

            for (int intento = 1; intento <= maxRetries; intento++)
            {
                Page pageActual;

                try
                {
                    // â™»ï¸ recrear página si está muerta o si forzás nuevo contexto
                    if (fuerzaBrowserNuevo || pages[0] == null || pages[0].IsClosed)
                    {
                        if (pages[0] != null)
                        {
                            try { await pages[0].DisposeAsync(); } catch { }
                        }

                        pageActual = (Page)await browser.NewPageAsync();
                        await pageActual.SetUserAgentAsync(userAgent);
                        ConfigurarLecturaJson(pageActual, tempData, agentesJson);
                        pages[0] = pageActual;
                    }
                    else
                    {
                        pageActual = pages[0];
                    }

                    await pageActual.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                        Timeout = 60000
                    });

                    await Task.Delay(2000); // deja que dispare JS

                    // 1) Intentar por JSON capturado
                    PropiedadGescoDto dto = null;
                    tempData.TryGetValue(mls, out dto);

                    // 2) Fallback HTML si no vino JSON
                    if (dto == null)
                    {
                        dto = await pageActual.EvaluateFunctionAsync<PropiedadGescoDto>(@"
                            (mls) => ({
                                MLSID: mls,
                                Titulo: document.querySelector('h1')?.innerText || 'S/D',
                                Precio: 0,
                                Ciudad: document.querySelector('.location')?.innerText || 'S/D',
                                Latitud: 0,
                                Longitud: 0,
                                AgentId: 0,
                                NombreAgente: document.querySelector('.agent-name')?.innerText || 'Agente Remax'
                            })
                        ", mls);
                    }

                    // âš ï¸ Si ni así hay datos mínimos, reintentar
                    if (dto == null || string.IsNullOrWhiteSpace(dto.MLSID))
                        throw new Exception("DTO vacío");

                    // âœ… ya existe en BD -> OK
                    if (mlsRegistrados.Contains(dto.MLSID))
                    {
                        mlsProcesadosOk.Add(dto.MLSID);
                        tempData.TryRemove(mls, out _);
                        return true;
                    }

                    // âœ… si ya lo insertamos en esta corrida -> OK
                    if (mlsProcesadosOk.Contains(dto.MLSID))
                    {
                        tempData.TryRemove(mls, out _);
                        return true;
                    }

                    // ðŸ” Crear/obtener agente de forma segura
                    var nombreAgente = (dto.NombreAgente ?? "Agente Remax").Trim();
                    if (!agentesCache.TryGetValue(nombreAgente, out int agenteIdDb))
                    {
                        // buscar por si existe en BD (doble seguro)
                        var agenteDb = await _context.Personas.FirstOrDefaultAsync(p =>
                            p.EmpresaId == empresaId && !p.EsCliente && p.RazonSocial == nombreAgente);

                        if (agenteDb != null)
                        {
                            agenteIdDb = agenteDb.PersonaId;
                        }
                        else
                        {
                            var nuevo = new Persona
                            {
                                EmpresaId = empresaId,
                                RazonSocial = nombreAgente,
                                EsCliente = false,
                                Documento = "S/D"
                            };
                            _context.Personas.Add(nuevo);
                            await _context.SaveChangesAsync();
                            agenteIdDb = nuevo.PersonaId;
                        }

                        agentesCache[nombreAgente] = agenteIdDb;
                    }

                    // âœ… Insertar propiedad al buffer
                    bufferInsert.Add(new Propiedad
                    {
                        EmpresaId = empresaId,
                        MLSID = dto.MLSID,
                        PrecioVentaGs = dto.Precio,
                        Latitud = dto.Latitud,
                        Longitud = dto.Longitud,
                        Ciudad = dto.Ciudad,
                        AgenteId = agenteIdDb,
                        Titulo = dto.Titulo,
                        DescripcionRemax = dto.Titulo,
                        Sync = true,
                        EsAprobada = true,
                        FechaFirmaContrato = DateTime.Now,
                        FechaCaducidad = DateTime.UtcNow.AddMonths(6)
                    });

                    estado.UltimoMLSIDProcesado = dto.MLSID;

                    // âœ… recién ahora lo marcamos OK
                    mlsProcesadosOk.Add(dto.MLSID);

                    tempData.TryRemove(mls, out _);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"âš ï¸ MLS {mls} intento {intento}/{maxRetries} falló: {ex.Message}");

                    // si es el último intento, marcar fallido
                    if (intento == maxRetries)
                    {
                        mlsFallidos.Add(mls);
                        tempData.TryRemove(mls, out _);
                        return false;
                    }

                    // fuerza recreación en el siguiente intento
                    fuerzaBrowserNuevo = true;
                }
            }

            // no debería llegar
            mlsFallidos.Add(mls);
            return false;
        }

        private async Task AutoScrollAsync(Page page)
        {
            await page.EvaluateExpressionAsync(@"
                new Promise(async (resolve) => {
                    let totalHeight = 0;
                    const distance = 800;
                    const timer = setInterval(() => {
                        window.scrollBy(0, distance);
                        totalHeight += distance;

                        if (totalHeight >= document.body.scrollHeight - window.innerHeight) {
                            clearInterval(timer);
                            resolve(true);
                        }
                    }, 300);
                });
            ");
        }

        private async Task<(HashSet<string> whitelist, int totalEsperado)> RecolectarWhitelistAsync(Page pageListado, string baseUrl)
        {
            var whitelist = new HashSet<string>();
            int totalEsperado = 0;

            await pageListado.GoToAsync(baseUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 60000
            });

            // ðŸ”¢ total esperado (Propiedades Encontradas)
            var textCount = await pageListado.EvaluateExpressionAsync<string>(@"
                (() => {
                    const p = Array.from(document.querySelectorAll('p'))
                      .find(el => el.innerText.includes('Propiedades Encontradas'));
                    return p ? p.innerText : '0';
                })()
            ");

            int.TryParse(System.Text.RegularExpressions.Regex.Match(textCount ?? "0", @"\d+").Value, out totalEsperado);
            Console.WriteLine($"ðŸŽ¯ Total esperado del listado: {totalEsperado}");

            int paginasSinNuevos = 0;
            int pageNum = 1;

            while (true)
            {
                await pageListado.WaitForSelectorAsync(".listing-card", new WaitForSelectorOptions { Timeout = 60000 });

                // âœ… Scroll para que cargue todo el contenido de la página
                await AutoScrollAsync(pageListado);
                await Task.Delay(800);

                // âœ… Extraer MLS visibles en esta página
                var mlsPage = await pageListado.EvaluateExpressionAsync<string[]>(@"
                    Array.from(document.querySelectorAll('.listing-card a.listing-info'))
                        .map(a => a.href)
                        .filter(h => h && h.includes('/propiedades/'))
                        .map(h => h.split('/').pop());
                ");

                int antes = whitelist.Count;
                foreach (var m in (mlsPage ?? Array.Empty<string>()))
                    whitelist.Add(m);

                int nuevos = whitelist.Count - antes;

                Console.WriteLine($"ðŸ“„ Página UI {pageNum}: +{nuevos} MLS (acum {whitelist.Count})");
                paginasSinNuevos = (nuevos == 0) ? paginasSinNuevos + 1 : 0;

                // âœ… Salidas seguras
                if (totalEsperado > 0 && whitelist.Count >= totalEsperado)
                    break;

                if (paginasSinNuevos >= 2)
                    break;

                // âœ… Detectar botón Next (MUI)
                var canGoNext = await pageListado.EvaluateExpressionAsync<bool>(@"
                    (() => {
                        const btn =
                            document.querySelector('button[aria-label=""Go to next page""]') ||
                            document.querySelector('button[aria-label=""Next page""]') ||
                            document.querySelector('button[title=""Next page""]');
                        return !!btn && !btn.disabled;
                    })()
                ");

                if (!canGoNext)
                    break;

                // âœ… Click Next
                await pageListado.EvaluateExpressionAsync(@"
                    (() => {
                        const btn =
                            document.querySelector('button[aria-label=""Go to next page""]') ||
                            document.querySelector('button[aria-label=""Next page""]') ||
                            document.querySelector('button[title=""Next page""]');
                        if (btn) btn.click();
                    })()
                ");

                await Task.Delay(1500);
                pageNum++;
            }

            Console.WriteLine($"âœ… Whitelist final: {whitelist.Count} (esperado {totalEsperado})");
            return (whitelist, totalEsperado);
        }

        public async Task<SyncEstadoDto> ScrappingMasivoRemax(int empresaId, string? urlManual)
        {
            var estado = await PrepararEstado(empresaId);

            const string USER_AGENT =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36";

            try
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();

                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-blink-features=AutomationControlled"
                    }
                });

                // Página para listado
                var pageListado = await browser.NewPageAsync();
                await pageListado.SetUserAgentAsync(USER_AGENT);

                // Cache DB MLS ya existentes
                var mlsRegistrados = new HashSet<string>(
                    await _context.Propiedades
                        .Where(p => p.EmpresaId == empresaId && p.MLSID != null)
                        .Select(p => p.MLSID!)
                        .ToListAsync()
                );

                // Cache agentes
                var agentesCache = await CargarAgentes(empresaId);

                // Buffers
                var bufferInsert = new List<Propiedad>();
                var tempData = new ConcurrentDictionary<string, PropiedadGescoDto>();
                var agentesJson = new ConcurrentDictionary<int, string>();

                // âœ… Estos dos son los importantes
                var mlsProcesadosOk = new HashSet<string>(); // OK en esta corrida
                var mlsFallidos = new HashSet<string>();     // fallidos

                int totalInsertados = 0;

                // Pool de páginas (para propiedades)
                var pages = new List<Page>();
                for (int i = 0; i < 3; i++)
                {
                    var p = await browser.NewPageAsync();
                    await p.SetUserAgentAsync(USER_AGENT);
                    ConfigurarLecturaJson(p, tempData, agentesJson);
                    pages.Add((Page)p);
                }

                // ============================================================
                // CASO 1: LINK DIRECTO
                // ============================================================
                if (!string.IsNullOrWhiteSpace(urlManual) && urlManual.Contains("/propiedades/"))
                {
                    var mls = urlManual.Split('/').Last();
                    var whitelist = new HashSet<string> { mls };

                    await ProcesarPropiedad(
                        urlManual,
                        whitelist,
                        (Browser)browser,
                        pages,
                        USER_AGENT,
                        empresaId,
                        bufferInsert,
                        mlsProcesadosOk,
                        mlsRegistrados,
                        agentesCache,
                        tempData,
                        agentesJson,
                        estado,
                        mlsFallidos,
                        maxRetries: 3,
                        fuerzaBrowserNuevo: true
                    );
                }
                // ============================================================
                // CASO 2: LISTADO
                // ============================================================
                else if (!string.IsNullOrWhiteSpace(urlManual))
                {
                    var baseUrl = urlManual;

                    // âœ… 1) Recolectar todos los MLS del listado (sin depender de &Page=)
                    var (whitelist, totalEsperado) = await RecolectarWhitelistAsync((Page)pageListado, baseUrl);

                    // âœ… 2) Procesar SOLO los MLS del listado
                    foreach (var mls in whitelist)
                    {
                        if (mlsRegistrados.Contains(mls) || mlsProcesadosOk.Contains(mls))
                            continue;

                        var urlProp = $"https://www.remax.com.py/propiedades/{mls}";

                        bool ok = await ProcesarPropiedad(
                            urlProp,
                            new HashSet<string> { mls },
                            (Browser)browser,
                            pages,
                            USER_AGENT,
                            empresaId,
                            bufferInsert,
                            mlsProcesadosOk,     // HashSet OK corrida
                            mlsRegistrados,
                            agentesCache,
                            tempData,
                            agentesJson,
                            estado,
                            mlsFallidos,
                            maxRetries: 3,
                            fuerzaBrowserNuevo: false
                        );

                        if (bufferInsert.Count >= 50)
                        {
                            await _context.BulkInsertAsync(bufferInsert);
                            totalInsertados += bufferInsert.Count;
                            bufferInsert.Clear();
                        }
                    }

                    // âœ… 3) Retry final de fallidos (si hay)
                    if (mlsFallidos.Any())
                    {
                        foreach (var mls in mlsFallidos.ToList())
                        {
                            var urlProp = $"https://www.remax.com.py/propiedades/{mls}";

                            bool ok = await ProcesarPropiedad(
                                urlProp,
                                new HashSet<string> { mls },
                                (Browser)browser,
                                pages,
                                USER_AGENT,
                                empresaId,
                                bufferInsert,
                                mlsProcesadosOk,
                                mlsRegistrados,
                                agentesCache,
                                tempData,
                                agentesJson,
                                estado,
                                new HashSet<string>(), // no reacumular
                                maxRetries: 2,
                                fuerzaBrowserNuevo: true
                            );

                            if (ok) mlsFallidos.Remove(mls);

                            if (bufferInsert.Count >= 50)
                            {
                                await _context.BulkInsertAsync(bufferInsert);
                                totalInsertados += bufferInsert.Count;
                                bufferInsert.Clear();
                            }
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException(nameof(urlManual), "Debes pasar una URL de listado o una URL /propiedades/...");
                }

                // Guardado final
                if (bufferInsert.Any())
                {
                    await _context.BulkInsertAsync(bufferInsert);
                    totalInsertados += bufferInsert.Count;
                    bufferInsert.Clear();
                }

                estado.Estado = "COMPLETED";
                estado.TotalInsertados = totalInsertados;
                estado.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new SyncEstadoDto
                {
                    Estado = estado.Estado,
                    Total = totalInsertados
                };
            }
            catch
            {
                estado.Estado = "ERROR";
                estado.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw;
            }
        }

        private async Task EnviarCorreoAlBroker(Propiedad p)
        {
            try
            {
                var fromAddress = new MailAddress("sistema-oima@tuempresa.com", "GESCO Real Estate");
                var toAddress = new MailAddress("broker@tuempresa.com", "Broker GESCO");
                const string fromPassword = "tu_password_aqui";
                string subject = "ðŸš¨ Nueva Captación Pendiente de Aprobación";
                string body = $@"
                    <h3>Hola Bróker,</h3>
                    <p>Se ha generado una nueva captación manual en el sistema:</p>
                    <ul>
                        <li><b>Propiedad:</b> {p.Titulo}</li>
                        <li><b>Agente:</b> {p.AgenteCreadorNombre}</li>
                        <li><b>Ciudad:</b> {p.Ciudad}</li>
                        <li><b>Precio:</b> Gs. {p.PrecioVentaGs:N0}</li>
                    </ul>
                    <p>Por favor, ingresa al panel de administración para revisarla y aprobarla.</p>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com", // Cambiar según tu proveedor
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo enviar el correo: {ex.Message}");
            }
        }

        public async Task<Propiedad?> CrearCaptacionManual(Propiedad propInput)
        {
            try
            {
                // Forzamos los valores de seguridad en el servidor
                propInput.EsAprobada = false;
                propInput.FechaCreacion = DateTime.Now;
                propInput.Sync = false; // Marcamos que no viene de un portal externo

                _context.Propiedades.Add(propInput);
                await _context.SaveChangesAsync();

                // OPCIONAL: Aquí llamarías a una función de envío de mail
                await EnviarCorreoAlBroker(propInput); 

                return propInput;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear captación: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AprobarPropiedad(int propiedadId)
        {
            try
            {
                var propiedad = await _context.Propiedades
                    .FirstOrDefaultAsync(p => p.PropiedadId == propiedadId);

                if (propiedad == null) return false;

                propiedad.EsAprobada = true;
                // Al aprobar, podrías querer actualizar la fecha de creación a la fecha de aprobación
                // propiedad.FechaCreacion = DateTime.Now; 

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al aprobar propiedad: {ex.Message}");
                return false;
            }
        }



        private async Task ProcesarIndividual(
                            string link,
                            List<Page> pages,
                            int pageIndex,
                            int empresaId,
                            List<Propiedad> bufferInsert,
                            HashSet<string> mlsRegistrados,
                            Dictionary<string, int> agentesCache,
                            ConcurrentDictionary<string, PropiedadGescoDto> tempData,
                            ConcurrentDictionary<int, string> agentesJson,
                            SyncEstado estado)
        {
            // 1. Extraemos el MLS que queremos capturar de este link
            var targetMls = link.Split('/').Last();

            int currentIndex = pageIndex % pages.Count;
            pageIndex++;

            try
            {
                var pageActual = pages[currentIndex];
                // ... (lógica de recrear página si es null o closed) ...

                // 2. Navegamos al link
                await pageActual.GoToAsync(link, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 60000 });

                // Esperamos un poco para que el evento de red dispare el llenado de tempData
                await Task.Delay(2500);

                // 3. INTELIGENCIA: Solo sacamos de tempData el objeto que coincide con nuestro targetMls
                // TryRemove saca el dato y lo elimina del diccionario para que no estorbe al siguiente link
                if (tempData.TryRemove(targetMls, out var dto))
                {
                    if (dto.Latitud == 0) return;

                    // Redondeo para comparación precisa
                    /*var lat = Math.Round((double)dto.Latitud, 6);
                    var lon = Math.Round((double)dto.Longitud, 6);
                    var llave = (dto.MLSID, lat, lon);*/

                    // 4. VERIFICAR CONTRA CACHE DE BASE DE DATOS
                    if (!mlsRegistrados.Contains(dto.MLSID))
                    {
                        // Limpieza de datos
                        dto.Descripcion ??= dto.Titulo;
                        string tituloLimpio = dto.Titulo?.Trim() ?? "Sin Título";

                        // (Lógica de Agentes aquí...)
                        // var agenteId = ObtenerOIdentificarAgente(dto, agentesCache, agentesJson...);

                        // 5. AGREGAR AL BUFFER
                        bufferInsert.Add(new Propiedad
                        {
                            EmpresaId = empresaId,
                            MLSID = dto.MLSID,
                            PrecioVentaGs = dto.Precio,
                            Latitud = dto.Latitud,
                            Longitud = dto.Longitud,
                            Ciudad = dto.Ciudad,
                            AgenteId = 923, // O el ID que detectes
                            Titulo = tituloLimpio,
                            DescripcionRemax = dto.Descripcion,
                            Sync = true,
                            FechaCaducidad = DateTime.UtcNow.AddMonths(6)
                        });

                        // Actualizar cache local para no re-insertar lo mismo en esta corrida
                        mlsRegistrados.Add(dto.MLSID);
                        estado.UltimoMLSIDProcesado = dto.MLSID;

                        Console.WriteLine($"âœ¨ Capturado con éxito: {dto.MLSID}");
                    }
                    else
                    {
                        Console.WriteLine($"â­ï¸ Saltando: {targetMls} ya existe en BD con esa ubicación.");
                    }
                }
                else
                {
                    // Si entra aquí, es que la página cargó pero el JSON de Remax no se disparó o no llegó a tempData
                    Console.WriteLine($"âš ï¸ No se pudo capturar el JSON técnico para MLS: {targetMls}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âŒ Error procesando {targetMls}: {ex.Message}");
                estado.Estado = "RUNNING_WITH_ERRORS";
                estado.UltimoMLSIDProcesado = $"ERROR:{targetMls}";
                await _context.SaveChangesAsync();
            }
        }

        // Una pequeña ayuda extra para limpiar los strings que vienen de la web
        private decimal LimpiarTextoANumero(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return 0;
            // Quitamos todo lo que no sea número
            string limpio = new string(texto.Where(c => char.IsDigit(c)).ToArray());
            return decimal.TryParse(limpio, out decimal res) ? res : 0;
        }

        public async Task<int> GuardarPropiedad(Propiedad propiedad)
        {
            _context.Propiedades.Add(propiedad);
            await _context.SaveChangesAsync();
            return propiedad.PropiedadId;
        }

        public async Task<List<object>> ObtenerPropiedades(int empresaId)
        {
            /*return await _context.Propiedades
                .Include(p => p.Agente) // Para ver quién captó
                .Where(p => p.EmpresaId == empresaId)
                .OrderByDescending(p => p.PropiedadId)
                .ToListAsync();*/

            // Aumentamos el tiempo de espera por si la base de datos está lenta
            //_context.Database.SetCommandTimeout(120);
            _context.Database.SetCommandTimeout(180);
            //int pagina = 1; int cantidad = 50;
            /*return await _context.Propiedades
                .Include(p => p.Agente) // El INNER JOIN que se ve en el log
                .Where(p => p.EmpresaId == empresaId && p.Sync == true)
                .AsNoTracking() // ðŸ”¥ Crucial: Mejora la velocidad y evita bloqueos
                .OrderByDescending(p => p.PropiedadId)
                .ToListAsync();*/
            return await _context.Propiedades
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .OrderByDescending(p => p.PropiedadId)
                .Select(p => new {
                    p.PropiedadId,
                    p.MLSID,
                    // Usamos .Trim() para eliminar esos espacios basura que se ven en Postman
                    Titulo = p.Titulo.Trim(),
                    p.PrecioVentaGs,
                    p.Ciudad,
                    p.Latitud,
                    p.Longitud,
                    p.FechaFirmaContrato,
                    p.FechaCaducidad,
                    p.EsAprobada,
                    // Traemos solo el nombre del agente, no todo el objeto agente
                    AgenteNombre = p.Agente.RazonSocial,
                    // OPCIONAL: No envíes la descripción si es para una lista
                    // Descripcion = p.DescripcionRemax 
                })
                .ToListAsync()
                .ContinueWith(t => t.Result.Cast<object>().ToList());

        }

        public async Task<string> ObtenerSiguienteNroFactura(int empresaId)
        {
            // 1. Buscamos la última factura
            var ultimaFactura = await _context.VentaCabeceras
                .Where(v => v.EmpresaId == empresaId)
                .OrderByDescending(v => v.VentaCabeceraId)
                .FirstOrDefaultAsync();

            // Si no hay nada, valor inicial por defecto
            if (ultimaFactura == null) return "001-001-0000001";

            try
            {
                string[] partes = ultimaFactura.NroFactura.Split('-');

                // Validamos que tenga el formato paraguayo XXX-XXX-XXXXXXX
                if (partes.Length != 3)
                {
                    throw new Exception("Formato inválido detectado."); // Forzamos ir al catch
                }

                string establecimiento = partes[0];
                string puntoExpedicion = partes[1];
                string secuencialActual = partes[2];

                // 2. Lógica Alfanumérica: Incrementamos el string (Ej: 00000A1 -> 00000A2)
                string nuevoSecuencial = IncrementarAlfanumerico(secuencialActual);

                // Validamos que el nuevo secuencial no exceda los 7 caracteres permitidos en la factura
                if (nuevoSecuencial.Length > 7)
                {
                    throw new Exception("Límite de numeración alcanzado.");
                }

                return $"{establecimiento}-{puntoExpedicion}-{nuevoSecuencial.PadLeft(7, '0')}";
            }
            catch (Exception ex)
            {
                // Si algo falla (formato raro, letras extrañas, etc.), reiniciamos o logueamos
                Console.WriteLine($"Error en secuencia: {ex.Message}");
                return "001-001-0000001"; // Retorno de seguridad
            }
        }

        public async Task<int> MigrarFacturasDesdeExcel(Stream archivoExcel, int empresaId)
        {
            using var workbook = new XLWorkbook(archivoExcel);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RowsUsed().Skip(1);

            // 1. Leemos todo el Excel y lo pasamos a una lista temporal en memoria
            var listaTemporal = rows.Select(r => new
            {
                NroFactura = r.Cell(1).GetValue<string>(),
                Fecha = r.Cell(2).GetValue<DateTime>(),
                RucCliente = r.Cell(3).GetValue<string>(),
                Descripcion = r.Cell(4).GetValue<string>(),
                Cantidad = r.Cell(5).GetValue<decimal>(),
                PrecioUnitario = r.Cell(6).GetValue<decimal>(),
                Iva = r.Cell(7).GetValue<int>()
            }).ToList();

            // 2. AGRUPAMOS por Nro de Factura (Esto es el "Boom")
            var facturasAgrupadas = listaTemporal.GroupBy(f => f.NroFactura);

            int facturasCreadas = 0;

            foreach (var grupo in facturasAgrupadas)
            {
                var nroFactura = grupo.Key;

                // Verificar si la factura ya existe para no duplicar
                if (await _context.VentaCabeceras.AnyAsync(v => v.NroFactura == nroFactura && v.EmpresaId == empresaId))
                    continue;

                // Buscar el ID del cliente por el RUC que viene en el Excel
                var ruc = grupo.First().RucCliente;
                var cliente = await _context.Personas.FirstOrDefaultAsync(p => p.Documento == ruc && p.EmpresaId == empresaId);

                if (cliente == null) continue; // O podrías crear un cliente genérico

                // 3. Crear la CABECERA (Una sola vez por número de factura)
                var cabecera = new VentaCabecera
                {
                    EmpresaId = empresaId,
                    PersonaId = cliente.PersonaId,
                    NroFactura = nroFactura,
                    Fecha = grupo.First().Fecha,
                    Total = grupo.Sum(x => x.Cantidad * x.PrecioUnitario), // Suma de todos los detalles
                    Detalles = new List<VentaDetalle>()
                };

                // 4. Crear los DETALLES (Uno por cada fila del grupo)
                foreach (var item in grupo)
                {
                    var detalle = new VentaDetalle
                    {
                        Descripcion = item.Descripcion,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Subtotal = item.Cantidad * item.PrecioUnitario,
                        IvaPorcentaje = item.Iva
                    };
                    cabecera.Detalles.Add(detalle);
                }

                _context.VentaCabeceras.Add(cabecera);
                facturasCreadas++;
            }

            await _context.SaveChangesAsync();
            return facturasCreadas;
        }

        public async Task<List<AnalisisMercado>> ObtenerAnalisisMercado(int empresaId)
        {
            return await _context.AnalisisMercados
                .Include(a => a.InmuebleReferencias) // Traemos las comparativas
                .Where(a => a.EmpresaId == empresaId)
                .ToListAsync();
        }

        public async Task<List<InmuebleReferencia>> ObtenerReferenciasPorAnalisis(int analisisId)
        {
            return await _context.InmuebleReferencia
                .Where(r => r.AnalisisMercadoId == analisisId)
                .OrderByDescending(r => r.PrecioVentaGs)
                .ToListAsync();
        }

        public async Task<int> CrearAnalisisManual(AnalisisMercado input)
        {
            // Aseguramos que la fecha de carga sea la actual
            input.FechaCarga = DateTime.Now;

            _context.AnalisisMercados.Add(input);
            await _context.SaveChangesAsync();

            return input.AnalisisMercadoId;
        }

        public async Task<bool> ActualizarAnalisis(int id, AnalisisMercado input)
        {
            var existente = await _context.AnalisisMercados
                .Include(a => a.InmuebleReferencias)
                .FirstOrDefaultAsync(a => a.AnalisisMercadoId == id);

            if (existente == null) return false;

            // Actualizar campos base
            existente.NombrePropiedad = input.NombrePropiedad;
            existente.M2Terreno = input.M2Terreno;
            existente.M2Construccion = input.M2Construccion;
            existente.PrecioM2Terreno = input.PrecioM2Terreno;
            existente.PrecioM2Construccion = input.PrecioM2Construccion;
            existente.TipoCambio = input.TipoCambio;

            // Actualizar cálculos (se vuelven a guardar por seguridad)
            existente.ResumenVentaGs = input.ResumenVentaGs;
            existente.ResumenNetoGs = input.ResumenNetoGs;
            existente.ResumenVentaUsd = input.ResumenVentaUsd;
            existente.ResumenComisionGs = input.ResumenComisionGs;
            existente.ResumenEscribaniaGs = input.ResumenEscribaniaGs;
            existente.ResumenRetencionGs = input.ResumenRetencionGs;
            existente.ResumenNetoUsd = input.ResumenNetoUsd;
            // ... mapear el resto de campos de resumen

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAnalisis(int id)
        {
            try
            {
                // 1. Buscamos el análisis incluyendo sus referencias para asegurar que EF las rastree
                var analisis = await _context.AnalisisMercados
                    .Include(a => a.InmuebleReferencias)
                    .FirstOrDefaultAsync(a => a.AnalisisMercadoId == id);

                if (analisis == null) return false;

                // 2. Removemos. EF se encarga de borrar las referencias en la BD 
                // si la relación tiene habilitado el Cascade Delete.
                _context.AnalisisMercados.Remove(analisis);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando ACM {id}: {ex.Message}");
                return false;
            }
        }

        private string ExtraerNombreDesdeForma(Stream archivoExcel)
        {
            try
            {
                archivoExcel.Position = 0;
                using (var doc = SpreadsheetDocument.Open(archivoExcel, false))
                {
                    var workbookPart = doc.WorkbookPart;
                    var sheet = workbookPart.Workbook.Descendants<Sheet>()
                                .FirstOrDefault(s => s.Name == "PORTADA ACM");

                    if (sheet == null) return "Agente Sin Nombre";

                    var wsPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    if (wsPart.DrawingsPart == null) return "Agente Sin Nombre";

                    // --- CAMBIO CLAVE AQUÍ ---
                    // En lugar de buscar Textos sueltos, buscamos Párrafos (<a:p>)
                    // Un párrafo equivale a una línea completa en el cuadro de texto.
                    var listaLineas = wsPart.DrawingsPart.WorksheetDrawing
                                .Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>()
                                .Select(p => {
                                    // Juntamos todos los fragmentos (<a:t>) que hay dentro de esta línea
                                    return string.Concat(p.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text)).Trim();
                                })
                                .Where(linea => !string.IsNullOrWhiteSpace(linea))
                                .ToList();

                    // Ahora listaLineas tendrá ["KAREN INSFRAN", "Agente Inmobiliario", ...]

                    // Búsqueda por ancla
                    int indiceEtiqueta = listaLineas.FindIndex(t => t.Contains("Agente Inmobiliario", StringComparison.OrdinalIgnoreCase));

                    if (indiceEtiqueta > 0)
                    {
                        string posibleNombre = listaLineas[indiceEtiqueta - 1];
                        if (EsUnNombreValido(posibleNombre)) return posibleNombre;
                    }

                    // Fallback: primer línea que parezca un nombre
                    return listaLineas.FirstOrDefault(t => EsUnNombreValido(t)) ?? "Agente GESCO";
                }
            }
            catch { return "Agente Remax"; }
        }

        // FUNCION DE VALIDACIÓN: El "Cerebro" del filtro
        private bool EsUnNombreValido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;

            // A. Descartar si tiene símbolos de contacto
            if (texto.Contains("@") || texto.Contains("+") || texto.Contains("www.")) return false;

            // B. Descartar si tiene números (Un nombre de agente no tiene números)
            if (texto.Any(char.IsDigit)) return false;

            // C. Descartar palabras clave del sistema/plantilla
            string[] blackList = { "Agente Inmobiliario", "Página", "ACM", "Superficie", "Total", "RE/MAX", "FDM", "ZONA" };
            if (blackList.Any(word => texto.Contains(word, StringComparison.OrdinalIgnoreCase))) return false;

            // D. Un nombre suele tener entre 2 y 4 palabras (Karen Insfran)
            int palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (palabras < 1 || palabras > 5) return false;

            return true;
        }


        public async Task<int> MigrarInmobiliario(Stream archivoExcel, int empresaId)
        {
            string nombreAgente = ExtraerNombreDesdeForma(archivoExcel);
            archivoExcel.Position = 0;
            using var workbook = new XLWorkbook(archivoExcel);
            var sheetResumen = workbook.Worksheet("RESUMEN ACM");

            // Capturamos el Tipo de Cambio primero (Celda H2)
            decimal tc = ParsearDecimalSeguro(sheetResumen.Cell("H2"));
            if (tc == 0) tc = 7070; // Fallback por si la celda está vacía

            // 1. Creamos el objeto PADRE con las celdas CORRECTAS
            var analisis = new AnalisisMercado
            {
                EmpresaId = empresaId,
                AgenteNombre = nombreAgente,
                FechaCarga = DateTime.Now,
                NombrePropiedad = sheetResumen.Cell("B3").GetValue<string>(),
                TipoCambio = ParsearDecimalSeguro(sheetResumen.Cell("H2")),

                // 1. DATOS INMUEBLE (Lo de arriba)
                M2Terreno = ParsearDecimalSeguro(sheetResumen.Cell("B5")),
                M2Construccion = ParsearDecimalSeguro(sheetResumen.Cell("B6")),
                PrecioM2Terreno = ParsearDecimalSeguro(sheetResumen.Cell("C5")),
                PrecioM2Construccion = ParsearDecimalSeguro(sheetResumen.Cell("C6")),
                PrecioM2TerrenoUsd = ParsearDecimalSeguro(sheetResumen.Cell("D5")),
                PrecioM2ConstruccionUsd = ParsearDecimalSeguro(sheetResumen.Cell("D6")),
                // Asumimos que viene en USD y se multiplica por TC

                // 2. RESUMEN FINAL (Captura directa de celdas C14 a D18)
                ResumenVentaGs = ParsearDecimalSeguro(sheetResumen.Cell("C14")),
                ResumenVentaUsd = ParsearDecimalSeguro(sheetResumen.Cell("D14")),

                ResumenComisionGs = ParsearDecimalSeguro(sheetResumen.Cell("C15")),
                ResumenComisionUsd = ParsearDecimalSeguro(sheetResumen.Cell("D15")),

                ResumenEscribaniaGs = ParsearDecimalSeguro(sheetResumen.Cell("C16")),
                ResumenEscribaniaUsd = ParsearDecimalSeguro(sheetResumen.Cell("D16")),

                ResumenRetencionGs = ParsearDecimalSeguro(sheetResumen.Cell("C17")),
                ResumenRetencionUsd = ParsearDecimalSeguro(sheetResumen.Cell("D17")),

                ResumenNetoGs = ParsearDecimalSeguro(sheetResumen.Cell("C18")),
                ResumenNetoUsd = ParsearDecimalSeguro(sheetResumen.Cell("D18"))
            };

            // 2. BUSCAR HOJA DE REFERENCIAS (Se mantiene igual, ya funcionaba)
            var sheetRefs = workbook.Worksheet("REFERENCIAS ACM");
            var celdasPrecio = sheetRefs.CellsUsed(c => c.Value.ToString().Contains("PRECIO VENTA GS.", StringComparison.OrdinalIgnoreCase));

            foreach (var celda in celdasPrecio)
            {
                var filaCabecera = celda.Address.RowNumber;
                var colCabecera = celda.Address.ColumnNumber;

                // El link está una fila arriba (filaCabecera - 1) 
                // y suele estar en la misma columna o una al lado. 
                // Vamos a buscar el hipervínculo en esa celda específica.
                var celdaConLink = sheetRefs.Cell(filaCabecera - 1, colCabecera);

                string urlExtraida = "";

                // MAGIA: Verificamos si la celda tiene un hipervínculo real de Excel
                if (celdaConLink.HasHyperlink)
                {
                    urlExtraida = celdaConLink.GetHyperlink().ExternalAddress.ToString();
                }
                else
                {
                    // Si no tiene hipervínculo, leemos el texto por si pegaron la URL como texto plano
                    urlExtraida = celdaConLink.GetValue<string>();
                }

                var precioGs = ParsearDecimalSeguro(sheetRefs.Cell(filaCabecera + 1, colCabecera));

                if (precioGs > 0)
                {
                    analisis.InmuebleReferencias.Add(new InmuebleReferencia
                    {
                        EmpresaId = empresaId,
                        PrecioVentaGs = precioGs,
                        M2Terreno = ParsearDecimalSeguro(sheetRefs.Cell(filaCabecera + 1, colCabecera + 2)),
                        LinkUrl = urlExtraida // <--- Ahora sí guarda la URL real
                    });
                }
            }

            _context.AnalisisMercados.Add(analisis);
            await _context.SaveChangesAsync();

            //return analisis.InmuebleReferencias.Count + 1;
            return analisis.AnalisisMercadoId;
        }

        public async Task<int> MigrarInteligente(Stream archivoExcel, string nombreArchivo, int empresaId)
        {
            using var workbook = new XLWorkbook(archivoExcel);
            var worksheet = workbook.Worksheet(1);

            // IMPORTANTE: Obtenemos todas las filas
            var todasLasFilas = worksheet.RowsUsed();
            var cabecera = todasLasFilas.FirstOrDefault(); // Fila 1 (Títulos)
            var datos = todasLasFilas.Skip(1);    // Fila 2 en adelante (Valores)

            string fileName = nombreArchivo.ToLower();
            int contador = 0;

            // --- PRIORIDAD 1: TABLAS DEL SISTEMA (Lo que ya funciona) ---

            // CASO PERSONAS
            if (fileName.Contains("persona") || fileName.Contains("cliente") || fileName.Contains("proveedor"))
            {
                var existentes = (await _context.Personas.Where(p => p.EmpresaId == empresaId)
                                                         .Select(p => p.Documento)
                                                         .ToListAsync())
                                                         .ToHashSet();
                foreach (var row in datos)
                {
                    var rucRaw = row.Cell(2).GetValue<string>().Trim();
                    var (rucBase, dv) = SepararRuc(rucRaw);
                    if (!existentes.Contains(rucBase))
                    {
                        _context.Personas.Add(new Persona
                        {
                            EmpresaId = empresaId,
                            RazonSocial = row.Cell(1).GetValue<string>(),
                            Documento = rucBase,
                            DV = dv,
                            EsCliente = true,
                            Telefono = row.Cell(3).GetValue<string>()
                        });
                        existentes.Add(rucBase);
                        contador++;
                    }
                }
            }
            // CASO PRODUCTOS
            else if (fileName.Contains("producto") || fileName.Contains("articulo") || fileName.Contains("stock"))
            {
                var existentes = (await _context.Productos.Where(p => p.EmpresaId == empresaId).Select(p => p.Descripcion.ToLower()).ToListAsync()).ToHashSet();
                foreach (var row in datos)
                {
                    var desc = row.Cell(1).GetValue<string>().Trim();
                    if (!existentes.Contains(desc.ToLower()))
                    {
                        _context.Productos.Add(new Producto
                        {
                            EmpresaId = empresaId,
                            Descripcion = desc,
                            PrecioVenta = row.Cell(2).GetValue<decimal>(),
                            StockActual = row.Cell(3).GetValue<decimal>(),
                            IvaPorcentaje = row.Cell(4).GetValue<int>()
                        });
                        existentes.Add(desc.ToLower());
                        contador++;
                    }
                }
            }
            else if (fileName.Contains("acm") || fileName.Contains("inmueble") || fileName.Contains("remax"))
            {
                return await MigrarInmobiliario(archivoExcel, empresaId);
            }
            // CASO SUCURSALES
            else if (fileName.Contains("sucursal"))
            {
                var existentes = (await _context.Sucursales.Where(s => s.EmpresaId == empresaId).Select(s => s.Nombre.ToLower()).ToListAsync()).ToHashSet();
                foreach (var row in datos)
                {
                    var nom = row.Cell(1).GetValue<string>().Trim();
                    if (!existentes.Contains(nom.ToLower()))
                    {
                        _context.Sucursales.Add(new Sucursal
                        {
                            EmpresaId = empresaId,
                            Nombre = nom,
                            PuntoExpedicion = row.Cell(2).GetValue<string>()
                        });
                        existentes.Add(nom.ToLower());
                        contador++;
                    }
                }
            }

            // --- PRIORIDAD 2: ENTIDAD DESCONOCIDA (Mapeo Dinámico) ---
            else
            {
                // 1. Buscar o crear el perfil para este archivo raro
                var perfil = await _context.MapeoPerfiles.FirstOrDefaultAsync(m => m.NombrePerfil == nombreArchivo);

                if (perfil == null)
                {
                    perfil = new MapeoPerfil { NombrePerfil = nombreArchivo, TablaDestino = "Dinamica" };
                    _context.MapeoPerfiles.Add(perfil);
                    await _context.SaveChangesAsync(); // Guardamos para tener el ID

                    foreach (var cell in cabecera.CellsUsed())
                    {
                        _context.MapeoDetalles.Add(new MapeoDetalle
                        {
                            MapeoPerfilId = perfil.MapeoPerfilId,
                            ColumnaOrigen = cell.Value.ToString(),
                            ColumnaDestino = cell.Value.ToString()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 2. Traer los Hashes existentes para no duplicar datos en esta tabla dinámica
                var hashesExistentes = (await _context.MapeoRegistros
                    .Where(r => r.MapeoPerfilId == perfil.MapeoPerfilId)
                    .Select(r => r.FilaHash).ToListAsync()).ToHashSet();

                var columnas = cabecera.CellsUsed().Select(c => c.Value.ToString()).ToList();

                // 3. Procesar las filas del Excel raro
                foreach (var row in datos)
                {
                    var filaDict = new Dictionary<string, string>();
                    foreach (var col in columnas)
                    {
                        int colIndex = columnas.IndexOf(col) + 1;
                        filaDict[col] = row.Cell(colIndex).GetValue<string>();
                    }

                    string jsonFila = System.Text.Json.JsonSerializer.Serialize(filaDict);
                    string hashFila = CalcularMD5(jsonFila);

                    if (!hashesExistentes.Contains(hashFila))
                    {
                        _context.MapeoRegistros.Add(new MapeoRegistro
                        {
                            MapeoPerfilId = perfil.MapeoPerfilId,
                            DatosJson = jsonFila,
                            FilaHash = hashFila
                        });
                        hashesExistentes.Add(hashFila);
                        contador++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return contador;
        }

        public async Task<VentaCabecera?> ObtenerFacturaCompleta(int ventaId)
        {
            return await _context.VentaCabeceras
                .Include(v => v.Detalles) // Trae los productos de la factura
                .Include(v => v.Cliente)  // Trae los datos del cliente (RUC, Razón Social)
                .AsNoTracking()           // Mejora el rendimiento (es solo lectura)
                .FirstOrDefaultAsync(v => v.VentaCabeceraId == ventaId);
        }

        public async Task<CatalogosDto> ObtenerCatalogos(int empresaId)
        {
            return new CatalogosDto
            {
                Clientes = await _context.Personas
                    .Where(p => p.EmpresaId == empresaId && p.EsCliente)
                    .ToListAsync(),

                Productos = await _context.Productos
                    .Where(p => p.EmpresaId == empresaId)
                    .ToListAsync(),

                Sucursales = await _context.Sucursales
                    .Where(p => p.EmpresaId == empresaId)
                    .ToListAsync()
            };
        }

        public async Task<int> GuardarFacturaManual(VentaCabecera factura)
        {
            // 1. Calculamos los totales en el servidor (Seguridad ante todo)
            decimal totalGeneral = 0;
            decimal totalIva10 = 0;
            decimal totalIva5 = 0;
            decimal totalExenta = 0;

            foreach (var detalle in factura.Detalles)
            {
                // Cálculo del subtotal por ítem
                detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
                totalGeneral += detalle.Subtotal;

                // Cálculo de IVA según tasa paraguaya
                if (detalle.IvaPorcentaje == 10)
                    totalIva10 += detalle.Subtotal / 11;
                else if (detalle.IvaPorcentaje == 5)
                    totalIva5 += detalle.Subtotal / 21;
                else
                    totalExenta += detalle.Subtotal;
            }

            // 2. Asignamos los totales calculados a la cabecera
            factura.Total = totalGeneral;
            // Podés guardar estos campos en columnas separadas si querés reportes rápidos
            // factura.TotalIva10 = totalIva10; 

            // 3. Entity Framework detecta la relación y guarda TODO en una sola transacción
            _context.VentaCabeceras.Add(factura);
            await _context.SaveChangesAsync();

            return factura.VentaCabeceraId;
        }
        public async Task<int> MigrarCondominioExcel(Stream archivoExcel, int empresaId)
        {
            using var workbook = new XLWorkbook(archivoExcel);
            int contadorRegistros = 0;

            // 1. LEER HOJA DE CONDOMINIOS
            if (workbook.TryGetWorksheet("Condominios", out var sheetCondos))
            {
                // La estructura viene como bloques: "Aguz 1", luego filas, luego "Aguz 2"...
                var rows = sheetCondos.RowsUsed();
                string ubicacionActual = "Condominio"; // Por si no encontramos cabecera inicial

                foreach (var row in rows)
                {
                    string colA = row.Cell(1).GetValue<string>()?.Trim();
                    string colB = row.Cell(2).GetValue<string>()?.Trim();

                    if (string.IsNullOrEmpty(colA)) continue;

                    // Detectar cabecera de bloque (e.g., "Aguz 1" seguido de "Duplex" en colB)
                    if (colB != null && colB.Equals("Duplex", StringComparison.OrdinalIgnoreCase))
                    {
                        // Normalizar para evitar que un error de tipeo en Excel (Agus vs Aguz) duplique toda la base
                        ubicacionActual = colA.Replace("Agus", "Aguz").Trim();
                        continue;
                    }

                    // Es una fila de inquilino. ColB es la unidad (e.g. "1")
                    if (!string.IsNullOrEmpty(colB) && int.TryParse(colB, out _))
                    {
                        string nombreInquilino = colA;
                        decimal alquilerPagado = 0;
                        decimal saldoPendiente = 0;

                        // Aplicar Regex para extraer "2800" (monto pagado) o "falta 4200" (deuda)
                        var matchPagado = System.Text.RegularExpressions.Regex.Match(nombreInquilino, @"(\d{4,})");
                        var matchFalta = System.Text.RegularExpressions.Regex.Match(nombreInquilino, @"falta\s*(\d{4,})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        if (matchPagado.Success) 
                        {
                            alquilerPagado = decimal.Parse(matchPagado.Groups[1].Value) * 1000;
                            // Limpiar nombre
                            nombreInquilino = nombreInquilino.Replace(matchPagado.Groups[1].Value, "").Trim();
                        }
                        if (matchFalta.Success)
                        {
                            saldoPendiente = decimal.Parse(matchFalta.Groups[1].Value) * 1000;
                            nombreInquilino = nombreInquilino.Replace(matchFalta.Groups[0].Value, "").Trim();
                        }
                        
                        // Limpiar paréntesis y espacios residuales
                        nombreInquilino = nombreInquilino.Replace("(", "").Replace(")", "").Trim();
                        // Remover dobles espacios para normalizar (ej: "Ana  Rosa" a "Ana Rosa")
                        nombreInquilino = System.Text.RegularExpressions.Regex.Replace(nombreInquilino, @"\s+", " ");

                        // 1. Upsert Persona (Inquilino)
                        var persona = await _context.Personas.FirstOrDefaultAsync(p => p.RazonSocial == nombreInquilino && p.EmpresaId == empresaId);
                        if (persona == null)
                        {
                            persona = new Persona { EmpresaId = empresaId, RazonSocial = nombreInquilino, Documento = "S/D", EsCliente = true };
                            _context.Personas.Add(persona);
                            await _context.SaveChangesAsync();
                        }

                        // 2. Upsert UnidadCondominio (Unidad)
                        string identificadorUnidad = $"D-{colB.Trim().PadLeft(2, '0')}";
                        var unidad = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.CodigoUnidad == identificadorUnidad && u.NombreCondominio == ubicacionActual && u.EmpresaId == empresaId);
                        if (unidad == null)
                        {
                            unidad = new UnidadCondominio 
                            { 
                                EmpresaId = empresaId, 
                                NombreCondominio = ubicacionActual,
                                CodigoUnidad = identificadorUnidad,
                                Estado = "Ocupado"
                            };
                            _context.UnidadesCondominio.Add(unidad);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            unidad.Estado = "Ocupado";
                            _context.UnidadesCondominio.Update(unidad);
                            await _context.SaveChangesAsync();
                        }

                        // 3. Upsert ContratoInquilino
                        var contrato = await _context.ContratosInquilinos.FirstOrDefaultAsync(c => c.InquilinoId == persona.PersonaId && c.UnidadId == unidad.UnidadId);
                        if (contrato == null)
                        {
                            contrato = new ContratoInquilino
                            {
                                UnidadId = unidad.UnidadId,
                                InquilinoId = persona.PersonaId,
                                FechaIngreso = new DateTime(2026, 1, 1),
                                FechaInicioContrato = new DateTime(2026, 1, 1), 
                                FechaFinContrato = new DateTime(2026, 12, 31),
                                MontoMensualAlquiler = alquilerPagado > 0 ? alquilerPagado : 2800000,
                                EstaActivo = true,
                                // Guardando también en los campos en caso que se usen en otro lado
                                PropiedadId = unidad.UnidadId,
                                MontoMensualGs = alquilerPagado > 0 ? alquilerPagado : 2800000,
                                DiaVencimiento = 5,
                                IncluyeAgua = true,
                                Estado = "Activo"
                            };
                            _context.ContratosInquilinos.Add(contrato);
                            await _context.SaveChangesAsync();
                        }

                        // --- IMPORTACIÓN DE PAGOS MENSUALES ---
                        // Limpiamos los pagos anteriores de tipo "Alquiler" para este contrato (evita duplicados si se re-migra)
                        var pagosAntiguos = await _context.PagosRecibos
                            .Where(p => p.ContratoId == contrato.ContratoId && p.TipoPago == "Alquiler")
                            .ToListAsync();
                        _context.PagosRecibos.RemoveRange(pagosAntiguos);
                        await _context.SaveChangesAsync();

                        decimal ultimoMontoValido = contrato.MontoMensualAlquiler;

                        // Recorremos las columnas del mes (Col 3 = Enero a Col 14 = Diciembre)
                        for (int colIndex = 3; colIndex <= 14; colIndex++)
                        {
                            var cell = row.Cell(colIndex);
                            if (cell != null && !cell.IsEmpty())
                            {
                                decimal val = ParsearDecimalSeguro(cell);
                                if (val > 0)
                                {
                                    if (val < 100000) val *= 1000; // Si viene en miles (p.ej. 2800 -> 2800000)

                                    ultimoMontoValido = val;

                                    var nuevoPago = new PagoRecibo
                                    {
                                        ContratoId = contrato.ContratoId,
                                        TipoPago = "Alquiler",
                                        MesPagado = colIndex - 2, // Col 3 -> Mes 1 (Enero)
                                        AnioPagado = 2026,
                                        MontoPagado = val,
                                        FechaPago = new DateTime(2026, colIndex - 2, 5),
                                        MetodoPago = "Transferencia",
                                        SaldoPendiente = 0,
                                        Mora = 0,
                                        FacturaEmitida = false,
                                        Notas = "Migrado desde Excel",
                                        NroComprobante = "MIGRADO"
                                    };
                                    _context.PagosRecibos.Add(nuevoPago);
                                }
                            }
                        }

                        // Actualizar el monto del contrato con el último valor válido encontrado en la planilla
                        contrato.MontoMensualAlquiler = ultimoMontoValido;
                        contrato.MontoMensualGs = ultimoMontoValido;
                        _context.ContratosInquilinos.Update(contrato);
                        await _context.SaveChangesAsync();

                        contadorRegistros++;
                    }
                }
            }

            // 2. LEER HOJA DE NIS (Luz y Deudas separadas)
            if (workbook.TryGetWorksheet("NIS", out var sheetNis))
            {
                var rows = sheetNis.RowsUsed();
                
                // La hoja NIS tiene dos bloques: Aguz 1 (cols 1-4) y Aguz 2 (cols 6-11)
                foreach (var row in rows)
                {
                    // Bloque Aguz 1
                    string colB_A1 = row.Cell(2).GetValue<string>()?.Trim();
                    string colC_A1 = row.Cell(3).GetValue<string>()?.Trim(); // NIS
                    
                    if (!string.IsNullOrEmpty(colB_A1) && int.TryParse(colB_A1, out _) && !string.IsNullOrEmpty(colC_A1) && colC_A1 != "NIS")
                    {
                        string identificadorUnidad = $"D-{colB_A1.Trim().PadLeft(2, '0')}";
                        var unidad = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.CodigoUnidad == identificadorUnidad && u.NombreCondominio == "Aguz 1" && u.EmpresaId == empresaId);
                        if (unidad != null)
                        {
                            unidad.NisAnde = colC_A1;
                            _context.UnidadesCondominio.Update(unidad);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Bloque Aguz 2 (o cualquier otra estructura en columnas derechas)
                    string colG_A2 = row.Cell(7).GetValue<string>()?.Trim(); // Unidad
                    string colH_A2 = row.Cell(8).GetValue<string>()?.Trim(); // NIS
                    string colJ_A2 = row.Cell(10).GetValue<string>()?.Trim(); // Deuda
                    
                    if (!string.IsNullOrEmpty(colG_A2) && int.TryParse(colG_A2, out _) && !string.IsNullOrEmpty(colH_A2) && colH_A2 != "NIS")
                    {
                        string identificadorUnidad = $"D-{colG_A2.Trim().PadLeft(2, '0')}";
                        var unidad = await _context.UnidadesCondominio.FirstOrDefaultAsync(u => u.CodigoUnidad == identificadorUnidad && u.NombreCondominio == "Aguz 2" && u.EmpresaId == empresaId);
                        if (unidad != null)
                        {
                            unidad.NisAnde = colH_A2;
                            _context.UnidadesCondominio.Update(unidad);
                            await _context.SaveChangesAsync();
                            
                            // Logica para deudas (si tiene números grandes)
                            if (!string.IsNullOrEmpty(colJ_A2) && int.TryParse(colJ_A2, out int montoDeuda) && montoDeuda > 0)
                            {
                                // Registramos la expensa adeudada de luz
                                // TODO: Para CondominioExpensas cuando exista el modelo, por ahora lo dejamos trackeado
                                contadorRegistros++;
                            }
                        }
                    }
                }
            }

            return contadorRegistros;
        }
    }
}
