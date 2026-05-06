using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.ProvidentFund;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/provident-fund")]
public class ProvidentFundController : ControllerBase
{
    private const string AdminFinanceRoles = $"{SystemRoles.Administrator},{SystemRoles.HumanResources},{SystemRoles.PayrollOfficer}";

    private readonly IProvidentFundService _providentFundService;

    public ProvidentFundController(IProvidentFundService providentFundService)
    {
        _providentFundService = providentFundService;
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("options")]
    [ProducesResponseType<ProvidentFundOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetOptionsAsync(cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("dashboard")]
    [ProducesResponseType<ProvidentFundDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetDashboardAsync(cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("policies")]
    [ProducesResponseType<PagedResultDto<ProvidentFundPolicyRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundPolicyRecordDto>>> GetPolicies([FromQuery] ProvidentFundPolicyListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetPoliciesAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("policies")]
    [ProducesResponseType<ProvidentFundPolicyRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundPolicyRecordDto>> CreatePolicy([FromBody] SaveProvidentFundPolicyRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreatePolicyAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetPolicies), new { id = record.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("policies/{policyId:guid}")]
    [ProducesResponseType<ProvidentFundPolicyRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundPolicyRecordDto>> UpdatePolicy(Guid policyId, [FromBody] SaveProvidentFundPolicyRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.UpdatePolicyAsync(policyId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("vesting-rules")]
    [ProducesResponseType<PagedResultDto<ProvidentFundVestingRuleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundVestingRuleDto>>> GetVestingRules([FromQuery] ProvidentFundVestingRuleListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetVestingRulesAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("vesting-rules")]
    [ProducesResponseType<ProvidentFundVestingRuleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundVestingRuleDto>> CreateVestingRule([FromBody] SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreateVestingRuleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetVestingRules), new { id = record.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("vesting-rules/{vestingRuleId:guid}")]
    [ProducesResponseType<ProvidentFundVestingRuleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundVestingRuleDto>> UpdateVestingRule(Guid vestingRuleId, [FromBody] SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.UpdateVestingRuleAsync(vestingRuleId, request, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpDelete("vesting-rules/{vestingRuleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVestingRule(Guid vestingRuleId, CancellationToken cancellationToken)
    {
        await _providentFundService.DeleteVestingRuleAsync(vestingRuleId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("enrollments")]
    [ProducesResponseType<PagedResultDto<ProvidentFundEnrollmentRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundEnrollmentRecordDto>>> GetEnrollments([FromQuery] ProvidentFundEnrollmentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetEnrollmentsAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("enrollments")]
    [ProducesResponseType<ProvidentFundEnrollmentRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundEnrollmentRecordDto>> CreateEnrollment([FromBody] SaveProvidentFundEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreateEnrollmentAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetEnrollments), new { id = record.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("enrollments/{enrollmentId:guid}")]
    [ProducesResponseType<ProvidentFundEnrollmentRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundEnrollmentRecordDto>> UpdateEnrollment(Guid enrollmentId, [FromBody] SaveProvidentFundEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.UpdateEnrollmentAsync(enrollmentId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("contribution-batches")]
    [ProducesResponseType<PagedResultDto<ProvidentFundContributionBatchSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundContributionBatchSummaryDto>>> GetContributionBatches([FromQuery] ProvidentFundContributionBatchListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetContributionBatchesAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("contribution-batches/{batchId:guid}")]
    [ProducesResponseType<ProvidentFundContributionBatchDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundContributionBatchDetailDto>> GetContributionBatch(Guid batchId, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetContributionBatchByIdAsync(batchId, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("contribution-batches/generate")]
    [ProducesResponseType<ProvidentFundContributionBatchDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundContributionBatchDetailDto>> GenerateContributionBatch([FromBody] GenerateProvidentFundContributionBatchRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.GenerateContributionBatchAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetContributionBatch), new { batchId = record.Batch.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("contribution-batches/{batchId:guid}/review")]
    [ProducesResponseType<ProvidentFundContributionBatchDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundContributionBatchDetailDto>> ReviewContributionBatch(Guid batchId, [FromBody] ProvidentFundContributionBatchActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.ReviewContributionBatchAsync(batchId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("contribution-batches/{batchId:guid}/post")]
    [ProducesResponseType<ProvidentFundContributionBatchDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundContributionBatchDetailDto>> PostContributionBatch(Guid batchId, [FromBody] ProvidentFundContributionBatchActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.PostContributionBatchAsync(batchId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("contribution-batches/{batchId:guid}/cancel")]
    [ProducesResponseType<ProvidentFundContributionBatchDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundContributionBatchDetailDto>> CancelContributionBatch(Guid batchId, [FromBody] ProvidentFundContributionBatchActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.CancelContributionBatchAsync(batchId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("ledger")]
    [ProducesResponseType<PagedResultDto<ProvidentFundLedgerTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundLedgerTransactionDto>>> GetLedger([FromQuery] ProvidentFundLedgerListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetLedgerAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("ledger/{ledgerTransactionId:guid}/reverse")]
    [ProducesResponseType<ProvidentFundLedgerTransactionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundLedgerTransactionDto>> ReverseLedger(Guid ledgerTransactionId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.ReverseLedgerTransactionAsync(ledgerTransactionId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("balances/{employeeId:guid}")]
    [ProducesResponseType<ProvidentFundBalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundBalanceDto>> GetEmployeeBalance(Guid employeeId, [FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetEmployeeBalanceAsync(employeeId, asOfDate, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("withdrawals")]
    [ProducesResponseType<PagedResultDto<ProvidentFundWithdrawalRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundWithdrawalRequestDto>>> GetWithdrawals([FromQuery] ProvidentFundWithdrawalListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetWithdrawalsAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("withdrawals")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> CreateWithdrawal([FromBody] SaveProvidentFundWithdrawalRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreateWithdrawalAsync(request, GetActorUserId(), ownOnly: false, cancellationToken);
        return CreatedAtAction(nameof(GetWithdrawals), new { id = record.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("withdrawals/{withdrawalId:guid}/submit")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> SubmitWithdrawal(Guid withdrawalId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.SubmitWithdrawalAsync(withdrawalId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("withdrawals/{withdrawalId:guid}/approve")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> ApproveWithdrawal(Guid withdrawalId, [FromBody] ProvidentFundWithdrawalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.ApproveWithdrawalAsync(withdrawalId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("withdrawals/{withdrawalId:guid}/reject")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> RejectWithdrawal(Guid withdrawalId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.RejectWithdrawalAsync(withdrawalId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("withdrawals/{withdrawalId:guid}/mark-paid")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> MarkWithdrawalPaid(Guid withdrawalId, [FromBody] ProvidentFundWithdrawalActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.MarkWithdrawalPaidAsync(withdrawalId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("adjustments")]
    [ProducesResponseType<PagedResultDto<ProvidentFundAdjustmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundAdjustmentDto>>> GetAdjustments([FromQuery] ProvidentFundAdjustmentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetAdjustmentsAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPost("adjustments")]
    [ProducesResponseType<ProvidentFundAdjustmentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundAdjustmentDto>> CreateAdjustment([FromBody] SaveProvidentFundAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreateAdjustmentAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetAdjustments), new { id = record.Id }, record);
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("adjustments/{adjustmentId:guid}/approve")]
    [ProducesResponseType<ProvidentFundAdjustmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundAdjustmentDto>> ApproveAdjustment(Guid adjustmentId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.ApproveAdjustmentAsync(adjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("adjustments/{adjustmentId:guid}/reject")]
    [ProducesResponseType<ProvidentFundAdjustmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundAdjustmentDto>> RejectAdjustment(Guid adjustmentId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.RejectAdjustmentAsync(adjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpPut("adjustments/{adjustmentId:guid}/post")]
    [ProducesResponseType<ProvidentFundAdjustmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundAdjustmentDto>> PostAdjustment(Guid adjustmentId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.PostAdjustmentAsync(adjustmentId, request, GetActorUserId(), cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("reports/contributions")]
    public async Task<ActionResult<IReadOnlyList<ProvidentFundContributionReportRowDto>>> GetContributionReport([FromQuery] ProvidentFundReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetContributionReportAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("reports/balances")]
    public async Task<ActionResult<IReadOnlyList<ProvidentFundBalanceReportRowDto>>> GetBalanceReport([FromQuery] ProvidentFundReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetBalanceReportAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("reports/withdrawals")]
    public async Task<ActionResult<IReadOnlyList<ProvidentFundWithdrawalReportRowDto>>> GetWithdrawalReport([FromQuery] ProvidentFundReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetWithdrawalReportAsync(query, cancellationToken));
    }

    [Authorize(Roles = AdminFinanceRoles)]
    [HttpGet("reports/ledger")]
    public async Task<ActionResult<IReadOnlyList<ProvidentFundLedgerTransactionDto>>> GetLedgerReport([FromQuery] ProvidentFundReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetLedgerReportAsync(query, cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
