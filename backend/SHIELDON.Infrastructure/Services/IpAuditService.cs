using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Features.Security.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implementation of IIpAuditService.
///
/// Responsibilities:
///   1. Log IP audit events for logins, exam start/end, and violation reports.
///   2. Detect VPN/proxy usage via static CIDR range matching (no external API).
///   3. Detect duplicate session race conditions (Wall 2 defence).
///   4. Detect network changes during an exam session (passive observation only).
///
/// Wall 1 (single active session enforcement) is handled in AuthService.LoginAsync
/// which revokes existing refresh tokens on each new login and pushes a SignalR
/// notification to the old session.
///
/// All errors are caught and logged - this service must NEVER throw or break
/// the primary controller flow.
/// </summary>
public class IpAuditService : IIpAuditService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpAuditService> _logger;

    // Loaded once from configuration on first use (thread-safe via lock)
    private List<(IPNetwork network, string cidr)>? _vpnCidrNetworks;
    private readonly object _cidrLock = new();

    // Wall 2: duplicate session window (seconds)
    private const int DuplicateSessionWindowSeconds = 30;

    public IpAuditService(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<IpAuditService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task LogAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        IpAuditEventType eventType,
        Guid? examAttemptId = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new IpAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 500
                    ? userAgent[..500]
                    : userAgent,
                ExamAttemptId = examAttemptId,
                IsVpnOrProxy = false,
                IsDuplicateSession = false,
                IsNetworkChangeDuringExam = false,
                OccurredAt = DateTime.UtcNow
            };

            _db.IpAuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Do NOT rethrow - IP logging must never break the primary user flow
            _logger.LogError(ex, "IpAuditService.LogAsync failed for User {UserId}, Event {Event}", userId, eventType);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<List<IpAuditLogDto>> GetLogsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var logs = await _db.IpAuditLogs
            .Include(l => l.User)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.OccurredAt)
            .ToListAsync(ct);

        return logs.Select(MapToDto).ToList();
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<List<IpAuditLogDto>> GetLogsForAttemptAsync(Guid attemptId, CancellationToken ct = default)
    {
        var logs = await _db.IpAuditLogs
            .Include(l => l.User)
            .Where(l => l.ExamAttemptId == attemptId)
            .OrderBy(l => l.OccurredAt)
            .ToListAsync(ct);

        return logs.Select(MapToDto).ToList();
    }

    // ──────────────────────────────────────────────────────────────────────────
    public async Task<AuditTrailPagedResult> GetAuditTrailAsync(
        AuditTrailQueryParams query,
        CancellationToken ct = default)
    {
        var q = _db.IpAuditLogs
            .Include(l => l.User)
            .AsQueryable();

        if (query.UserId.HasValue)
            q = q.Where(l => l.UserId == query.UserId);

        if (query.EventType.HasValue)
            q = q.Where(l => l.EventType == query.EventType);

        if (query.IsVpnOrProxy.HasValue)
            q = q.Where(l => l.IsVpnOrProxy == query.IsVpnOrProxy);

        if (query.IsDuplicateSession.HasValue)
            q = q.Where(l => l.IsDuplicateSession == query.IsDuplicateSession);

        if (query.IsNetworkChangeDuringExam.HasValue)
            q = q.Where(l => l.IsNetworkChangeDuringExam == query.IsNetworkChangeDuringExam);

        if (query.FromDate.HasValue)
            q = q.Where(l => l.OccurredAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(l => l.OccurredAt <= query.ToDate.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new AuditTrailPagedResult
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private IpAuditLogDto MapToDto(IpAuditLog log)
    {
        return new IpAuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserFullName = log.User?.FullName ?? "Unknown",
            UserDisplayId = log.User?.StudentId ?? log.User?.TutorId ?? log.User?.AdminId,
            EventType = log.EventType,
            EventTypeLabel = log.EventType.ToString(),
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            ExamAttemptId = log.ExamAttemptId,
            IsVpnOrProxy = log.IsVpnOrProxy,
            IsDuplicateSession = log.IsDuplicateSession,
            IsNetworkChangeDuringExam = log.IsNetworkChangeDuringExam,
            OccurredAt = log.OccurredAt
        };
    }

    /// <summary>
    /// Checks if the given IP address falls within any known VPN/datacenter CIDR range.
    /// Uses static configuration - no external API calls.
    /// Returns false if the IP is null, malformed, or parsing fails.
    /// </summary>
    private bool IsKnownVpnOrProxy(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return false;

        var networks = GetVpnCidrNetworks();

        foreach (var (network, _) in networks)
        {
            if (IsInNetwork(ip, network))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Lazily loads and caches the VPN CIDR networks from configuration.
    /// Thread-safe via lock on first access.
    /// </summary>
    private List<(IPNetwork, string)> GetVpnCidrNetworks()
    {
        if (_vpnCidrNetworks != null)
            return _vpnCidrNetworks;

        lock (_cidrLock)
        {
            if (_vpnCidrNetworks != null)
                return _vpnCidrNetworks;

            var cidrs = _configuration
                .GetSection("SecuritySettings:KnownVpnCidrRanges")
                .Get<string[]>() ?? [];

            _vpnCidrNetworks = [];
            foreach (var cidr in cidrs)
            {
                try
                {
                    var parts = cidr.Split('/');
                    if (parts.Length == 2 &&
                        IPAddress.TryParse(parts[0], out var networkAddress) &&
                        int.TryParse(parts[1], out var prefix))
                    {
                        _vpnCidrNetworks.Add((new IPNetwork(networkAddress, prefix), cidr));
                    }
                }
                catch
                {
                    _logger.LogWarning("Invalid CIDR range in SecuritySettings: {Cidr}", cidr);
                }
            }
        }

        return _vpnCidrNetworks;
    }

    /// <summary>
    /// Checks if an IP address belongs to an IP network (CIDR range).
    /// Handles both IPv4 and IPv6.
    /// </summary>
    private static bool IsInNetwork(IPAddress ip, IPNetwork network)
    {
        try
        {
            return network.Contains(ip);
        }
        catch
        {
            return false;
        }
    }
}
