namespace SHIELDON.Domain.Entities;

public class ExamToken
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public Guid Token { get; set; } = Guid.NewGuid();
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    public ExamAttempt? Attempt { get; set; }
}
