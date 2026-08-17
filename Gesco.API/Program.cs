using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Gesco.Application.Interfaces;
using Gesco.Application.Services;
using Gesco.Infrastructure.Persistence;
using System.IO.Compression;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
    options.AddPolicy("GescoPolicy", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
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

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Esta línea corta los bucles infinitos automáticamente
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IMigrationService, Gesco.Application.Services.MigrationService>();
builder.Services.AddScoped<ICloudExtractionService, Gesco.Application.Services.CloudExtractionService>();
builder.Services.AddHttpClient<IAiMappingService, Gesco.Application.Services.AiMappingService>();

// Pagos SaaS (Strategy Pattern)
builder.Services.AddScoped<IPaymentGatewayService, Gesco.Application.Services.StripePaymentService>();
builder.Services.AddScoped<IPaymentGatewayService, Gesco.Application.Services.BancardPaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IHabitacionService, HabitacionService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
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
// AndeSyncBackgroundService desactivado: Chrome/ANDE solo al consultar saldo desde Gesco (bajo demanda).
builder.Services.AddHostedService<Gesco.API.BackgroundServices.AlquilerAlertasBackgroundService>();

builder.Services.AddDbContext<GescoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<GescoDbContext>());


var app = builder.Build();

app.UseCors("GescoPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
