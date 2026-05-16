namespace SHIELDON.Application.Features.Calendar.DTOs;

public class UpdateCustomEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public DateTime EventDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    
    public Guid? CourseId { get; set; }
}
