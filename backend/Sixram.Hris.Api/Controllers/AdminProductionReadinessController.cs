using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Operations;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/production-readiness")]
public class AdminProductionReadinessController : ControllerBase
{
    private readonly IProductionReadinessService _productionReadinessService;

    public AdminProductionReadinessController(IProductionReadinessService productionReadinessService)
    {
        _productionReadinessService = productionReadinessService;
    }

    [HttpGet]
    [ProducesResponseType<ProductionReadinessOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductionReadinessOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await _productionReadinessService.GetOverviewAsync(cancellationToken));
    }

    [HttpPost("imports/preview")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DataImportPreviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DataImportPreviewDto>> PreviewImport([FromForm] PreviewDataImportRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _productionReadinessService.PreviewImportAsync(request, cancellationToken));
    }

    [HttpPost("imports/apply")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DataImportApplyResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DataImportApplyResultDto>> ApplyImport([FromForm] ApplyDataImportRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _productionReadinessService.ApplyImportAsync(request, GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
