using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/attendance")]
public class AdminAttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceSetupService _attendanceSetupService;

    public AdminAttendanceController(IAttendanceService attendanceService, IAttendanceSetupService attendanceSetupService)
    {
        _attendanceService = attendanceService;
        _attendanceSetupService = attendanceSetupService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<AttendanceDashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.GetDashboardSummaryAsync(cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<AttendanceListOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceListOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetOptionsAsync(cancellationToken: cancellationToken));
    }

    [HttpGet("records")]
    [ProducesResponseType<PagedResultDto<AttendanceRecordListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AttendanceRecordListItemDto>>> GetRecords(
        [FromQuery] AttendanceRecordListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.GetAttendanceRecordsAsync(query, cancellationToken));
    }

    [HttpGet("records/{attendanceRecordId:guid}")]
    [ProducesResponseType<AttendanceRecordListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceRecordListItemDto>> GetRecordById(Guid attendanceRecordId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.GetAttendanceRecordByIdAsync(attendanceRecordId, cancellationToken));
    }

    [HttpPost("records")]
    [ProducesResponseType<AttendanceRecordListItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AttendanceRecordListItemDto>> CreateRecord(
        [FromBody] SaveAttendanceRecordRequestDto request,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceService.CreateAttendanceRecordAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetRecordById), new { attendanceRecordId = record.AttendanceRecordId }, record);
    }

    [HttpPut("records/{attendanceRecordId:guid}")]
    [ProducesResponseType<AttendanceRecordListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceRecordListItemDto>> UpdateRecord(
        Guid attendanceRecordId,
        [FromBody] SaveAttendanceRecordRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.UpdateAttendanceRecordAsync(attendanceRecordId, request, GetActorUserId(), cancellationToken));
    }

    [HttpDelete("records/{attendanceRecordId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRecord(Guid attendanceRecordId, CancellationToken cancellationToken)
    {
        await _attendanceService.DeleteAttendanceRecordAsync(attendanceRecordId, cancellationToken);
        return NoContent();
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
