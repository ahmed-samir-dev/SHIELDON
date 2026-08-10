namespace SHIELDON.Domain.Interfaces;

/// <summary>
/// Defines entities that support soft deletion to preserve audit trails and historical records.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
