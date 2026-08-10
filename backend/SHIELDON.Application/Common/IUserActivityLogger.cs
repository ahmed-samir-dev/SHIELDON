using System;
using System.Threading;
using System.Threading.Tasks;

namespace SHIELDON.Application.Common;

public interface IUserActivityLogger
{
    Task LogAsync(
        Guid? userId,
        string category,
        string action,
        string description,
        string? entityId = null,
        string? entityType = null,
        object? metadata = null,
        CancellationToken ct = default);
}

public class NullUserActivityLogger : IUserActivityLogger
{
    public Task LogAsync(
        Guid? userId,
        string category,
        string action,
        string description,
        string? entityId = null,
        string? entityType = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
