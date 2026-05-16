using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Features.Payment.DTOs;
using SHIELDON.Application.Features.Payment.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;

namespace SHIELDON.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, IConfiguration config, ILogger<PaymentService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<List<PaymentRecordDto>> GetPendingPaymentsAsync(Guid studentId, CancellationToken ct = default)
    {
        var records = await _db.PaymentRecords
            .Include(r => r.Course)
            .AsNoTracking()
            .Where(r => r.StudentId == studentId && (r.Status == PaymentRecordStatus.Pending || r.Status == PaymentRecordStatus.Processing))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return records.Select(r => new PaymentRecordDto
        {
            Id = r.Id,
            CourseId = r.CourseId,
            CourseName = r.Course!.Title,
            AmountUSD = r.AmountUSD,
            Status = r.Status.ToString(),
            PaidAt = r.PaidAt,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(Guid studentId, Guid paymentRecordId, CancellationToken ct = default)
    {
        var record = await _db.PaymentRecords
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.Id == paymentRecordId, ct)
            ?? throw new NotFoundException("PaymentRecord", paymentRecordId);

        if (record.StudentId != studentId)
            throw new ForbiddenException("You can only pay for your own enrollments.");

        if (record.Status == PaymentRecordStatus.Paid)
            throw new BusinessRuleException("This course fee has already been paid.");

        var frontendUrl = _config["Stripe:FrontendUrl"] ?? "http://localhost:4201";

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            Mode = "payment",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = record.AmountUSD * 100, // Stripe expects cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = record.Course!.Title,
                            Description = $"Course Fee for {record.Course.CourseCode}"
                        }
                    },
                    Quantity = 1,
                }
            ],
            SuccessUrl = $"{frontendUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{frontendUrl}/payment/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "paymentRecordId", record.Id.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        record.StripeSessionId = session.Id;
        record.Status = PaymentRecordStatus.Processing;
        await _db.SaveChangesAsync(ct);

        return new CheckoutSessionResponse
        {
            CheckoutUrl = session.Url,
            SessionId = session.Id
        };
    }

    public async Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken ct = default)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("Stripe WebhookSecret is not configured.");
            return;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, webhookSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session?.Metadata != null && session.Metadata.TryGetValue("paymentRecordId", out var recordIdStr))
                {
                    if (Guid.TryParse(recordIdStr, out var recordId))
                    {
                        var record = await _db.PaymentRecords.FindAsync([recordId], ct);
                        if (record != null && record.Status != PaymentRecordStatus.Paid)
                        {
                            record.Status = PaymentRecordStatus.Paid;
                            record.PaidAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync(ct);
                            _logger.LogInformation($"Payment confirmed for record {recordId}");
                        }
                    }
                }
            }
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe Webhook signature verification failed.");
            throw;
        }
    }
}
