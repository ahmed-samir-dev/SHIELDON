using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Notifications.DTOs;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string? ActionUrl,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt,
    Guid? RelatedEntityId
);
