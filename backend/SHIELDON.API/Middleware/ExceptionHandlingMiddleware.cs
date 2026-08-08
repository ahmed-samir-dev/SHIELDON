using SHIELDON.Application.Common;
using SHIELDON.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SHIELDON.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches all unhandled exceptions, maps them to the correct HTTP status code,
/// and returns a consistent ApiResponse envelope.
///
/// Why: Controllers should not catch exceptions - this middleware is the single safety net.
/// All domain exceptions map to specific HTTP status codes here.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        string message;
        List<string> errors = [];

        switch (exception)
        {
            case NotFoundException notFound:
                statusCode = HttpStatusCode.NotFound;
                message = notFound.Message;
                _logger.LogWarning("Resource not found: {Message}", notFound.Message);
                break;

            case BusinessRuleException businessRule:
                statusCode = HttpStatusCode.BadRequest;
                message = businessRule.Message;
                _logger.LogWarning("Business rule violation: {Message}", businessRule.Message);
                break;

            case ForbiddenException forbidden:
                statusCode = HttpStatusCode.Forbidden;
                message = forbidden.Message;
                _logger.LogWarning("Forbidden access attempt: {Message}", forbidden.Message);
                break;

            case ConflictException conflict:
                statusCode = HttpStatusCode.Conflict;
                message = conflict.Message;
                _logger.LogWarning("Conflict: {Message}", conflict.Message);
                break;

            case UnauthorizedException unauthorized:
                statusCode = HttpStatusCode.Unauthorized;
                message = unauthorized.Message;
                _logger.LogWarning("Unauthorized: {Message}", unauthorized.Message);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Authentication is required to access this resource.";
                _logger.LogWarning("Unauthorized access attempt.");
                break;

            default:
                // Unexpected error - log full details server-side, return generic message to client
                statusCode = HttpStatusCode.InternalServerError;
                var isProd = context.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName == "Production";
                message = isProd ? "An unexpected error occurred. Please try again later." : $"{exception.GetType().Name}: {exception.Message}";
                if (!isProd && exception.InnerException != null)
                {
                    message += $" | Inner: {exception.InnerException.Message}";
                }
                _logger.LogError(exception,
                    "Unhandled exception: {ExceptionType} - {Message}",
                    exception.GetType().Name,
                    exception.Message);
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message, errors.Count > 0 ? errors : null);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
