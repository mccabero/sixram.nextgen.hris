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
[Route("api/admin/payroll")]
public class AdminPayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly IPayrollSetupService _payrollSetupService;

    public AdminPayrollController(IPayrollService payrollService, IPayrollSetupService payrollSetupService)
    {
        _payrollService = payrollService;
        _payrollSetupService = payrollSetupService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<PayrollDashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetDashboardSummaryAsync(cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<PayrollOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _payrollSetupService.GetOptionsAsync(cancellationToken));
    }

    [HttpGet("pay-periods")]
    [ProducesResponseType<PagedResultDto<PayPeriodRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PayPeriodRecordDto>>> GetPayPeriods([FromQuery] PayPeriodListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetPayPeriodsAsync(query, cancellationToken));
    }

    [HttpPost("pay-periods")]
    [ProducesResponseType<PayPeriodRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PayPeriodRecordDto>> CreatePayPeriod([FromBody] SavePayPeriodRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollService.CreatePayPeriodAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPayPeriods), new { id = record.Id }, record);
    }

    [HttpPut("pay-periods/{payPeriodId:guid}")]
    [ProducesResponseType<PayPeriodRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayPeriodRecordDto>> UpdatePayPeriod(Guid payPeriodId, [FromBody] SavePayPeriodRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.UpdatePayPeriodAsync(payPeriodId, request, cancellationToken));
    }

    [HttpDelete("pay-periods/{payPeriodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePayPeriod(Guid payPeriodId, CancellationToken cancellationToken)
    {
        await _payrollService.DeletePayPeriodAsync(payPeriodId, cancellationToken);
        return NoContent();
    }

    [HttpGet("runs")]
    [ProducesResponseType<PagedResultDto<PayrollRunSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PayrollRunSummaryDto>>> GetRuns([FromQuery] PayrollRunListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetPayrollRunsAsync(query, cancellationToken));
    }

    [HttpGet("runs/{payrollRunId:guid}")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> GetRunById(Guid payrollRunId, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetPayrollRunByIdAsync(payrollRunId, cancellationToken));
    }

    [HttpPost("runs/generate")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PayrollRunDetailDto>> GenerateRun([FromBody] GeneratePayrollRunRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollService.GeneratePayrollRunAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetRunById), new { payrollRunId = record.Run.Id }, record);
    }

    [HttpPost("runs/{payrollRunId:guid}/recalculate")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> RecalculateRun(Guid payrollRunId, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.RecalculatePayrollRunAsync(payrollRunId, GetActorUserId(), cancellationToken));
    }

    [HttpPost("runs/{payrollRunId:guid}/submit-review")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> SubmitRunForReview(Guid payrollRunId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.SubmitPayrollRunForReviewAsync(payrollRunId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("runs/{payrollRunId:guid}/approve")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> ApproveRun(Guid payrollRunId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.ApprovePayrollRunAsync(payrollRunId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("runs/{payrollRunId:guid}/mark-paid")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> MarkRunPaid(Guid payrollRunId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.MarkPayrollRunAsPaidAsync(payrollRunId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("runs/{payrollRunId:guid}/cancel")]
    [ProducesResponseType<PayrollRunDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollRunDetailDto>> CancelRun(Guid payrollRunId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.CancelPayrollRunAsync(payrollRunId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("adjustments")]
    [ProducesResponseType<PagedResultDto<PayrollAdjustmentRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PayrollAdjustmentRecordDto>>> GetAdjustments([FromQuery] PayrollAdjustmentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetPayrollAdjustmentsAsync(query, cancellationToken));
    }

    [HttpPost("adjustments")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> CreateAdjustment([FromBody] SavePayrollAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _payrollService.CreatePayrollAdjustmentAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetAdjustments), new { id = record.Id }, record);
    }

    [HttpPut("adjustments/{payrollAdjustmentId:guid}")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> UpdateAdjustment(Guid payrollAdjustmentId, [FromBody] SavePayrollAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.UpdatePayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [HttpDelete("adjustments/{payrollAdjustmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAdjustment(Guid payrollAdjustmentId, CancellationToken cancellationToken)
    {
        await _payrollService.DeletePayrollAdjustmentAsync(payrollAdjustmentId, cancellationToken);
        return NoContent();
    }

    [HttpPost("adjustments/{payrollAdjustmentId:guid}/approve")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> ApproveAdjustment(Guid payrollAdjustmentId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.ApprovePayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("adjustments/{payrollAdjustmentId:guid}/reject")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> RejectAdjustment(Guid payrollAdjustmentId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.RejectPayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("adjustments/{payrollAdjustmentId:guid}/cancel")]
    [ProducesResponseType<PayrollAdjustmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollAdjustmentRecordDto>> CancelAdjustment(Guid payrollAdjustmentId, [FromBody] PayrollRunActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.CancelPayrollAdjustmentAsync(payrollAdjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [HttpGet("reports")]
    [ProducesResponseType<PayrollReportsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollReportsDto>> GetReports([FromQuery] PayrollReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetReportsAsync(query, cancellationToken));
    }

    [HttpGet("payslips/{payrollRunItemId:guid}")]
    [ProducesResponseType<PayslipDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayslipDto>> GetPayslip(Guid payrollRunItemId, CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetPayslipAsync(payrollRunItemId, cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

