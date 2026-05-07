using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs every 15 seconds to detect disconnected exam sessions.
///
/// A session is considered "Disconnected" when an InProgress attempt has not received
/// a heartbeat for more than 45 seconds. When detected:
///   - A PresenceLog entry of type Disconnected is created.
///   - The attempt itself is NOT force-submitted (that requires tutor manual action).
///
/// This gives the student a chance to reconnect before any enforcement action is taken.
/// The tutor dashboard will highlight disconnected students so the tutor can respond.
/// </summary>
public class HeartbeatMonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HeartbeatMonitorBackgroundService> _logger;

    // How often the background service polls the database
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    // How long without a heartbeat before we mark the session as Disconnected
    private static readonly TimeSpan DisconnectThreshold = TimeSpan.FromSeconds(45);

    public HeartbeatMonitorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HeartbeatMonitorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeartbeatMonitorBackgroundService started. " +
            "Polling every {Interval}s. Disconnect threshold: {Threshold}s.",
            PollingInterval.TotalSeconds, DisconnectThreshold.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollingInterval, stoppingToken);

            try
            {
                await DetectDisconnectedSessionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in HeartbeatMonitorBackgroundService.");
            }
        }
    }

    private async Task DetectDisconnectedSessionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var staleThreshold = now - DisconnectThreshold;

        // Find InProgress attempts that:
        // 1. Have received at least one heartbeat (LastHeartbeatAt is not null)
        // 2. Have NOT received a heartbeat in the last 45 seconds
        // 3. Don't already have a recent Disconnected log (prevent spam)
        var staleAttempts = await db.ExamAttempts
            .Where(a =>
                a.Status == AttemptStatus.InProgress &&
                a.LastHeartbeatAt != null &&
                a.LastHeartbeatAt < staleThreshold)
            .Include(a => a.Exam)
            .AsNoTracking()
            .ToListAsync(ct);

        if (staleAttempts.Count == 0) return;

        // Get attempt IDs that already have a recent Disconnected log
        // to avoid inserting duplicate disconnection events
        var staleAttemptIds = staleAttempts.Select(a => a.Id).ToList();
        var recentDisconnectCutoff = now - DisconnectThreshold;

        var alreadyLoggedIds = await db.PresenceLogs
            .Where(p =>
                staleAttemptIds.Contains(p.AttemptId) &&
                p.EventType == PresenceEventType.Disconnected &&
                p.OccurredAt >= recentDisconnectCutoff)
            .Select(p => p.AttemptId)
            .Distinct()
            .ToListAsync(ct);

        var alreadyLoggedSet = alreadyLoggedIds.ToHashSet();

        var newDisconnections = new List<PresenceLog>();

        foreach (var attempt in staleAttempts)
        {
            // Skip if we already logged a Disconnected event in this polling window
            if (alreadyLoggedSet.Contains(attempt.Id)) continue;

            _logger.LogInformation(
                "Disconnected session detected: AttemptId={AttemptId}, LastHeartbeat={LastHB}",
                attempt.Id, attempt.LastHeartbeatAt);

            newDisconnections.Add(new PresenceLog
            {
                AttemptId  = attempt.Id,
                StudentId  = attempt.StudentId,
                ExamId     = attempt.ExamId,
                CourseId   = attempt.Exam!.CourseId,
                EventType  = PresenceEventType.Disconnected,
                Detail     = $"No heartbeat received for {(int)(now - attempt.LastHeartbeatAt!.Value).TotalSeconds}s.",
                OccurredAt = now,
                CreatedAt  = now
            });
        }

        if (newDisconnections.Count > 0)
        {
            db.PresenceLogs.AddRange(newDisconnections);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "HeartbeatMonitor: Logged {Count} disconnection event(s).",
                newDisconnections.Count);
        }
    }
}
