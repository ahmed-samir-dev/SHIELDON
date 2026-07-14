namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents the delivery state of a chat message, following a WhatsApp-style
/// progression from Sent → Delivered → Read.
/// </summary>
public enum MessageStatus
{
    /// <summary>Message has been persisted to the database and sent to the hub.</summary>
    Sent = 0,

    /// <summary>Message has been acknowledged by the recipient's SignalR connection.</summary>
    Delivered = 1,

    /// <summary>Message has been opened and viewed by the recipient.</summary>
    Read = 2
}
