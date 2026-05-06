using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/leave")]
public class AdminLeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public AdminLeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<LeaveDashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetDashboardSummaryAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<LeaveManagementOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveManagementOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetOptionsAsync(cancellationToken));
    }

    [HttpGet("balances")]
    [ProducesResponseType<PagedResultDto<EmployeeLeaveBalanceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeLeaveBalanceDto>>> GetBalances([FromQuery] LeaveBalanceListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetBalancesAsync(query, cancellationToken));
    }

    [HttpPost("balances/adjustments")]
    [ProducesResponseType<LeaveBalanceTransactionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveBalanceTransactionDto>> AdjustBalance([FromBody] LeaveBalanceAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.AdjustBalanceAsync(request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("requests")]
    [ProducesResponseType<PagedResultDto<LeaveRequestListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<LeaveRequestListItemDto>>> GetRequests([FromQuery] LeaveRequestListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetLeaveRequestsAsync(query, cancellationToken));
    }

    [HttpGet("requests/{leaveRequestId:guid}")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> GetRequestById(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken));
    }

    [HttpPost("requests")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaveRequestListItemDto>> CreateRequest([FromForm] SaveLeaveRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _leaveService.CreateLeaveRequestAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetRequestById), new { leaveRequestId = record.Id }, record);
    }

    [HttpPut("requests/{leaveRequestId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> UpdateRequest(Guid leaveRequestId, [FromForm] SaveLeaveRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.UpdateLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("requests/{leaveRequestId:guid}/approve")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> ApproveRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.ApproveLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("requests/{leaveRequestId:guid}/reject")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> RejectRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.RejectLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("requests/{leaveRequestId:guid}/cancel")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> CancelRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.CancelLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpDelete("requests/{leaveRequestId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRequest(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        await _leaveService.DeleteLeaveRequestAsync(leaveRequestId, cancellationToken);
        return NoContent();
    }

    [HttpGet("requests/{leaveRequestId:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAttachment(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var file = await _leaveService.GetAttachmentAsync(leaveRequestId, cancellationToken);
        return File(file.Content, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpGet("calendar")]
    [ProducesResponseType<LeaveCalendarResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveCalendarResponseDto>> GetCalendar([FromQuery] LeaveCalendarQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetCalendarAsync(query, cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
