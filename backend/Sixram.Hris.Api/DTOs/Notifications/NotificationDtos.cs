using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Notifications;

public sealed class UserNotificationDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string ReferenceType { get; init; } = string.Empty;

    public string ReferenceId { get; init; } = string.Empty;

    public string ActionUrl { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class NotificationSummaryDto
{
    public int UnreadCount { get; init; }

    public IReadOnlyList<UserNotificationDto> Recent { get; init; } = Array.Empty<UserNotificationDto>();
}

public sealed class NotificationListQueryDto : PagedQueryDto
{
    public bool? IsRead { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class MarkNotificationsReadRequestDto
{
    public IReadOnlyList<Guid> NotificationIds { get; init; } = Array.Empty<Guid>();
}
