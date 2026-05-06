using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/leave-types")]
public class AdminLeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public AdminLeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResultDto<LeaveTypeRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<LeaveTypeRecordDto>>> GetLeaveTypes([FromQuery] LeaveTypeListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _leaveTypeService.GetLeaveTypesAsync(query, cancellationToken));
    }

    [HttpGet("{leaveTypeId:guid}")]
    [ProducesResponseType<LeaveTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveTypeRecordDto>> GetLeaveTypeById(Guid leaveTypeId, CancellationToken cancellationToken)
    {
        return Ok(await _leaveTypeService.GetLeaveTypeByIdAsync(leaveTypeId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<LeaveTypeRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaveTypeRecordDto>> CreateLeaveType([FromBody] SaveLeaveTypeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _leaveTypeService.CreateLeaveTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLeaveTypeById), new { leaveTypeId = record.Id }, record);
    }

    [HttpPut("{leaveTypeId:guid}")]
    [ProducesResponseType<LeaveTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveTypeRecordDto>> UpdateLeaveType(Guid leaveTypeId, [FromBody] SaveLeaveTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _leaveTypeService.UpdateLeaveTypeAsync(leaveTypeId, request, cancellationToken));
    }

    [HttpDelete("{leaveTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLeaveType(Guid leaveTypeId, CancellationToken cancellationToken)
    {
        await _leaveTypeService.DeleteLeaveTypeAsync(leaveTypeId, cancellationToken);
        return NoContent();
    }
}
