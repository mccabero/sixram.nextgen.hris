using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("registry")]
    public async Task<ActionResult<ReportsCenterDto>> GetRegistry(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetRegistryAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("options")]
    public async Task<ActionResult<ReportOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetOptionsAsync(GetActorUserId(), cancellationToken));
    }

    [HttpGet("{reportKey}")]
    public async Task<ActionResult<ReportResultDto>> RunReport(string reportKey, [FromQuery] ReportQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.RunReportAsync(reportKey, query, GetActorUserId(), cancellationToken));
    }

    [HttpGet("{reportKey}/export/csv")]
    public async Task<IActionResult> ExportCsv(string reportKey, [FromQuery] ReportQueryDto query, CancellationToken cancellationToken)
    {
        var file = await _reportsService.ExportCsvAsync(reportKey, query, GetActorUserId(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("saved")]
    public async Task<ActionResult<IReadOnlyList<SavedReportDto>>> GetSavedReports(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetSavedReportsAsync(GetActorUserId(), cancellationToken));
    }

    [HttpPost("saved")]
    public async Task<ActionResult<SavedReportDto>> CreateSavedReport([FromBody] SaveSavedReportDto request, CancellationToken cancellationToken)
    {
        var record = await _reportsService.CreateSavedReportAsync(request, GetActorUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetSavedReports), new { id = record.Id }, record);
    }

    [HttpPut("saved/{savedReportId:guid}")]
    public async Task<ActionResult<SavedReportDto>> UpdateSavedReport(Guid savedReportId, [FromBody] SaveSavedReportDto request, CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.UpdateSavedReportAsync(savedReportId, request, GetActorUserId(), cancellationToken));
    }

    [HttpDelete("saved/{savedReportId:guid}")]
    public async Task<IActionResult> DeleteSavedReport(Guid savedReportId, CancellationToken cancellationToken)
    {
        await _reportsService.DeleteSavedReportAsync(savedReportId, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
