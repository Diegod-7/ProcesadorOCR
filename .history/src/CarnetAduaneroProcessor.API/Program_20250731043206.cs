using CarnetAduaneroProcessor.Core.Services;
using CarnetAduaneroProcessor.Infrastructure.Services;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Agregar servicios al contenedor
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

    // Configurar AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    // Registrar servicios
    builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();
    builder.Services.AddScoped<ICarnetAduaneroProcessorService, CarnetAduaneroProcessorService>();
    builder.Services.AddScoped<IDeclaracionIngresoService, DeclaracionIngresoService>();
    builder.Services.AddScoped<IDocumentoRecepcionService, DocumentoRecepcionService>();
    builder.Services.AddScoped<ITactAdcService, TactAdcService>();
    builder.Services.AddScoped<IComprobanteTransaccionService, ComprobanteTransaccionService>();
    builder.Services.AddScoped<IGuiaDespachoService, GuiaDespachoService>();
    builder.Services.AddScoped<ISeleccionAforoService, SeleccionAforoService>();
    builder.Services.AddSingleton<ICarnetAduaneroRepository, CarnetAduaneroRepository>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins != null)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Carnet Aduanero Processor API", 
        Version = "v1",
        Description = "API para procesar PDFs de Carnés Aduaneros y extraer información automáticamente"
    });
    
    // Configurar autenticación por API Key
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key para autenticación"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

    // Rate limiting se puede agregar en futuras versiones

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Carnet Aduanero Processor API v1");
    c.RoutePrefix = string.Empty; // Servir Swagger en la raíz
});

app.UseHttpsRedirection();

// Servir archivos estáticos
app.UseStaticFiles();

// Usar CORS
app.UseCors("AllowSpecificOrigins");

// Rate limiting se puede agregar en futuras versiones

// Middleware de logging de solicitudes
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    
    Log.Information("Iniciando solicitud: {Method} {Path}", 
        context.Request.Method, context.Request.Path);
    
    await next();
    
    var duration = DateTime.UtcNow - startTime;
    Log.Information("Solicitud completada: {Method} {Path} - {StatusCode} en {Duration}ms", 
        context.Request.Method, context.Request.Path, context.Response.StatusCode, duration.TotalMilliseconds);
});

// Middleware de manejo de errores global
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = new
        {
            message = "Ha ocurrido un error interno en el servidor",
            timestamp = DateTime.UtcNow,
            requestId = context.TraceIdentifier
        };
        
        await context.Response.WriteAsJsonAsync(error);
        
        Log.Error("Error no manejado: {RequestId}", context.TraceIdentifier);
    });
});

app.UseAuthorization();

app.MapControllers();

// Endpoint de salud
app.MapGet("/health", () => new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

// Endpoint de información del sistema
app.MapGet("/info", () => new
{
    application = "Carnet Aduanero Processor",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    features = new
    {
        azureFormRecognizer = !string.IsNullOrEmpty(builder.Configuration["AzureFormRecognizer:Endpoint"]),
        database = "SQL Server",
        logging = "Serilog"
    }
});

    // No se necesitan migraciones con almacenamiento en memoria

try
{
    Log.Information("Iniciando aplicación Carnet Aduanero Processor");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal al iniciar la aplicación");
}
finally
{
    Log.CloseAndFlush();
} 