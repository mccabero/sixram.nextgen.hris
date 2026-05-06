using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/attendance/setup")]
public class AdminAttendanceSetupController : ControllerBase
{
    private readonly IAttendanceSetupService _attendanceSetupService;

    public AdminAttendanceSetupController(IAttendanceSetupService attendanceSetupService)
    {
        _attendanceSetupService = attendanceSetupService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<AttendanceSetupSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceSetupSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<AttendanceListOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceListOptionsDto>> GetOptions([FromQuery] Guid? assignmentId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetOptionsAsync(assignmentId, cancellationToken));
    }

    [HttpGet("work-schedules")]
    [ProducesResponseType<PagedResultDto<WorkScheduleRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<WorkScheduleRecordDto>>> GetWorkSchedules(
        [FromQuery] WorkScheduleListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetWorkSchedulesAsync(query, cancellationToken));
    }

    [HttpGet("work-schedules/{workScheduleId:guid}")]
    [ProducesResponseType<WorkScheduleRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkScheduleRecordDto>> GetWorkScheduleById(Guid workScheduleId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetWorkScheduleByIdAsync(workScheduleId, cancellationToken));
    }

    [HttpPost("work-schedules")]
    [ProducesResponseType<WorkScheduleRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkScheduleRecordDto>> CreateWorkSchedule(
        [FromBody] SaveWorkScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceSetupService.CreateWorkScheduleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkScheduleById), new { workScheduleId = record.Id }, record);
    }

    [HttpPut("work-schedules/{workScheduleId:guid}")]
    [ProducesResponseType<WorkScheduleRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkScheduleRecordDto>> UpdateWorkSchedule(
        Guid workScheduleId,
        [FromBody] SaveWorkScheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.UpdateWorkScheduleAsync(workScheduleId, request, cancellationToken));
    }

    [HttpDelete("work-schedules/{workScheduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWorkSchedule(Guid workScheduleId, CancellationToken cancellationToken)
    {
        await _attendanceSetupService.DeleteWorkScheduleAsync(workScheduleId, cancellationToken);
        return NoContent();
    }

    [HttpGet("shifts")]
    [ProducesResponseType<PagedResultDto<ShiftRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ShiftRecordDto>>> GetShifts(
        [FromQuery] ShiftListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetShiftsAsync(query, cancellationToken));
    }

    [HttpGet("shifts/{shiftId:guid}")]
    [ProducesResponseType<ShiftRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftRecordDto>> GetShiftById(Guid shiftId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetShiftByIdAsync(shiftId, cancellationToken));
    }

    [HttpPost("shifts")]
    [ProducesResponseType<ShiftRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ShiftRecordDto>> CreateShift(
        [FromBody] SaveShiftRequestDto request,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceSetupService.CreateShiftAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetShiftById), new { shiftId = record.Id }, record);
    }

    [HttpPut("shifts/{shiftId:guid}")]
    [ProducesResponseType<ShiftRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShiftRecordDto>> UpdateShift(
        Guid shiftId,
        [FromBody] SaveShiftRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.UpdateShiftAsync(shiftId, request, cancellationToken));
    }

    [HttpDelete("shifts/{shiftId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteShift(Guid shiftId, CancellationToken cancellationToken)
    {
        await _attendanceSetupService.DeleteShiftAsync(shiftId, cancellationToken);
        return NoContent();
    }

    [HttpGet("assignments")]
    [ProducesResponseType<PagedResultDto<EmployeeScheduleAssignmentRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeScheduleAssignmentRecordDto>>> GetAssignments(
        [FromQuery] EmployeeScheduleAssignmentListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetScheduleAssignmentsAsync(query, cancellationToken));
    }

    [HttpGet("assignments/{assignmentId:guid}")]
    [ProducesResponseType<EmployeeScheduleAssignmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeScheduleAssignmentRecordDto>> GetAssignmentById(Guid assignmentId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.GetScheduleAssignmentByIdAsync(assignmentId, cancellationToken));
    }

    [HttpPost("assignments")]
    [ProducesResponseType<EmployeeScheduleAssignmentRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeScheduleAssignmentRecordDto>> CreateAssignment(
        [FromBody] SaveEmployeeScheduleAssignmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var record = await _attendanceSetupService.CreateScheduleAssignmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAssignmentById), new { assignmentId = record.Id }, record);
    }

    [HttpPut("assignments/{assignmentId:guid}")]
    [ProducesResponseType<EmployeeScheduleAssignmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeScheduleAssignmentRecordDto>> UpdateAssignment(
        Guid assignmentId,
        [FromBody] SaveEmployeeScheduleAssignmentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _attendanceSetupService.UpdateScheduleAssignmentAsync(assignmentId, request, cancellationToken));
    }
}
