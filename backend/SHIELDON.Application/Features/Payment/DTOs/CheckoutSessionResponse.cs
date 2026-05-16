namespace SHIELDON.Application.Features.Payment.DTOs;

public class CheckoutSessionResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
