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
                string htmlBody = $@"
                    <div style='font-family: ""Inter"", ""Helvetica Neue"", Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 0; background-color: #F9FAFB; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #E5E7EB;'>
                        <div style='background-color: #215DAE; padding: 30px 20px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;'>SHIELDON</h1>
                            <p style='color: #DBEAFE; margin: 5px 0 0 0; font-size: 14px;'>Integrity You Can Trust</p>
                        </div>
                        <div style='padding: 40px 30px; background-color: #ffffff;'>
                            <h2 style='color: #111827; margin: 0 0 15px 0; font-size: 20px; font-weight: 600;'>{title}</h2>
                            <p style='color: #4B5563; line-height: 1.6; margin: 0 0 25px 0; font-size: 16px;'>{message}</p>
                            <div style='text-align: center; margin: 35px 0 20px 0;'>
                                <a href='{frontendUrl}' style='background-color: #215DAE; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; display: inline-block; transition: background-color 0.2s;'>View Dashboard</a>
                            </div>
                        </div>
                        <div style='background-color: #F3F4F6; padding: 20px; text-align: center; color: #6B7280; font-size: 13px; border-top: 1px solid #E5E7EB;'>
                            <p style='margin: 0;'>This is an automated system notification from SHIELDON LMS.</p>
                            <p style='margin: 5px 0 0 0;'>Please do not reply directly to this email.</p>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", $"SHIELDON: {title}", htmlBody);
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
