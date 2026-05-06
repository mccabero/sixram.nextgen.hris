using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/employees/{employeeId:guid}/payroll")]
public class AdminEmployeePayrollController : ControllerBase
{
    private readonly IPayrollCompensationService _payrollCompensationService;

    public AdminEmployeePayrollController(IPayrollCompensationService payrollCompensationService)
    {
        _payrollCompensationService = payrollCompensationService;
    }

    [HttpGet]
    [ProducesResponseType<EmployeePayrollProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeePayrollProfileDto>> GetProfile(Guid employeeId, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.GetEmployeePayrollProfileAsync(employeeId, cancellationToken));
    }
}
