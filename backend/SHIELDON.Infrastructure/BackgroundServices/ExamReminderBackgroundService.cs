using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.BackgroundServices;

/// <summary>
/// A hosted background service that polls every 15 minutes for published exams.
///
/// Reminder logic:
///   - If exam start date (ScheduledAt) is set >= 24 hours in advance:
///     Sends a 24-hour advance reminder ("starts in 24 hours").
///   - If the scheduling period is less than 24 hours or exam starts immediately:
///     Sends the reminder when the exam becomes active.
///   - Enforces single delivery per student per exam via Notifications tracking.
/// </summary>
public class ExamReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(15);

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
                // Host is shutting down - exit cleanly.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExamReminderBackgroundService cycle.");
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

        var publishedExams = await db.Exams
            .Where(e => e.Status == ExamStatus.Published)
            .Include(e => e.Course)
            .ToListAsync(ct);

        if (!publishedExams.Any())
            return;

        foreach (var exam in publishedExams)
        {
            // Skip expired exams
            if (exam.ScheduledEndAt.HasValue && exam.ScheduledEndAt.Value < now)
                continue;

            bool is24HourReminder = false;
            bool isActiveReminder = false;

            if (exam.ScheduledAt.HasValue)
            {
                var timeUntilStart = exam.ScheduledAt.Value - now;

                if (timeUntilStart > TimeSpan.FromHours(24))
                {
                    // More than 24h away - not time yet
                    continue;
                }
                else if (timeUntilStart > TimeSpan.Zero)
                {
                    // Starts in <= 24h. Check if scheduled period was >= 24h
                    var totalPeriod = exam.ScheduledAt.Value - exam.CreatedAt;
                    if (totalPeriod >= TimeSpan.FromHours(24))
                    {
                        is24HourReminder = true;
                    }
                    else
                    {
                        // Period < 24h: wait until exam is active
                        continue;
                    }
                }
                else
                {
                    // ScheduledAt <= now -> exam is active
                    isActiveReminder = true;
                }
            }
            else
            {
                // No ScheduledAt set -> exam is active
                isActiveReminder = true;
            }

            if (!is24HourReminder && !isActiveReminder)
                continue;

            // Get students who already started or completed an attempt for this exam
            var studentsWithAttempts = await db.ExamAttempts
                .Where(a => a.ExamId == exam.Id)
                .Select(a => a.StudentId)
                .Distinct()
                .ToListAsync(ct);

            // Get students who have already received an UpcomingExamReminder for this exam
            var alreadyNotifiedStudentIds = await db.Notifications
                .Where(n => n.Type == NotificationType.UpcomingExamReminder && n.RelatedEntityId == exam.Id)
                .Select(n => n.UserId)
                .Distinct()
                .ToListAsync(ct);

            var eligibleStudentIds = await db.CourseEnrollments
                .Where(e =>
                    e.CourseId == exam.CourseId &&
                    e.Status == CourseEnrollmentStatus.Approved &&
                    !studentsWithAttempts.Contains(e.StudentId) &&
                    !alreadyNotifiedStudentIds.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .ToListAsync(ct);

            if (!eligibleStudentIds.Any())
                continue;

            string reminderTitle;
            string reminderMessage;

            if (is24HourReminder)
            {
                var scheduledTime = exam.ScheduledAt!.Value.ToString("dd MMM yyyy, HH:mm");
                reminderTitle = $"Exam Reminder: '{exam.Title}'";
                reminderMessage = $"Your exam '{exam.Title}' in '{exam.Course!.Title}' starts in 24 hours ({scheduledTime} UTC). Make sure you are ready!";
            }
            else
            {
                reminderTitle = $"Exam Active: '{exam.Title}'";
                reminderMessage = $"Your exam '{exam.Title}' in '{exam.Course!.Title}' is now active and ready for you to take.";
            }

            _logger.LogInformation(
                "Sending exam reminder ({Type}) for '{ExamTitle}' to {Count} student(s).",
                is24HourReminder ? "24-Hour" : "Active", exam.Title, eligibleStudentIds.Count);

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
