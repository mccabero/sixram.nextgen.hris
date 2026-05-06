using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/documents")]
public class AdminDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentService _employeeDocumentService;

    public AdminDocumentsController(IEmployeeDocumentService employeeDocumentService)
    {
        _employeeDocumentService = employeeDocumentService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<DocumentComplianceSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentComplianceSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.GetDashboardSummaryAsync(cancellationToken));
    }

    [HttpGet("options")]
    [ProducesResponseType<EmployeeDocumentListOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentListOptionsDto>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.GetListOptionsAsync(cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType<PagedResultDto<EmployeeDocumentListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<EmployeeDocumentListItemDto>>> GetDocuments([FromQuery] EmployeeDocumentListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.GetDocumentsAsync(query, cancellationToken));
    }

    [HttpGet("{documentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var file = await _employeeDocumentService.GetDocumentContentAsync(documentId, cancellationToken);
        return File(file.Content, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
    }
}
