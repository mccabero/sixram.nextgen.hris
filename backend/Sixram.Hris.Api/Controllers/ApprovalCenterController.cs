using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/approvals")]
public class ApprovalCenterController : ControllerBase
{
    private readonly IUserAccessService _userAccessService;
    private readonly IApprovalCenterService _approvalCenterService;
    private readonly IProfileChangeRequestService _profileChangeRequestService;
    private readonly IAttendanceAdjustmentService _attendanceAdjustmentService;
    private readonly ILeaveService _leaveService;
    private readonly IPayrollService _payrollService;

    public ApprovalCenterController(
        IUserAccessService userAccessService,
        IApprovalCenterService approvalCenterService,
        IProfileChangeRequestService profileChangeRequestService,
        IAttendanceAdjustmentService attendanceAdjustmentService,
        ILeaveService leaveService,
        IPayrollService payrollService)
    {
        _userAccessService = userAccessService;
        _approvalCenterService = approvalCenterService;
        _profileChangeRequestService = profileChangeRequestService;
        _attendanceAdjustmentService = attendanceAdjustmentService;
        _leaveService = leaveService;
        _payrollService = payrollService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<ApprovalCenterSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalCenterSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _approvalCenterService.GetSummaryAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<ApprovalCenterOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalCenterOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _approvalCenterService.GetOptionsAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("inbox")]
    [ProducesResponseType<PagedResultDto<ApprovalCenterInboxItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ApprovalCenterInboxItemDto>>> GetInbox([FromQuery] ApprovalCenterQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _approvalCenterService.GetInboxAsync(query, GetActorUserId(), cancellationToken));
    }

    [HttpGet("profile-change-requests/{requestId:guid}")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> GetProfileChangeRequest(Guid requestId, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.GetByIdAsync(requestId, GetActorUserId(), ownOnly: false, cancellationToken));
    }

    [HttpPost("profile-change-requests/{requestId:guid}/approve")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> ApproveProfileChangeRequest(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.ApproveAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("profile-change-requests/{requestId:guid}/reject")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> RejectProfileChangeRequest(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.RejectAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("attendance-adjustments/{requestId:guid}")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> GetAttendanceAdjustment(Guid requestId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.GetByIdAsync(requestId, GetActorUserId(), ownOnly: false, cancellationToken));
    }

    [HttpPost("attendance-adjustments/{requestId:guid}/approve")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> ApproveAttendanceAdjustment(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.ApproveAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("attendance-adjustments/{requestId:guid}/reject")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> RejectAttendanceAdjustment(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.RejectAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("leave-requests/{leaveRequestId:guid}")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> GetLeaveRequest(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetActorContextAsync(GetActorUserId(), cancellationToken);
        var record = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        _userAccessService.EnsureCanManageEmployee(actor, record.EmployeeId);
        if (!actor.IsAdministrator && !actor.IsHumanResources && actor.LinkedEmployeeId == record.EmployeeId)
        {
            return Forbid();
        }

        return Ok(record);
    }

    [HttpPost("leave-requests/{leaveRequestId:guid}/approve")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> ApproveLeaveRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetActorContextAsync(GetActorUserId(), cancellationToken);
        var record = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        _userAccessService.EnsureCanManageEmployee(actor, record.EmployeeId);
        if (!actor.IsAdministrator && !actor.IsHumanResources && actor.LinkedEmployeeId == record.EmployeeId)
        {
            return Forbid();
        }

        return Ok(await _leaveService.ApproveLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("leave-requests/{leaveRequestId:guid}/reject")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> RejectLeaveRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetActorContextAsync(GetActorUserId(), cancellationToken);
        var record = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        _userAccessService.EnsureCanManageEmployee(actor, record.EmployeeId);
        if (!actor.IsAdministrator && !actor.IsHumanResources && actor.LinkedEmployeeId == record.EmployeeId)
        {
            return Forbid();
        }

        return Ok(await _leaveService.RejectLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("payroll-adjustments/{payrollAdjustmentId:guid}/approve")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> ApprovePayrollAdjustment(Guid payrollAdjustmentId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetActorContextAsync(GetActorUserId(), cancellationToken);
        _userAccessService.EnsureCanReviewPayrollAdjustments(actor);
        return Ok(await _payrollService.ApprovePayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("payroll-adjustments/{payrollAdjustmentId:guid}/reject")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> RejectPayrollAdjustment(Guid payrollAdjustmentId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetActorContextAsync(GetActorUserId(), cancellationToken);
        _userAccessService.EnsureCanReviewPayrollAdjustments(actor);
        return Ok(await _payrollService.RejectPayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
