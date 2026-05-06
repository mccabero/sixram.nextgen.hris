using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;

    public ComplianceController(IComplianceService complianceService)
    {
        _complianceService = complianceService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ComplianceSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _complianceService.GetSummaryAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("issues")]
    public async Task<ActionResult<PagedResultDto<ComplianceIssueDto>>> GetIssues([FromQuery] ComplianceIssueQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _complianceService.GetIssuesAsync(query, GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
