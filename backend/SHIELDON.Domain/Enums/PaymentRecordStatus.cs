namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents the status of a student's payment for a course.
/// </summary>
public enum PaymentRecordStatus
{
    /// <summary>Fee is owed, but student has not initiated payment yet.</summary>
    Pending = 0,

    /// <summary>Stripe Checkout session created, awaiting confirmation webhook.</summary>
    Processing = 1,

    /// <summary>Payment successfully confirmed via Stripe webhook.</summary>
    Paid = 2,

    /// <summary>Payment failed or Stripe session expired.</summary>
    Failed = 3
}
