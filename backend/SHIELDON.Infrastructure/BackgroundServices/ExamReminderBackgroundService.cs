using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.BackgroundServices;

/// <summary>
/// A hosted background service that polls every 15 minutes for exams scheduled
/// to start within the next 3 hours and sends in-app + email reminders to
/// enrolled students who have not yet started an attempt.
///
/// Reminder logic:
///   - Fires when: ScheduledAt is between (now + 2h45m) and (now + 3h15m)
///   - Target: all Approved-enrolled students who have 0 completed attempts
///   - Each student receives both an in-app notification and an email
/// </summary>
public class ExamReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _reminderWindow = TimeSpan.FromHours(3);
    private static readonly TimeSpan _windowBuffer = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExamReminderBackgroundService> _logger;

    public ExamReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExamReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExamReminderBackgroundService started. Polling every {Interval} minutes.", _pollingInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendUpcomingExamRemindersAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down - exit the loop cleanly without crashing.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExamReminderBackgroundService cycle.");
                // Brief back-off before retrying so a transient error doesn't tight-loop.
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SendUpcomingExamRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var windowStart = now + _reminderWindow - _windowBuffer;   // now + 2h45m
        var windowEnd   = now + _reminderWindow + _windowBuffer;   // now + 3h15m

        // Find published exams whose ScheduledAt falls in the 3-hour window
        var upcomingExams = await db.Exams
            .Where(e =>
                e.Status == ExamStatus.Published &&
                e.ScheduledAt.HasValue &&
                e.ScheduledAt.Value >= windowStart &&
                e.ScheduledAt.Value <= windowEnd)
            .Include(e => e.Course)
            .ToListAsync(ct);

        if (!upcomingExams.Any())
            return;

        _logger.LogInformation("Found {Count} exam(s) with upcoming 3-hour reminders.", upcomingExams.Count);

        foreach (var exam in upcomingExams)
        {
            // Get enrolled students who have NOT yet started or completed an attempt
            var studentsWithAttempts = await db.ExamAttempts
                .Where(a => a.ExamId == exam.Id)
                .Select(a => a.StudentId)
                .Distinct()
                .ToListAsync(ct);

            var eligibleStudentIds = await db.CourseEnrollments
                .Where(e =>
                    e.CourseId == exam.CourseId &&
                    e.Status == CourseEnrollmentStatus.Approved &&
                    !studentsWithAttempts.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .ToListAsync(ct);

            if (!eligibleStudentIds.Any())
            {
                _logger.LogDebug("All students have already started exam '{ExamTitle}'. No reminders needed.", exam.Title);
                continue;
            }

            var scheduledTime = exam.ScheduledAt!.Value.ToString("dd MMM yyyy, HH:mm");
            string reminderTitle  = $"⏰ Exam Reminder: '{exam.Title}'";
            string reminderMessage = $"Your exam '{exam.Title}' in '{exam.Course!.Title}' starts in approximately 3 hours ({scheduledTime} UTC). Make sure you are ready!";

            _logger.LogInformation(
                "Sending 3-hour reminder for exam '{ExamTitle}' to {Count} student(s).",
                exam.Title, eligibleStudentIds.Count);

            foreach (var studentId in eligibleStudentIds)
            {
                await notificationService.TriggerNotificationAsync(
                    studentId,
                    reminderTitle,
                    reminderMessage,
                    $"/courses/{exam.CourseId}?tab=exams",
                    NotificationType.UpcomingExamReminder,
                    exam.Id,
                    sendEmail: true,
                    ct);
            }
        }
    }
}
