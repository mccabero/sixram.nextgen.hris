using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Notifications;
using Sixram.Api.Entities;

namespace Sixram.Api.Services;

public sealed record NotificationDraft(
    string UserId,
    string Title,
    string Message,
    string Type,
    string ReferenceType = "",
    string ReferenceId = "",
    string ActionUrl = "");

public interface INotificationService
{
    Task<NotificationSummaryDto> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<UserNotificationDto>> GetNotificationsAsync(string userId, NotificationListQueryDto query, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    Task CreateAsync(NotificationDraft notification, CancellationToken cancellationToken = default);

    Task CreateManyAsync(IEnumerable<NotificationDraft> notifications, CancellationToken cancellationToken = default);

    Task<string?> GetUserIdForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetUserIdsForEmployeesAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        return new NotificationSummaryDto
        {
            UnreadCount = await _dbContext.Notifications.CountAsync(record => record.UserId == userId && record.ReadAtUtc == null, cancellationToken),
            Recent = await _dbContext.Notifications
                .AsNoTracking()
                .Where(record => record.UserId == userId)
                .OrderByDescending(record => record.CreatedAtUtc)
                .Take(8)
                .Select(record => Map(record))
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResultDto<UserNotificationDto>> GetNotificationsAsync(string userId, NotificationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Notifications
            .AsNoTracking()
            .Where(record => record.UserId == userId);

        if (query.IsRead is true)
        {
            source = source.Where(record => record.ReadAtUtc != null);
        }
        else if (query.IsRead is false)
        {
            source = source.Where(record => record.ReadAtUtc == null);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("read", true) => source.OrderByDescending(record => record.ReadAtUtc.HasValue).ThenByDescending(record => record.CreatedAtUtc),
            ("read", false) => source.OrderBy(record => record.ReadAtUtc.HasValue).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => Map(record))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<UserNotificationDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .SingleOrDefaultAsync(record => record.Id == notificationId && record.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return;
        }

        if (notification.ReadAtUtc is null)
        {
            notification.ReadAtUtc = DateTime.UtcNow;
            notification.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .Where(record => record.UserId == userId && record.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.ReadAtUtc = now;
            notification.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAsync(NotificationDraft notification, CancellationToken cancellationToken = default)
    {
        await CreateManyAsync([notification], cancellationToken);
    }

    public async Task CreateManyAsync(IEnumerable<NotificationDraft> notifications, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var records = notifications
            .Where(record => !string.IsNullOrWhiteSpace(record.UserId))
            .Select(record => new NotificationRecord
            {
                UserId = record.UserId,
                Title = record.Title.Trim(),
                Message = record.Message.Trim(),
                Type = record.Type.Trim(),
                ReferenceType = record.ReferenceType.Trim(),
                ReferenceId = record.ReferenceId.Trim(),
                ActionUrl = record.ActionUrl.Trim(),
                CreatedAtUtc = now
            })
            .ToList();

        if (records.Count == 0)
        {
            return;
        }

        _dbContext.Notifications.AddRange(records);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> GetUserIdForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.Id == employeeId)
            .Select(record => record.UserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserIdsForEmployeesAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default)
    {
        var scopedIds = employeeIds.Distinct().ToArray();
        if (scopedIds.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.Employees
            .AsNoTracking()
            .Where(record => scopedIds.Contains(record.Id) && record.UserId != null)
            .ToDictionaryAsync(record => record.Id, record => record.UserId!, cancellationToken);
    }

    private static UserNotificationDto Map(NotificationRecord record)
    {
        return new UserNotificationDto
        {
            Id = record.Id,
            Title = record.Title,
            Message = record.Message,
            Type = record.Type,
            ReferenceType = record.ReferenceType,
            ReferenceId = record.ReferenceId,
            ActionUrl = record.ActionUrl,
            IsRead = record.ReadAtUtc != null,
            ReadAtUtc = record.ReadAtUtc,
            CreatedAtUtc = record.CreatedAtUtc
        };
    }
}
