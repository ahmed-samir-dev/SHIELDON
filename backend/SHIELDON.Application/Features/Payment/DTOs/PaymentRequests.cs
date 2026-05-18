namespace SHIELDON.Application.Features.Payment.DTOs;

public record PaymentHistoryQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null
);
