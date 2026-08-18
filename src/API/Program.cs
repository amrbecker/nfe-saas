using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NfeSaas.API.Middleware;
using NfeSaas.API.Workers;
using NfeSaas.Application;
using NfeSaas.Infrastructure;
using NfeSaas.Infrastructure.Data;
using Serilog;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/nfesaas-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Sentry:Dsn vazio (default em dev/local) = SDK inativo, sem custo. Configure via env var
// Sentry__Dsn no host de produção para ativar captura de exceções e tracing.
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = 0.1;
});

// Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// JWT Auth — falha cedo se o segredo não foi configurado ou ainda é placeholder.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 ||
    jwtSecret.Contains("SUA_CHAVE", StringComparison.OrdinalIgnoreCase) ||
    jwtSecret.Contains("__TROCAR", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Configuração inválida: 'Jwt:Secret' precisa ter no mínimo 32 caracteres e não pode ser o placeholder. " +
        "Configure via variável de ambiente Jwt__Secret (ver .env.example).");
}

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException(
        "Configuração inválida: 'ConnectionStrings:DefaultConnection' não pode estar vazio. " +
        "Configure via ConnectionStrings__DefaultConnection (ver .env.example).");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowWebUI", policy =>
    {
        policy.WithOrigins(builder.Configuration["WebUI:BaseUrl"] ?? "http://localhost:5002")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NfeSaas API",
        Version = "v1",
        Description = "API para emissão de NF-e e NFC-e"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insira: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// Worker de atualização semanal da tabela NCM (configurado via seção `Ncm`).
builder.Services.Configure<NcmUpdateWorkerOptions>(builder.Configuration.GetSection("Ncm"));
builder.Services.AddHostedService<NcmUpdateWorker>();

// Rate limiting — política restritiva para os endpoints de autenticação (login/refresh), que são
// o alvo natural de força bruta. Particiona por IP do cliente. Limite alto no ambiente "Testing"
// (WebApplicationFactory/BDD): todas as requisições do TestServer compartilham o mesmo IP
// sintético, então o limite de produção derrubaria os próprios testes com 429.
var authPermitLimit = builder.Environment.IsEnvironment("Testing") ? 100_000 : 10;
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Cabeçalhos de proxy reverso (X-Forwarded-For/Proto) — necessário para IP real do cliente
// (rate limiting, logs) e esquema correto (https) quando atrás de nginx/load balancer, conforme
// docs/README.md "HTTPS / TLS obrigatório". KnownProxies/KnownNetworks ficam vazios por padrão
// (ASP.NET Core só confia em loopback) — configure-os para o IP real do proxy antes de publicar
// atrás de um reverse proxy que não seja no mesmo host.
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Handler global de exceções — formata ValidationException/erros inesperados como JSON
// estruturado em vez do corpo vazio padrão do ASP.NET Core em produção.
app.UseExceptionHandling();

// Migrations no boot: conveniente para single-instance (dev, docker-compose local), mas racy
// com múltiplas réplicas. Desligado por padrão em Production — restart.sh já aplica migrations
// via job idempotente antes de subir a API (ver docs/README.md "Migrations"). Forçar via
// Database__MigrateOnStartup=true se necessário para um deploy single-instance.
var migrateOnStartup = builder.Configuration.GetValue<bool?>("Database:MigrateOnStartup")
    ?? !app.Environment.IsProduction();
if (migrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NfeDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NfeSaas API v1"));
}

app.UseSerilogRequestLogging();
app.UseCors("AllowWebUI");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
