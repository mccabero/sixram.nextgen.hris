using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/employees/{employeeId:guid}/leave")]
public class AdminEmployeeLeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public AdminEmployeeLeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet]
    [ProducesResponseType<EmployeeLeaveProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeLeaveProfileDto>> GetEmployeeLeaveProfile(Guid employeeId, [FromQuery] int? periodYear, CancellationToken cancellationToken)
    {
        return Ok(await _leaveService.GetEmployeeLeaveProfileAsync(employeeId, periodYear, cancellationToken));
    }
}
