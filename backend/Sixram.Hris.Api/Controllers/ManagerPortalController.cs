using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/manager")]
public class ManagerPortalController : ControllerBase
{
    private readonly IUserAccessService _userAccessService;
    private readonly IPortalService _portalService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILeaveService _leaveService;

    public ManagerPortalController(
        IUserAccessService userAccessService,
        IPortalService portalService,
        IAttendanceService attendanceService,
        ILeaveService leaveService)
    {
        _userAccessService = userAccessService;
        _portalService = portalService;
        _attendanceService = attendanceService;
        _leaveService = leaveService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType<ManagerDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetManagerDashboardAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<ManagerPortalOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerPortalOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetManagerOptionsAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("team")]
    [ProducesResponseType<PagedResultDto<ManagerTeamMemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ManagerTeamMemberDto>>> GetTeam([FromQuery] ManagerTeamMemberListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _portalService.GetMyTeamAsync(query, GetActorUserId(), cancellationToken));
    }

    [HttpGet("attendance")]
    [ProducesResponseType<PagedResultDto<AttendanceRecordListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AttendanceRecordListItemDto>>> GetTeamAttendance([FromQuery] AttendanceRecordListQueryDto query, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        if (!actor.IsManager)
        {
            return Forbid();
        }

        if (query.EmployeeId is not null && !actor.ManagedEmployeeIds.Contains(query.EmployeeId.Value))
        {
            return Forbid();
        }

        var scopedQuery = new AttendanceRecordListQueryDto
        {
            EmployeeId = query.EmployeeId,
            EmployeeIds = actor.ManagedEmployeeIds,
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
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

    [HttpGet("leave/requests")]
    [ProducesResponseType<PagedResultDto<LeaveRequestListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<LeaveRequestListItemDto>>> GetTeamLeaveRequests([FromQuery] LeaveRequestListQueryDto query, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        if (!actor.IsManager)
        {
            return Forbid();
        }

        if (query.EmployeeId is not null && !actor.ManagedEmployeeIds.Contains(query.EmployeeId.Value))
        {
            return Forbid();
        }

        var scopedQuery = new LeaveRequestListQueryDto
        {
            EmployeeId = query.EmployeeId,
            EmployeeIds = actor.ManagedEmployeeIds,
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

    [HttpGet("leave/calendar")]
    [ProducesResponseType<LeaveCalendarResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveCalendarResponseDto>> GetTeamLeaveCalendar([FromQuery] LeaveCalendarQueryDto query, CancellationToken cancellationToken)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(GetActorUserId(), cancellationToken);
        if (!actor.IsManager)
        {
            return Forbid();
        }

        if (query.EmployeeId is not null && !actor.ManagedEmployeeIds.Contains(query.EmployeeId.Value))
        {
            return Forbid();
        }

        var scopedQuery = new LeaveCalendarQueryDto
        {
            Year = query.Year,
            Month = query.Month,
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            EmployeeId = query.EmployeeId,
            EmployeeIds = actor.ManagedEmployeeIds,
            LeaveTypeId = query.LeaveTypeId,
            Status = query.Status
        };

        return Ok(await _leaveService.GetCalendarAsync(scopedQuery, cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
