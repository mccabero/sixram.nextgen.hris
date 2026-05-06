using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MyPortalController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IPortalService _portalService;
    private readonly IEmployeeDocumentService _employeeDocumentService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceAdjustmentService _attendanceAdjustmentService;
    private readonly ILeaveService _leaveService;
    private readonly IProfileChangeRequestService _profileChangeRequestService;

    public MyPortalController(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IPortalService portalService,
        IEmployeeDocumentService employeeDocumentService,
        IAttendanceService attendanceService,
        IAttendanceAdjustmentService attendanceAdjustmentService,
        ILeaveService leaveService,
        IProfileChangeRequestService profileChangeRequestService)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _portalService = portalService;
        _employeeDocumentService = employeeDocumentService;
        _attendanceService = attendanceService;
        _attendanceAdjustmentService = attendanceAdjustmentService;
        _leaveService = leaveService;
        _profileChangeRequestService = profileChangeRequestService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType<EmployeePortalDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeePortalDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetEmployeeDashboardAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("profile")]
    [ProducesResponseType<EmployeeSelfProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeSelfProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetMyProfileAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("profile-change-requests")]
    [ProducesResponseType<PagedResultDto<EmployeeProfileChangeRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeProfileChangeRequestDto>>> GetProfileChangeRequests([FromQuery] EmployeeProfileChangeRequestListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.GetRequestsAsync(query, GetActorUserId(), ownOnly: true, cancellationToken));
    }

    [HttpGet("profile-change-requests/{requestId:guid}")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> GetProfileChangeRequest(Guid requestId, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.GetByIdAsync(requestId, GetActorUserId(), ownOnly: true, cancellationToken));
    }

    [HttpPost("profile-change-requests")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> CreateProfileChangeRequest([FromBody] SaveProfileChangeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _profileChangeRequestService.CreateOwnRequestAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetProfileChangeRequest), new { requestId = record.Id }, record);
    }

    [HttpPost("profile-change-requests/{requestId:guid}/cancel")]
    [ProducesResponseType<EmployeeProfileChangeRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileChangeRequestDto>> CancelProfileChangeRequest(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _profileChangeRequestService.CancelAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("documents")]
    [ProducesResponseType<EmployeeDocumentProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentProfileDto>> GetDocuments(CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        return Ok(await _employeeDocumentService.GetEmployeeDocumentProfileAsync(actor.LinkedEmployeeId!.Value, cancellationToken));
    }

    [HttpGet("documents/{documentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var employeeId = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(record => record.Id == documentId)
            .Select(record => record.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);

        if (employeeId == Guid.Empty || employeeId != actor.LinkedEmployeeId)
        {
            return Forbid();
        }

        var file = await _employeeDocumentService.GetDocumentContentAsync(documentId, cancellationToken);
        return File(file.Content, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpGet("attendance")]
    [ProducesResponseType<PagedResultDto<AttendanceRecordListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AttendanceRecordListItemDto>>> GetAttendance([FromQuery] AttendanceRecordListQueryDto query, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var scopedQuery = new AttendanceRecordListQueryDto
        {
            EmployeeId = actor.LinkedEmployeeId,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            Status = query.Status,
            Source = query.Source,
            SortBy = query.SortBy,
            Descending = query.Descending,
            Search = query.Search,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Ok(await _attendanceService.GetAttendanceRecordsAsync(scopedQuery, cancellationToken));
    }

    [HttpGet("attendance/adjustments")]
    [ProducesResponseType<PagedResultDto<AttendanceAdjustmentRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AttendanceAdjustmentRequestDto>>> GetAttendanceAdjustments([FromQuery] AttendanceAdjustmentRequestListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.GetRequestsAsync(query, GetActorUserId(), ownOnly: true, cancellationToken));
    }

    [HttpGet("attendance/adjustments/{requestId:guid}")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> GetAttendanceAdjustment(Guid requestId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.GetByIdAsync(requestId, GetActorUserId(), ownOnly: true, cancellationToken));
    }

    [HttpPost("attendance/adjustments")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> CreateAttendanceAdjustment([FromBody] SaveAttendanceAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _attendanceAdjustmentService.CreateOwnRequestAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetAttendanceAdjustment), new { requestId = record.Id }, record);
    }

    [HttpPost("attendance/adjustments/{requestId:guid}/cancel")]
    [ProducesResponseType<AttendanceAdjustmentRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceAdjustmentRequestDto>> CancelAttendanceAdjustment(Guid requestId, [FromBody] ApprovalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceAdjustmentService.CancelAsync(requestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("leave/profile")]
    [ProducesResponseType<EmployeeLeaveProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeLeaveProfileDto>> GetLeaveProfile([FromQuery] int? periodYear, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        return Ok(await _leaveService.GetEmployeeLeaveProfileAsync(actor.LinkedEmployeeId!.Value, periodYear, cancellationToken));
    }

    [HttpGet("leave/options")]
    [ProducesResponseType<LeaveManagementOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveManagementOptionsDto>> GetLeaveOptions(CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var options = await _leaveService.GetOptionsAsync(cancellationToken);

        return Ok(new LeaveManagementOptionsDto
        {
            Employees = options.Employees
                .Where(record => record.Id == actor.LinkedEmployeeId)
                .ToArray(),
            LeaveTypes = options.LeaveTypes,
            Statuses = options.Statuses,
            PeriodYears = options.PeriodYears
        });
    }

    [HttpGet("leave/requests")]
    [ProducesResponseType<PagedResultDto<LeaveRequestListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<LeaveRequestListItemDto>>> GetLeaveRequests([FromQuery] LeaveRequestListQueryDto query, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var scopedQuery = new LeaveRequestListQueryDto
        {
            EmployeeId = actor.LinkedEmployeeId,
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            LeaveTypeId = query.LeaveTypeId,
            Status = query.Status,
            ApproverId = query.ApproverId,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            Descending = query.Descending,
            Search = query.Search,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Ok(await _leaveService.GetLeaveRequestsAsync(scopedQuery, cancellationToken));
    }

    [HttpGet("leave/requests/{leaveRequestId:guid}")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> GetLeaveRequest(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var record = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        if (record.EmployeeId != actor.LinkedEmployeeId)
        {
            return Forbid();
        }

        return Ok(record);
    }

    [HttpPost("leave/requests")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaveRequestListItemDto>> CreateLeaveRequest([FromForm] SaveLeaveRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        if (request.EmployeeId != actor.LinkedEmployeeId)
        {
            return Forbid();
        }

        var record = await _leaveService.CreateLeaveRequestAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetLeaveRequest), new { leaveRequestId = record.Id }, record);
    }

    [HttpPost("leave/requests/{leaveRequestId:guid}/cancel")]
    [ProducesResponseType<LeaveRequestListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveRequestListItemDto>> CancelLeaveRequest(Guid leaveRequestId, [FromBody] LeaveActionRequestDto request, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var existing = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        if (existing.EmployeeId != actor.LinkedEmployeeId)
        {
            return Forbid();
        }

        return Ok(await _leaveService.CancelLeaveRequestAsync(leaveRequestId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("leave/requests/{leaveRequestId:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadLeaveAttachment(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        var existing = await _leaveService.GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
        if (existing.EmployeeId != actor.LinkedEmployeeId)
        {
            return Forbid();
        }

        var file = await _leaveService.GetAttachmentAsync(leaveRequestId, cancellationToken);
        return File(file.Content, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpGet("payslips")]
    [ProducesResponseType<PagedResultDto<PayslipSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PayslipSummaryDto>>> GetPayslips([FromQuery] MyPayslipListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetMyPayslipsAsync(query, GetActorUserId(), cancellationToken));
    }

    [HttpGet("payslips/{payrollRunItemId:guid}")]
    [ProducesResponseType<PayslipDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayslipDto>> GetPayslip(Guid payrollRunItemId, CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetMyPayslipAsync(payrollRunItemId, GetActorUserId(), cancellationToken));
    }

    [HttpGet("requests")]
    [ProducesResponseType<IReadOnlyList<EmployeeRequestHistoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeRequestHistoryItemDto>>> GetMyRequests(CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetMyRequestHistoryAsync(GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
