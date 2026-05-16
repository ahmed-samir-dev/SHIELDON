using System.ComponentModel.DataAnnotations;

namespace SHIELDON.Application.Features.Payment.DTOs;

public class CreateCheckoutSessionRequest
{
    [Required]
    public Guid PaymentRecordId { get; set; }
}
