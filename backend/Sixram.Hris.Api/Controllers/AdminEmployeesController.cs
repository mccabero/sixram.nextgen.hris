using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Employees;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/employees")]
public class AdminEmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public AdminEmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResultDto<EmployeeSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeSummaryDto>>> GetEmployees([FromQuery] EmployeeListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.GetEmployeesAsync(query, cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<EmployeeEditorOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeEditorOptionsDto>> GetOptions([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.GetEditorOptionsAsync(employeeId, cancellationToken));
    }

    [HttpGet("{employeeId:guid}")]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDetailDto>> GetEmployeeById(Guid employeeId, CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.GetEmployeeByIdAsync(employeeId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeDetailDto>> CreateEmployee([FromBody] SaveEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeById), new { employeeId = employee.Id }, employee);
    }

    [HttpPut("{employeeId:guid}")]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDetailDto>> UpdateEmployee(Guid employeeId, [FromBody] SaveEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.UpdateEmployeeAsync(employeeId, request, cancellationToken));
    }

    [HttpDelete("{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEmployee(Guid employeeId, CancellationToken cancellationToken)
    {
        await _employeeService.DeleteEmployeeAsync(employeeId, cancellationToken);
        return NoContent();
    }
}
