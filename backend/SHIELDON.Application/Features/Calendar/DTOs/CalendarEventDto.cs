using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Calendar.DTOs;

public class CalendarEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public EventType Type { get; set; }
    
    public Guid? CourseId { get; set; }
    public string? CourseName { get; set; }
    
    // For routing (e.g., to click an assignment and go to its page)
    public Guid? SourceEntityId { get; set; }
}
