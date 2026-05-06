using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Notifications;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<NotificationSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.GetSummaryAsync(GetActorUserId()!, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType<PagedResultDto<UserNotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<UserNotificationDto>>> GetNotifications([FromQuery] NotificationListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.GetNotificationsAsync(GetActorUserId()!, query, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(GetActorUserId()!, notificationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(GetActorUserId()!, cancellationToken);
        return NoContent();
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
