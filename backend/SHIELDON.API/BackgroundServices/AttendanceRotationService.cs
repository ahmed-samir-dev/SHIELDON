using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Attendance.DTOs;
using SHIELDON.API.Hubs;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;

namespace SHIELDON.API.BackgroundServices;

/// <summary>
/// Runs every 7 seconds to rotate the QR secret for all active attendance checks.
/// Pushes the new QR payload to the tutor's SignalR group so their screen updates automatically.
/// </summary>
public class AttendanceRotationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AttendanceHub> _hubContext;
    private readonly ILogger<AttendanceRotationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(7);

    public AttendanceRotationService(
        IServiceScopeFactory scopeFactory,
        IHubContext<AttendanceHub> hubContext,
        ILogger<AttendanceRotationService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AttendanceRotationService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                await RotateActiveChecks(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in AttendanceRotationService rotation cycle.");
            }
        }

        _logger.LogInformation("AttendanceRotationService stopped.");
    }

    private async Task RotateActiveChecks(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activeChecks = await db.AttendanceChecks
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        if (activeChecks.Count == 0) return;

        var now = DateTime.UtcNow;

        foreach (var check in activeChecks)
        {
            check.CurrentSecret = AttendanceService.GenerateSecret();
            check.SecretExpiresAt = now.AddSeconds(7);
        }

        await db.SaveChangesAsync(ct);

        // Broadcast new QR payload to each tutor's group
        foreach (var check in activeChecks)
        {
            var payload = $"{check.Id}|{check.CurrentSecret}";
            var dto = new QrUpdatedDto
            {
                CheckId = check.Id,
                Payload = payload,
                ExpiresAt = check.SecretExpiresAt
            };

            await _hubContext.Clients
                .Group($"attendance-tutor-{check.Id}")
                .SendAsync("QrUpdated", dto, ct);
        }
    }
}
