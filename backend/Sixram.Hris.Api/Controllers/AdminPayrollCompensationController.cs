using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/payroll/compensation")]
public class AdminPayrollCompensationController : ControllerBase
{
    private readonly IPayrollCompensationService _payrollCompensationService;

    public AdminPayrollCompensationController(IPayrollCompensationService payrollCompensationService)
    {
        _payrollCompensationService = payrollCompensationService;
    }

    [HttpGet("profiles")]
    [ProducesResponseType<PagedResultDto<CompensationProfileRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<CompensationProfileRecordDto>>> GetProfiles([FromQuery] CompensationProfileListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.GetCompensationProfilesAsync(query, cancellationToken));
    }

    [HttpPost("profiles")]
    [ProducesResponseType<CompensationProfileRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CompensationProfileRecordDto>> CreateProfile([FromBody] SaveCompensationProfileRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollCompensationService.CreateCompensationProfileAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetProfiles), new { id = record.Id }, record);
    }

    [HttpPut("profiles/{compensationProfileId:guid}")]
    [ProducesResponseType<CompensationProfileRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CompensationProfileRecordDto>> UpdateProfile(Guid compensationProfileId, [FromBody] SaveCompensationProfileRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.UpdateCompensationProfileAsync(compensationProfileId, request, GetActorUserId(), cancellationToken));
    }

    [HttpDelete("profiles/{compensationProfileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProfile(Guid compensationProfileId, CancellationToken cancellationToken)
    {
        await _payrollCompensationService.DeleteCompensationProfileAsync(compensationProfileId, cancellationToken);
        return NoContent();
    }

    [HttpGet("recurring-earnings")]
    [ProducesResponseType<PagedResultDto<EmployeeRecurringEarningRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeRecurringEarningRecordDto>>> GetRecurringEarnings([FromQuery] RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.GetRecurringEarningsAsync(query, cancellationToken));
    }

    [HttpPost("recurring-earnings")]
    [ProducesResponseType<EmployeeRecurringEarningRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeRecurringEarningRecordDto>> CreateRecurringEarning([FromBody] SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollCompensationService.CreateRecurringEarningAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRecurringEarnings), new { id = record.Id }, record);
    }

    [HttpPut("recurring-earnings/{recurringEarningId:guid}")]
    [ProducesResponseType<EmployeeRecurringEarningRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeRecurringEarningRecordDto>> UpdateRecurringEarning(Guid recurringEarningId, [FromBody] SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.UpdateRecurringEarningAsync(recurringEarningId, request, cancellationToken));
    }

    [HttpDelete("recurring-earnings/{recurringEarningId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRecurringEarning(Guid recurringEarningId, CancellationToken cancellationToken)
    {
        await _payrollCompensationService.DeleteRecurringEarningAsync(recurringEarningId, cancellationToken);
        return NoContent();
    }

    [HttpGet("recurring-deductions")]
    [ProducesResponseType<PagedResultDto<EmployeeRecurringDeductionRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeRecurringDeductionRecordDto>>> GetRecurringDeductions([FromQuery] RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.GetRecurringDeductionsAsync(query, cancellationToken));
    }

    [HttpPost("recurring-deductions")]
    [ProducesResponseType<EmployeeRecurringDeductionRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeRecurringDeductionRecordDto>> CreateRecurringDeduction([FromBody] SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollCompensationService.CreateRecurringDeductionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRecurringDeductions), new { id = record.Id }, record);
    }

    [HttpPut("recurring-deductions/{recurringDeductionId:guid}")]
    [ProducesResponseType<EmployeeRecurringDeductionRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeRecurringDeductionRecordDto>> UpdateRecurringDeduction(Guid recurringDeductionId, [FromBody] SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollCompensationService.UpdateRecurringDeductionAsync(recurringDeductionId, request, cancellationToken));
    }

    [HttpDelete("recurring-deductions/{recurringDeductionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRecurringDeduction(Guid recurringDeductionId, CancellationToken cancellationToken)
    {
        await _payrollCompensationService.DeleteRecurringDeductionAsync(recurringDeductionId, cancellationToken);
        return NoContent();
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

