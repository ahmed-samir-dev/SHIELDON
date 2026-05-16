using SHIELDON.Application.Features.Payment.DTOs;

namespace SHIELDON.Application.Features.Payment.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentRecordDto>> GetPendingPaymentsAsync(Guid studentId, CancellationToken ct = default);
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(Guid studentId, Guid paymentRecordId, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken ct = default);
}
