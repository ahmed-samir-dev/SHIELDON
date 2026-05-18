namespace SHIELDON.Application.Features.Payment.DTOs;

public class PaymentRecordDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public decimal AmountUSD { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentDisplayId { get; set; } = string.Empty;
}
