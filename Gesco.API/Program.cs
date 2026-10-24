using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Gesco.Application.Interfaces;
using Gesco.Application.Services;
using Gesco.Infrastructure.Persistence;
using Gesco.API.Services;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = (builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? Array.Empty<string>())
    .Concat(new[]
    {
        "http://localhost:3000",
        "https://localhost:3000",
        "https://vordia.duckdns.org",
        "http://vordia.duckdns.org",
        "https://vordia.com.py",
        "https://www.vordia.com.py",
    })
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("GescoPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Límite global de uploads (~25 MB) — mitigación DoS
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 25 * 1024 * 1024;
    o.ValueLengthLimit = 25 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 25 * 1024 * 1024;
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "Vordia_Dev_Only_Change_In_Production_Min_32_Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Vordia";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VordiaWeb";

builder.Services.AddSingleton<ISessionEpochService, SessionEpochService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var epochSvc = context.HttpContext.RequestServices.GetRequiredService<ISessionEpochService>();
                var sid = context.Principal?.FindFirst(JwtTokenService.SessionEpochClaim)?.Value;
                if (string.IsNullOrEmpty(sid) || !string.Equals(sid, epochSvc.Epoch, StringComparison.Ordinal))
                {
                    context.Fail("Sesión invalidada por actualización del sistema. Inicie sesión nuevamente.");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("leads", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    options.AddPolicy("uploads", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers(options =>
{
    // Por defecto toda la API exige JWT; endpoints públicos usan [AllowAnonymous]
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    // Cada usuario solo opera sobre su empresa (salvo el equipo operador de VORDIA)
    options.Filters.Add<Gesco.API.Services.EmpresaScopeFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vordia / Gesco API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IMigrationService, Gesco.Application.Services.MigrationService>();
builder.Services.AddScoped<ICloudExtractionService, Gesco.Application.Services.CloudExtractionService>();
builder.Services.AddHttpClient<IAiMappingService, Gesco.Application.Services.AiMappingService>();

builder.Services.AddScoped<IPaymentGatewayService, Gesco.Application.Services.StripePaymentService>();
builder.Services.AddScoped<IPaymentGatewayService, Gesco.Application.Services.BancardPaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<Gesco.API.Services.IEmpresaScope, Gesco.API.Services.EmpresaScope>();
builder.Services.AddScoped<Gesco.API.Services.EmpresaScopeFilter>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IHabitacionService, HabitacionService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IStayService, StayService>();
builder.Services.AddScoped<IAgroService, AgroService>();
builder.Services.AddScoped<IMercadoVentasService, MercadoVentasService>();
// Secuencias de correo con redacción por IA gratuita sin tarjeta (Groq y OpenRouter) y envío en horario hábil
builder.Services.AddHttpClient<IRedaccionIaService, RedaccionIaService>(c => c.Timeout = TimeSpan.FromSeconds(40));
builder.Services.AddScoped<ISecuenciasService, SecuenciasService>();
builder.Services.AddHostedService<Gesco.API.BackgroundServices.SecuenciasBackgroundService>();
builder.Services.AddHttpClient<IMercadoService, MercadoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("VordiaHQ-MercadoRadar/1.0 (+https://vordia.com.py)");
});
builder.Services.AddScoped<IUnidadCondominioService, UnidadCondominioService>();
builder.Services.AddScoped<IContratoInquilinoService, ContratoInquilinoService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IGastoMantenimientoService, GastoMantenimientoService>();
builder.Services.AddScoped<IConfiguracionTimbradoService, ConfiguracionTimbradoService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<ICajaPosService, CajaPosService>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<ITesoreriaService, TesoreriaService>();
builder.Services.AddScoped<IMorosidadService, MorosidadService>();
builder.Services.AddScoped<DataIntelligenceService>();
builder.Services.AddScoped<IActividadService, ActividadService>();
builder.Services.AddScoped<ICrmService, CrmService>();
builder.Services.AddScoped<ICrmFinanzasService, CrmFinanzasService>();
builder.Services.AddScoped<ICrmOperacionService, CrmOperacionService>();
builder.Services.AddScoped<ICrmDocumentosService, CrmDocumentosService>();
builder.Services.AddScoped<ICrmAutomatizacionesService, CrmAutomatizacionesService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IAndeCredentialsStore, AndeCredentialsStore>();
builder.Services.AddHttpClient<IAndeScrapingService, AndeScrapingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient<IAndeJwtConsultaService, AndeJwtConsultaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddHostedService<Gesco.API.BackgroundServices.AlquilerAlertasBackgroundService>();
builder.Services.AddHostedService<Gesco.API.BackgroundServices.CrmAutomatizacionesBackgroundService>();
builder.Services.AddHostedService<Gesco.API.BackgroundServices.MercadoRadarBackgroundService>();

builder.Services.AddDbContext<GescoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<GescoDbContext>());

// Detrás de Nginx: la IP real del visitante llega en X-Forwarded-For. Solo se confía en proxies de redes
// privadas (Docker); un pedido directo desde internet no puede falsear su IP para saltar los límites.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("127.0.0.0"), 8));
    // Kestrel en Docker escucha en IPv6: las IPv4 privadas pueden llegar como ::ffff:x.x.x.x
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("::ffff:10.0.0.0"), 104));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("::ffff:172.16.0.0"), 108));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("::ffff:192.168.0.0"), 112));
    o.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse("::ffff:127.0.0.0"), 104));
    o.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseCors("GescoPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
