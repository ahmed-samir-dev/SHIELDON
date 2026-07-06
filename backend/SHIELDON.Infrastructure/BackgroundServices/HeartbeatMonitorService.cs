using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.BackgroundServices;

/// <summary>
/// A hosted background service that polls every 30 seconds for exam attempts
/// whose heartbeat has gone silent for more than 90 seconds.
///
/// When a gap is detected:
///   - Sets ExamAttempt.IsDisconnected = true
///   - Inserts a PresenceLog row with EventType = Disconnected
///
/// When the student's next heartbeat arrives (via MonitoringService.ProcessHeartbeatAsync),
/// IsDisconnected is reset to false and a Reconnected event is logged.
/// </summary>
public class HeartbeatMonitorService : BackgroundService
{
    private static readonly TimeSpan PollingInterval      = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DisconnectThreshold  = TimeSpan.FromSeconds(90);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HeartbeatMonitorService> _logger;

    public HeartbeatMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<HeartbeatMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "HeartbeatMonitorService started. Polling every {Interval}s, disconnect threshold {Threshold}s.",
            PollingInterval.TotalSeconds, DisconnectThreshold.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
                await SweepDisconnectedAttemptsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down - exit cleanly.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HeartbeatMonitorService sweep cycle.");
                // Brief back-off before retrying so a transient error doesn't tight-loop.
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("HeartbeatMonitorService stopped.");
    }

    private async Task SweepDisconnectedAttemptsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var staleThreshold = now - DisconnectThreshold;

        // Find InProgress attempts where:
        //   1. A heartbeat was received at some point (LastHeartbeatAt is not null), AND
        //   2. The last heartbeat is older than 90 seconds, AND
        //   3. The attempt is NOT already flagged as disconnected (avoid duplicate logs)
        var staleAttempts = await db.ExamAttempts
            .Where(a =>
                a.Status == AttemptStatus.InProgress &&
                a.LastHeartbeatAt != null &&
                a.LastHeartbeatAt < staleThreshold &&
                !a.IsDisconnected)
            .ToListAsync(ct);

        if (staleAttempts.Count == 0)
            return;

        _logger.LogInformation("HeartbeatMonitor: flagging {Count} attempt(s) as disconnected.", staleAttempts.Count);

        foreach (var attempt in staleAttempts)
        {
            attempt.IsDisconnected = true;

            db.PresenceLogs.Add(new PresenceLog
            {
                AttemptId  = attempt.Id,
                StudentId  = attempt.StudentId,
                ExamId     = attempt.ExamId,
                EventType  = PresenceEventType.Disconnected,
                OccurredAt = now
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
