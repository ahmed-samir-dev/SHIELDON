using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Notifications.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<PagedResponse<NotificationResponse>> GetMyNotificationsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalItems = await query.CountAsync(ct);

        var notifications = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse(
                n.Id,
                n.Title,
                n.Message,
                n.ActionUrl,
                n.Type,
                n.IsRead,
                n.CreatedAt,
                n.RelatedEntityId
            ))
            .ToListAsync(ct);

        return new PagedResponse<NotificationResponse>
        {
            Items = notifications,
            TotalCount = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (unreadNotifications.Any())
        {
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAllNotificationsAsync(Guid userId, CancellationToken ct)
    {
        // ExecuteDeleteAsync is highly optimized for bulk deletes in EF Core 7+
        await _context.Notifications
            .Where(n => n.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task TriggerNotificationAsync(Guid recipientUserId, string title, string message, string? actionUrl, NotificationType type, Guid? relatedEntityId, bool sendEmail, CancellationToken ct)
    {
        // ── 1. Aggregation Logic ────────────────────────────────────────────────────────
        // For events that happen repeatedly (e.g., uploading 3 materials at once), 
        // we check if an UNREAD notification of the same type and target entity exists.
        Notification? existingNotification = null;

        if (relatedEntityId.HasValue && IsAggregableType(type))
        {
            existingNotification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == recipientUserId 
                                          && n.Type == type 
                                          && n.RelatedEntityId == relatedEntityId 
                                          && !n.IsRead, ct);
        }

        if (existingNotification != null)
        {
            // Update existing to a pluralized/aggregated summary
            existingNotification.Title = GetAggregatedTitle(type, title);
            existingNotification.Message = await GetAggregatedMessageAsync(type, message, relatedEntityId, ct);
            existingNotification.CreatedAt = DateTime.UtcNow; // Bump timestamp to top
            // Do not send a second email for an aggregated event
            sendEmail = false; 
        }
        else
        {
            // Insert new 
            var notification = new Notification
            {
                UserId = recipientUserId,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                Type = type,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(ct);

        // ── 2. Email Delivery (for critical events, without sensitive info) ──────────────
        if (sendEmail)
        {
            var user = await _context.Users.FindAsync(new object[] { recipientUserId }, ct);
            if (user != null)
            {
                string frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:4200";
                await _emailService.SendNotificationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", title, message, frontendUrl);
            }
        }
    }

    // ── Aggregation Helpers ──────────────────────────────────────────────────────────

    private bool IsAggregableType(NotificationType type)
    {
        // Define which events can be batched to avoid spam
        return type == NotificationType.NewCourseMaterial 
            || type == NotificationType.NewCourseAssignment
            || type == NotificationType.CourseUpdate
            || type == NotificationType.NewCourseAnnouncement;
    }

    private string GetAggregatedTitle(NotificationType type, string fallbackTitle)
    {
        return type switch
        {
            NotificationType.NewCourseMaterial => "Multiple New Materials",
            NotificationType.NewCourseAssignment => "Multiple New Assignments",
            NotificationType.NewCourseAnnouncement => "Multiple New Announcements",
            NotificationType.CourseUpdate => "Multiple Course Updates",
            _ => fallbackTitle
        };
    }

    private async Task<string> GetAggregatedMessageAsync(NotificationType type, string fallbackMessage, Guid? relatedEntityId, CancellationToken ct)
    {
        string location = "this course";
        if (relatedEntityId.HasValue)
        {
            var course = await _context.Courses.FindAsync(new object[] { relatedEntityId.Value }, ct);
            if (course != null) location = $"'{course.Title}'";
        }

        return type switch
        {
            NotificationType.NewCourseMaterial => $"Multiple materials have been recently uploaded to {location}.",
            NotificationType.NewCourseAssignment => $"Multiple assignments have been published in {location}.",
            NotificationType.NewCourseAnnouncement => $"Multiple announcements have been posted in {location}.",
            NotificationType.CourseUpdate => $"Multiple updates have occurred in {location}.",
            _ => fallbackMessage
        };
    }
}
