using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Payment.DTOs;
using SHIELDON.Application.Features.Payment.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    /// <summary>
    /// Gets paginated payment history for the authenticated user.
    /// Admins see all payments; Students see only their own.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "Student,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PaymentRecordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] PaymentHistoryQueryParams query, CancellationToken ct)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();
        
        var result = await _paymentService.GetPaymentHistoryAsync(userId, userRole, query, ct);
        return Ok(ApiResponse<PagedResponse<PaymentRecordDto>>.Ok(result, "Payment history retrieved successfully."));
    }

    /// <summary>
    /// Gets all pending payment records for the authenticated student.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentRecordDto>>), 200)]
    public async Task<IActionResult> GetPendingPayments(CancellationToken ct)
    {
        var studentId = GetUserId();
        var records = await _paymentService.GetPendingPaymentsAsync(studentId, ct);
        return Ok(ApiResponse<List<PaymentRecordDto>>.Ok(records, "Pending payments retrieved successfully."));
    }

    /// <summary>
    /// Creates a Stripe Checkout session for a specific payment record.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutSessionResponse>), 200)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var studentId = GetUserId();
        var result = await _paymentService.CreateCheckoutSessionAsync(studentId, request.PaymentRecordId, ct);
        return Ok(ApiResponse<CheckoutSessionResponse>.Ok(result, "Checkout session created successfully."));
    }
}
