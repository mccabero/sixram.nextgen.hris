using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.ProvidentFund;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/provident-fund")]
public class MyProvidentFundController : ControllerBase
{
    private readonly IProvidentFundService _providentFundService;

    public MyProvidentFundController(IProvidentFundService providentFundService)
    {
        _providentFundService = providentFundService;
    }

    [HttpGet]
    [ProducesResponseType<ProvidentFundBalanceDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundBalanceDto>> GetMyFund(CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.GetMyBalanceAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("ledger")]
    [ProducesResponseType<PagedResultDto<ProvidentFundLedgerTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundLedgerTransactionDto>>> GetMyLedger([FromQuery] ProvidentFundLedgerListQueryDto query, CancellationToken cancellationToken)
    {
        var balance = await _providentFundService.GetMyBalanceAsync(GetActorUserId(), cancellationToken);
        var scopedQuery = new ProvidentFundLedgerListQueryDto
        {
            EmployeeId = balance.EmployeeId,
            PolicyId = query.PolicyId,
            TransactionType = query.TransactionType,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            Descending = query.Descending,
            Search = query.Search,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Ok(await _providentFundService.GetLedgerAsync(scopedQuery, cancellationToken));
    }

    [HttpGet("withdrawals")]
    [ProducesResponseType<PagedResultDto<ProvidentFundWithdrawalRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProvidentFundWithdrawalRequestDto>>> GetMyWithdrawals([FromQuery] ProvidentFundWithdrawalListQueryDto query, CancellationToken cancellationToken)
    {
        var balance = await _providentFundService.GetMyBalanceAsync(GetActorUserId(), cancellationToken);
        var scopedQuery = new ProvidentFundWithdrawalListQueryDto
        {
            EmployeeId = balance.EmployeeId,
            Status = query.Status,
            WithdrawalType = query.WithdrawalType,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            Descending = query.Descending,
            Search = query.Search,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Ok(await _providentFundService.GetWithdrawalsAsync(scopedQuery, cancellationToken));
    }

    [HttpPost("withdrawals")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> CreateMyWithdrawal([FromBody] SaveProvidentFundWithdrawalRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _providentFundService.CreateWithdrawalAsync(request, GetActorUserId(), ownOnly: true, cancellationToken);
        return CreatedAtAction(nameof(GetMyWithdrawals), new { id = record.Id }, record);
    }

    [HttpPut("withdrawals/{withdrawalId:guid}/submit")]
    [ProducesResponseType<ProvidentFundWithdrawalRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidentFundWithdrawalRequestDto>> SubmitMyWithdrawal(Guid withdrawalId, [FromBody] ProvidentFundActionRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _providentFundService.SubmitWithdrawalAsync(withdrawalId, request, GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
