using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public sealed class AuditLogEntry
{
    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public Guid? EmployeeId { get; init; }

    public object? OldValues { get; init; }

    public object? NewValues { get; init; }

    public string Remarks { get; init; } = string.Empty;
}

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);

    Task<AuditLogDto> GetAuditLogByIdAsync(Guid auditLogId, string? actorUserId, CancellationToken cancellationToken = default);
}

public class AuditLogService : IAuditLogService
{
    private static readonly string[] PayrollEntityTypes =
    [
        AuditEntityTypes.CompensationProfile,
        AuditEntityTypes.RecurringEarning,
        AuditEntityTypes.RecurringDeduction,
        AuditEntityTypes.PayrollRun,
        AuditEntityTypes.PayrollAdjustment,
        AuditEntityTypes.Payslip
    ];

    private static readonly string[] ProvidentFundEntityTypes =
    [
        ProvidentFundAuditEntityTypes.Policy,
        ProvidentFundAuditEntityTypes.VestingRule,
        ProvidentFundAuditEntityTypes.Enrollment,
        ProvidentFundAuditEntityTypes.ContributionBatch,
        ProvidentFundAuditEntityTypes.LedgerTransaction,
        ProvidentFundAuditEntityTypes.Withdrawal,
        ProvidentFundAuditEntityTypes.Adjustment,
        "provident_fund_policy",
        "provident_fund_vesting_rule",
        "provident_fund_enrollment",
        "provident_fund_contribution_batch",
        "provident_fund_contribution_batch_line",
        "provident_fund_ledger_transaction",
        "provident_fund_withdrawal_request",
        "provident_fund_withdrawal_approval",
        "provident_fund_adjustment",
        "provident_fund_adjustment_approval"
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var principal = httpContext?.User;
            var actorUserId = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var actorName = principal?.FindFirstValue(ClaimTypes.Name)
                ?? principal?.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;

            var record = new AuditLog
            {
                ActorUserId = string.IsNullOrWhiteSpace(actorUserId) ? null : actorUserId,
                ActorNameSnapshot = actorName,
                Action = Normalize(entry.Action, 128),
                EntityType = Normalize(entry.EntityType, 128),
                EntityId = Normalize(entry.EntityId, 128),
                EmployeeId = entry.EmployeeId,
                OldValuesJson = SerializeSanitized(entry.OldValues),
                NewValuesJson = SerializeSanitized(entry.NewValues),
                IpAddress = Normalize(httpContext?.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = Normalize(httpContext?.Request.Headers.UserAgent.ToString(), 512),
                Remarks = Normalize(entry.Remarks, 1000),
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.AuditLogs.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Audit log write failed for action {Action} on {EntityType}/{EntityId}.",
                entry.Action,
                entry.EntityType,
                entry.EntityId);
        }
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessAuditLogs(actor);

        var source = ApplyAuditVisibility(_dbContext.AuditLogs
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.ActorUser), actor);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.ActorNameSnapshot.Contains(search) ||
                record.Action.Contains(search) ||
                record.EntityType.Contains(search) ||
                record.EntityId.Contains(search) ||
                record.Remarks.Contains(search) ||
                (record.Employee != null && (
                    record.Employee.EmployeeCode.Contains(search) ||
                    record.Employee.FirstName.Contains(search) ||
                    record.Employee.MiddleName.Contains(search) ||
                    record.Employee.LastName.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            source = source.Where(record => record.EntityType == query.EntityType.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            source = source.Where(record => record.Action == query.Action.Trim().ToLowerInvariant());
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DateFrom is not null)
        {
            var fromUtc = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc >= fromUtc);
        }

        if (query.DateTo is not null)
        {
            var toExclusiveUtc = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc < toExclusiveUtc);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("action", true) => source.OrderByDescending(record => record.Action).ThenByDescending(record => record.CreatedAtUtc),
            ("action", false) => source.OrderBy(record => record.Action).ThenByDescending(record => record.CreatedAtUtc),
            ("entity", true) => source.OrderByDescending(record => record.EntityType).ThenByDescending(record => record.CreatedAtUtc),
            ("entity", false) => source.OrderBy(record => record.EntityType).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => MapAuditLog(record))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AuditLogDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<AuditLogDto> GetAuditLogByIdAsync(Guid auditLogId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessAuditLogs(actor);

        var record = await ApplyAuditVisibility(_dbContext.AuditLogs
            .AsNoTracking()
            .Include(item => item.Employee)
            .Include(item => item.ActorUser), actor)
            .SingleOrDefaultAsync(item => item.Id == auditLogId, cancellationToken)
            ?? throw new NotFoundException($"Audit log '{auditLogId}' was not found.");

        return MapAuditLog(record);
    }

    private static IQueryable<AuditLog> ApplyAuditVisibility(IQueryable<AuditLog> source, PortalActorContext actor)
    {
        if (actor.IsAdministrator)
        {
            return source;
        }

        if (actor.IsPayrollOfficer)
        {
            return source.Where(record => PayrollEntityTypes.Contains(record.EntityType) || ProvidentFundEntityTypes.Contains(record.EntityType));
        }

        if (actor.IsHumanResources)
        {
            return source.Where(record => !PayrollEntityTypes.Contains(record.EntityType) || ProvidentFundEntityTypes.Contains(record.EntityType));
        }

        return source.Where(_ => false);
    }

    private static AuditLogDto MapAuditLog(AuditLog record)
    {
        return new AuditLogDto
        {
            Id = record.Id,
            ActorUserId = record.ActorUserId ?? string.Empty,
            ActorName = !string.IsNullOrWhiteSpace(record.ActorNameSnapshot)
                ? record.ActorNameSnapshot
                : BuildUserDisplayName(record.ActorUser),
            Action = record.Action,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee == null
                ? string.Empty
                : string.Join(" ", new[]
                {
                    record.Employee.FirstName.Trim(),
                    record.Employee.MiddleName.Trim(),
                    record.Employee.LastName.Trim(),
                    record.Employee.Suffix.Trim()
                }.Where(part => !string.IsNullOrWhiteSpace(part))),
            OldValuesJson = record.OldValuesJson,
            NewValuesJson = record.NewValuesJson,
            IpAddress = record.IpAddress,
            UserAgent = record.UserAgent,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc
        };
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }

    private static string SerializeSanitized(object? payload)
    {
        if (payload is null)
        {
            return string.Empty;
        }

        var node = JsonSerializer.SerializeToNode(payload, payload.GetType());
        SanitizeNode(node, string.Empty);
        return node?.ToJsonString() ?? string.Empty;
    }

    private static void SanitizeNode(JsonNode? node, string propertyName)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var keyValuePair in jsonObject.ToList())
            {
                var childName = keyValuePair.Key;
                if (IsSecretField(childName))
                {
                    jsonObject[childName] = "[redacted]";
                    continue;
                }

                if (IsGovernmentIdField(childName))
                {
                    jsonObject[childName] = MaskGovernmentValue(keyValuePair.Value?.ToString());
                    continue;
                }

                SanitizeNode(keyValuePair.Value, childName);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                SanitizeNode(child, propertyName);
            }

            return;
        }

        if (node is JsonValue && IsGovernmentIdField(propertyName))
        {
            return;
        }
    }

    private static bool IsSecretField(string propertyName)
    {
        return propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("securitystamp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGovernmentIdField(string propertyName)
    {
        return propertyName.Contains("sss", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("philhealth", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("pagibig", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("pag-ibig", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("tin", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Contains("governmentid", StringComparison.OrdinalIgnoreCase);
    }

    private static string MaskGovernmentValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }

    private static string Normalize(string? value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
