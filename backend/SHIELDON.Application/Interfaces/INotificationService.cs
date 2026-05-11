using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Notifications.DTOs;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Interfaces;

public interface INotificationService
{
    // Frontend Queries
    Task<PagedResponse<NotificationResponse>> GetMyNotificationsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken ct);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct);
    Task DeleteAllNotificationsAsync(Guid userId, CancellationToken ct);

    // Internal System Triggers
    /// <summary>
    /// Generates a notification, applies aggregation logic, and optionally fires an email for critical alerts.
    /// </summary>
    Task TriggerNotificationAsync(Guid recipientUserId, string title, string message, string? actionUrl, NotificationType type, Guid? relatedEntityId, bool sendEmail, CancellationToken ct);
}
