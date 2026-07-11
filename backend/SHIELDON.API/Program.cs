using SHIELDON.Infrastructure;
using SHIELDON.API.Middleware;
using SHIELDON.API.Hubs;
using SHIELDON.API.BackgroundServices;
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
using System.Text.Json;
using System.Text.Json.Serialization;


// ── SERILOG: Configure bootstrap logger ────────────────────────────────────
// NOTE: Only wraps app.Run() so WebApplicationFactory can propagate startup
// exceptions correctly during integration testing.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/shieldon-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

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

    // ── STRIPE: Initialize global configuration ───────────────────────────
    Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

    // ── API-layer Background Services & Hub Services ──────────────────────
    builder.Services.AddHostedService<AttendanceRotationService>();
    builder.Services.AddScoped<SHIELDON.Application.Interfaces.IDashboardNotificationService, SHIELDON.API.Services.DashboardNotificationService>();

    // ── CONTROLLERS & UTILITIES ──────────────────────────────────────────
    builder.Services.AddControllers(options =>
    {
        // Increase the default multipart body size limit to 100 MB for file uploads
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(105_000_000));
    })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        });
    builder.Services.AddResponseCompression();

    // Allow large file uploads (up to 100 MB) through Kestrel and IIS
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 105_000_000; // 100 MB + overhead
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 105_000_000;
    });


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
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
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
            ClockSkew = TimeSpan.Zero // No tolerance - token expiry is exact
        };

        // SignalR: browsers cannot set Authorization headers on WebSocket connections.
        // The Angular client sends the JWT as a query string parameter.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/dashboard")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireStudent", policy => 
            policy.RequireRole(SHIELDON.Domain.Enums.UserRole.Student.ToString()));

        options.AddPolicy("RequireTutorOrAdmin", policy =>
            policy.RequireRole(
                SHIELDON.Domain.Enums.UserRole.Tutor.ToString(),
                SHIELDON.Domain.Enums.UserRole.Admin.ToString()));
    });

    // ── SWAGGER / OPENAPI: With JWT bearer auth support ────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SHIELDON API",
            Version = "v1",
            Description = "SHIELDON LMS & Anti-Cheating Engine REST API - Integrity You Can Trust",
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

    // ── SIGNALR: Real-time chat ──────────────────────────────────────────────
    builder.Services.AddSignalR();

// ──────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── DATABASE INITIALIZATION & SEEDING ────────────────────────────────
if (app.Environment.EnvironmentName != "Testing")
{
    await SHIELDON.Infrastructure.Persistence.DbInitializer.InitAsync(app.Services);
}

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
        options.DocumentTitle = "SHIELDON API - Swagger UI";
    });
}

// ── CORS: Must be before Auth and Static Files ────────────────────────
app.UseCors("ShieldonCorsPolicy");

// ── STATIC FILES: Serves wwwroot (but files are protected by controllers)
app.UseStaticFiles();

// ── HTTPS REDIRECTION ──────────────────────────────────────────────────
app.UseHttpsRedirection();

// ── RATE LIMITER ───────────────────────────────────────────────────────
app.UseRateLimiter();

// ── AUTHENTICATION & AUTHORIZATION ────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── SERILOG: Log every HTTP request ───────────────────────────────────
app.UseSerilogRequestLogging();

// ── CONTROLLERS ───────────────────────────────────────────────────────
app.MapControllers();

// ── SIGNALR HUBS ──────────────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<AttendanceHub>("/hubs/attendance");
app.MapHub<DashboardHub>("/hubs/dashboard");

// ── BACKGROUND SERVICES (API-layer) ──────────────────────────────────────────
// AttendanceRotationService is registered above via builder.Services

Log.Information("SHIELDON API starting - environment: {Env}", app.Environment.EnvironmentName);

// Serilog try/catch only wraps Run() - not the full build pipeline.
// This lets WebApplicationFactory propagate startup exceptions in integration tests.
try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SHIELDON API crashed at runtime.");
    throw; // Re-throw so the process exits with a non-zero code
}
finally
{
    Log.CloseAndFlush();
}

// Needed so WebApplicationFactory can use this in integration tests
public partial class Program { }

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // When EF Core loads from SQL Server datetime2, Kind is Unspecified. Assume UTC.
        var utcValue = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        // The framework's default ToString("O") or equivalent appending Z
        writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
    }
}
