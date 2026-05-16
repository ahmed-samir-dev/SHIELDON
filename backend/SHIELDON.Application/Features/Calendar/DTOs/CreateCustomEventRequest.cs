namespace SHIELDON.Application.Features.Calendar.DTOs;

public class CreateCustomEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public DateTime EventDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    
    // Nullable for Global events
    public Guid? CourseId { get; set; }
}
