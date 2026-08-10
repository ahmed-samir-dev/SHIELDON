using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Common;
using SHIELDON.Domain.Entities;
using SHIELDON.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace SHIELDON.Infrastructure.Services;

public class UserActivityLogger : IUserActivityLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserActivityLogger> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public UserActivityLogger(
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserActivityLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid? userId,
        string category,
        string action,
        string description,
        string? entityId = null,
        string? entityType = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // 1. Resolve User ID if null
            if (!userId.HasValue && httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var nameIdentifier = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
                if (Guid.TryParse(nameIdentifier, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            // 2. Resolve User Email and Role
            var userEmail = httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? httpContext?.User?.FindFirst("email")?.Value;
            var userRole = httpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext?.User?.FindFirst("role")?.Value;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Fallback: If claims are missing (e.g. unauthenticated login/register endpoints), lookup User from DB
            if ((string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(userRole)) && userId.HasValue)
            {
                var dbUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                if (dbUser is not null)
                {
                    userEmail ??= dbUser.Email;
                    userRole ??= dbUser.Role.ToString();
                }
            }

            // 3. Resolve Client IP and User-Agent
            string? ipAddress = null;
            string? userAgent = null;

            if (httpContext is not null)
            {
                ipAddress = httpContext.Request.Headers["X-Forwarded-For"].ToString();
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                }

                if (string.Equals(ipAddress, "::1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ipAddress, "::ffff:127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    ipAddress = "127.0.0.1";
                }
                else if (ipAddress != null && ipAddress.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
                {
                    ipAddress = ipAddress[7..];
                }

                userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (userAgent?.Length > 500)
                {
                    userAgent = userAgent[..500];
                }
            }

            // 4. Serialize Metadata (Auto-populate HTTP request metadata if none provided)
            var metadataObj = metadata;
            if (metadataObj is null && httpContext is not null)
            {
                metadataObj = new
                {
                    Method = httpContext.Request.Method,
                    Path = httpContext.Request.Path.Value
                };
            }

            string? metadataJson = null;
            if (metadataObj is not null)
            {
                try
                {
                    metadataJson = metadataObj is string str ? str : JsonSerializer.Serialize(metadataObj, _jsonOptions);
                }
                catch
                {
                    metadataJson = metadataObj.ToString();
                }
            }

            // 5. Construct Log Entity
            var activityLog = new UserActivityLog
            {
                UserId = userId,
                Category = string.IsNullOrWhiteSpace(category) ? "SYSTEM" : category.ToUpperInvariant(),
                Action = action,
                EventType = action, // Legacy compatibility
                UserEmail = userEmail,
                UserRole = userRole,
                Description = description,
                EntityId = entityId,
                EntityType = entityType,
                MetadataJson = metadataJson,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            db.UserActivityLogs.Add(activityLog);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record UserActivityLog for action: {Action}", action);
        }
    }
}
