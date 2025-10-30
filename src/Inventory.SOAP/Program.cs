using Inventory.Data;                 // DbConfig, ArticuloRepository
using Inventory.Domain.Contracts;     // IArticuloRepository, IInventarioService
using Inventory.Business;             // InventarioService
using Inventory.Domain.Soap;          // IInventarioSoap (contrato SOAP)
using Inventory.ServiceHost.Soap;     // InventarioSoapService (implementación)
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

// Dapper: mapeo snake_case -> PascalCase
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// DB config (appsettings.json)
var connString = builder.Configuration.GetConnectionString("Pg")!;
builder.Services.AddSingleton(new DbConfig { ConnectionString = connString });

// DI
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<IInventarioService, InventarioService>();

// ===== CORS para el cliente web =====
string[] allowedOrigins = new[] { "http://localhost:8080", "http://127.0.0.1:8080" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// SOAP
builder.Services.AddSoapCore();
builder.Services.AddScoped<IInventarioSoap, InventarioSoapService>();

var app = builder.Build();

app.UseRouting();

// --- Preflight OPTIONS (algunos hosts no lo manejan solos) ---
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsOptions(ctx.Request.Method))
    {
        var origin = ctx.Request.Headers["Origin"].ToString();
        if (Array.Exists(allowedOrigins, o => o.Equals(origin, StringComparison.OrdinalIgnoreCase)))
        {
            ctx.Response.Headers["Access-Control-Allow-Origin"]  = origin;
            ctx.Response.Headers["Vary"]                         = "Origin";
            ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS";
            ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, SOAPAction";
        }
        ctx.Response.StatusCode = StatusCodes.Status200OK;
        await ctx.Response.CompleteAsync();
        return;
    }
    await next();
});

// CORS (debe ir después de UseRouting y antes de UseEndpoints)
app.UseCors("WebClient");

// Añadimos CORS en las respuestas reales (POST SOAP)
app.Use(async (ctx, next) =>
{
    await next();
    var origin = ctx.Request.Headers["Origin"].ToString();
    if (Array.Exists(allowedOrigins, o => o.Equals(origin, StringComparison.OrdinalIgnoreCase)))
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
        ctx.Response.Headers["Vary"] = "Origin";
    }
});

// Endpoint SOAP (XmlSerializer o DataContractSerializer según tu contrato)
app.UseEndpoints(endpoints =>
{
    endpoints.UseSoapEndpoint<IInventarioSoap>(
        "/soap/inventario.asmx",
        new SoapEncoderOptions(),
        SoapSerializer.DataContractSerializer // o XmlSerializer si así lo usabas
    );
});

app.Run();
