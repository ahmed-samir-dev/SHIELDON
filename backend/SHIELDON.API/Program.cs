using SHIELDON.Infrastructure;
using SHIELDON.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

// ── SERILOG: Configure early so startup errors are also logged ──────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/shieldon-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── SERILOG: Replace default Microsoft logging with Serilog ────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/shieldon-.log", rollingInterval: RollingInterval.Day));

    // ── INFRASTRUCTURE: Register all services (DB, Email, JWT, Files, etc.) ─
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── CONTROLLERS & UTILITIES ──────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddResponseCompression();


    // ── FLUENTVALIDATION: Auto-register all validators from Application layer
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssembly(
        Assembly.Load("SHIELDON.Application"),
        includeInternalTypes: true);

    // ── CORS: Allow Angular dev server ─────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ShieldonCorsPolicy", policy =>
        {
            policy
                .WithOrigins(
                    builder.Configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ── JWT AUTHENTICATION ──────────────────────────────────────────────────
    var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
        ?? throw new InvalidOperationException(
            "JwtSettings:SecretKey is not configured. " +
            "Add it to appsettings.Development.json or User Secrets.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SHIELDON",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "SHIELDON-Users",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No tolerance — token expiry is exact
        };
    });

    builder.Services.AddAuthorization();

    // ── SWAGGER / OPENAPI: With JWT bearer auth support ────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SHIELDON API",
            Version = "v1",
            Description = "SHIELDON LMS & Anti-Cheating Engine REST API — Integrity You Can Trust",
            Contact = new OpenApiContact { Name = "SHIELDON Team" }
        });

        // Add JWT auth button to Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Enter your JWT token. Example: Bearer eyJhbGci...",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── RATE LIMITING: Protect against brute-force and abuse ───────────────
    builder.Services.AddRateLimiter(options =>
    {
        // Login endpoint: max 10 attempts per minute per IP
        options.AddFixedWindowLimiter("login", o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueLimit = 0;
        });

        // General API: 100 requests per minute
        options.AddFixedWindowLimiter("api", o =>
        {
            o.PermitLimit = 100;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueLimit = 5;
        });

        // Violation reporting: max 30 per minute (anti-cheat flooding protection)
        options.AddFixedWindowLimiter("violations", o =>
        {
            o.PermitLimit = 30;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueLimit = 0;
        });

        options.RejectionStatusCode = 429; // Too Many Requests
    });

    // ──────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── DATABASE INITIALIZATION & SEEDING ────────────────────────────────
    await SHIELDON.Infrastructure.Persistence.DbInitializer.InitAsync(app.Services);

    // ──────────────────────────────────────────────────────────────────────

    // ── GLOBAL EXCEPTION HANDLER: Must be FIRST in pipeline ────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // ── RESPONSE COMPRESSION ───────────────────────────────────────────────
    app.UseResponseCompression();

    // ── SWAGGER: Only in development ──────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SHIELDON API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "SHIELDON API — Swagger UI";
        });
    }

    // ── STATIC FILES: Serves wwwroot (but files are protected by controllers)
    app.UseStaticFiles();

    // ── HTTPS REDIRECTION ──────────────────────────────────────────────────
    app.UseHttpsRedirection();

    // ── CORS: Must be before Auth ──────────────────────────────────────────
    app.UseCors("ShieldonCorsPolicy");

    // ── RATE LIMITER ───────────────────────────────────────────────────────
    app.UseRateLimiter();

    // ── AUTHENTICATION & AUTHORIZATION ────────────────────────────────────
    app.UseAuthentication();
    app.UseAuthorization();

    // ── SERILOG: Log every HTTP request ───────────────────────────────────
    app.UseSerilogRequestLogging();

    // ── CONTROLLERS ───────────────────────────────────────────────────────
    app.MapControllers();

    Log.Information("SHIELDON API starting — environment: {Env}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SHIELDON API failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

// Needed so WebApplicationFactory can use this in integration tests
public partial class Program { }
