using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a financial tracking record for a student's course enrollment.
/// Payment is entirely optional and does NOT block course access.
/// </summary>
public class PaymentRecord
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid EnrollmentId { get; set; }

    // ── Financial Info ──────────────────────────────────────────
    /// <summary>The fee amount for the course at the time of enrollment (in USD).</summary>
    public decimal AmountUSD { get; set; }

    /// <summary>Current status of this payment.</summary>
    public PaymentRecordStatus Status { get; set; } = PaymentRecordStatus.Pending;

    // ── Stripe Info ─────────────────────────────────────────────
    /// <summary>The Stripe Checkout Session ID used to process this payment.</summary>
    public string? StripeSessionId { get; set; }

    /// <summary>UTC timestamp when the payment was confirmed via webhook.</summary>
    public DateTime? PaidAt { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public User? Student { get; set; }
    public Course? Course { get; set; }
    public CourseEnrollment? Enrollment { get; set; }
}
