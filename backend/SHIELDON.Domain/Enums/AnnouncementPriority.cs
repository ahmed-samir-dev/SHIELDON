namespace SHIELDON.Domain.Enums;

/// <summary>
/// Controls display order and notification delivery urgency for announcements.
/// </summary>
public enum AnnouncementPriority
{
    /// <summary>Standard announcement - shown in date-descending order.</summary>
    Normal = 0,

    /// <summary>
    /// Pinned at the top of the list and visually highlighted.
    /// Bypasses notification aggregation for immediate delivery.
    /// </summary>
    Important = 1
}
