using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/payroll/setup")]
public class AdminPayrollSetupController : ControllerBase
{
    private readonly IPayrollSetupService _payrollSetupService;

    public AdminPayrollSetupController(IPayrollSetupService payrollSetupService)
    {
        _payrollSetupService = payrollSetupService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<PayrollSetupSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollSetupSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("settings")]
    [ProducesResponseType<PayrollSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetSettingsAsync(cancellationToken));
    }

    [HttpPut("settings")]
    [ProducesResponseType<PayrollSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollSettingsDto>> UpdateSettings([FromBody] PayrollSettingsDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateSettingsAsync(request, cancellationToken));
    }

    [HttpGet("pay-period-templates")]
    [ProducesResponseType<PagedResultDto<PayPeriodTemplateRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PayPeriodTemplateRecordDto>>> GetPayPeriodTemplates([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetPayPeriodTemplatesAsync(query, cancellationToken));
    }

    [HttpPost("pay-period-templates")]
    [ProducesResponseType<PayPeriodTemplateRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PayPeriodTemplateRecordDto>> CreatePayPeriodTemplate([FromBody] SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreatePayPeriodTemplateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPayPeriodTemplates), new { id = record.Id }, record);
    }

    [HttpPut("pay-period-templates/{payPeriodTemplateId:guid}")]
    [ProducesResponseType<PayPeriodTemplateRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayPeriodTemplateRecordDto>> UpdatePayPeriodTemplate(Guid payPeriodTemplateId, [FromBody] SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdatePayPeriodTemplateAsync(payPeriodTemplateId, request, cancellationToken));
    }

    [HttpDelete("pay-period-templates/{payPeriodTemplateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePayPeriodTemplate(Guid payPeriodTemplateId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeletePayPeriodTemplateAsync(payPeriodTemplateId, cancellationToken);
        return NoContent();
    }

    [HttpGet("earning-types")]
    [ProducesResponseType<PagedResultDto<EarningTypeRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EarningTypeRecordDto>>> GetEarningTypes([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetEarningTypesAsync(query, cancellationToken));
    }

    [HttpPost("earning-types")]
    [ProducesResponseType<EarningTypeRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EarningTypeRecordDto>> CreateEarningType([FromBody] SaveEarningTypeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreateEarningTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEarningTypes), new { id = record.Id }, record);
    }

    [HttpPut("earning-types/{earningTypeId:guid}")]
    [ProducesResponseType<EarningTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EarningTypeRecordDto>> UpdateEarningType(Guid earningTypeId, [FromBody] SaveEarningTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateEarningTypeAsync(earningTypeId, request, cancellationToken));
    }

    [HttpDelete("earning-types/{earningTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEarningType(Guid earningTypeId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeleteEarningTypeAsync(earningTypeId, cancellationToken);
        return NoContent();
    }

    [HttpGet("deduction-types")]
    [ProducesResponseType<PagedResultDto<DeductionTypeRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<DeductionTypeRecordDto>>> GetDeductionTypes([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetDeductionTypesAsync(query, cancellationToken));
    }

    [HttpPost("deduction-types")]
    [ProducesResponseType<DeductionTypeRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DeductionTypeRecordDto>> CreateDeductionType([FromBody] SaveDeductionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreateDeductionTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDeductionTypes), new { id = record.Id }, record);
    }

    [HttpPut("deduction-types/{deductionTypeId:guid}")]
    [ProducesResponseType<DeductionTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DeductionTypeRecordDto>> UpdateDeductionType(Guid deductionTypeId, [FromBody] SaveDeductionTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateDeductionTypeAsync(deductionTypeId, request, cancellationToken));
    }

    [HttpDelete("deduction-types/{deductionTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDeductionType(Guid deductionTypeId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeleteDeductionTypeAsync(deductionTypeId, cancellationToken);
        return NoContent();
    }

    [HttpGet("contribution-types")]
    [ProducesResponseType<PagedResultDto<ContributionTypeRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ContributionTypeRecordDto>>> GetContributionTypes([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetContributionTypesAsync(query, cancellationToken));
    }

    [HttpPost("contribution-types")]
    [ProducesResponseType<ContributionTypeRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ContributionTypeRecordDto>> CreateContributionType([FromBody] SaveContributionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreateContributionTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetContributionTypes), new { id = record.Id }, record);
    }

    [HttpPut("contribution-types/{contributionTypeId:guid}")]
    [ProducesResponseType<ContributionTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContributionTypeRecordDto>> UpdateContributionType(Guid contributionTypeId, [FromBody] SaveContributionTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateContributionTypeAsync(contributionTypeId, request, cancellationToken));
    }

    [HttpDelete("contribution-types/{contributionTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContributionType(Guid contributionTypeId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeleteContributionTypeAsync(contributionTypeId, cancellationToken);
        return NoContent();
    }

    [HttpGet("contribution-tables")]
    [ProducesResponseType<PagedResultDto<GovernmentContributionTableRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<GovernmentContributionTableRecordDto>>> GetContributionTables([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetGovernmentContributionTablesAsync(query, cancellationToken));
    }

    [HttpPost("contribution-tables")]
    [ProducesResponseType<GovernmentContributionTableRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GovernmentContributionTableRecordDto>> CreateContributionTable([FromBody] SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreateGovernmentContributionTableAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetContributionTables), new { id = record.Id }, record);
    }

    [HttpPut("contribution-tables/{contributionTableId:guid}")]
    [ProducesResponseType<GovernmentContributionTableRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GovernmentContributionTableRecordDto>> UpdateContributionTable(Guid contributionTableId, [FromBody] SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateGovernmentContributionTableAsync(contributionTableId, request, cancellationToken));
    }

    [HttpDelete("contribution-tables/{contributionTableId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContributionTable(Guid contributionTableId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeleteGovernmentContributionTableAsync(contributionTableId, cancellationToken);
        return NoContent();
    }

    [HttpGet("tax-tables")]
    [ProducesResponseType<PagedResultDto<TaxTableRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TaxTableRecordDto>>> GetTaxTables([FromQuery] PayrollSetupListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetTaxTablesAsync(query, cancellationToken));
    }

    [HttpPost("tax-tables")]
    [ProducesResponseType<TaxTableRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TaxTableRecordDto>> CreateTaxTable([FromBody] SaveTaxTableRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollSetupService.CreateTaxTableAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTaxTables), new { id = record.Id }, record);
    }

    [HttpPut("tax-tables/{taxTableId:guid}")]
    [ProducesResponseType<TaxTableRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TaxTableRecordDto>> UpdateTaxTable(Guid taxTableId, [FromBody] SaveTaxTableRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.UpdateTaxTableAsync(taxTableId, request, cancellationToken));
    }

    [HttpDelete("tax-tables/{taxTableId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTaxTable(Guid taxTableId, CancellationToken cancellationToken)
    {
        await _payrollSetupService.DeleteTaxTableAsync(taxTableId, cancellationToken);
        return NoContent();
    }
}

