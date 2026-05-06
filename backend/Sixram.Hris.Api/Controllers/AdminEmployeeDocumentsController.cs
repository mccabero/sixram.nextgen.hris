using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/employees/{employeeId:guid}/documents")]
public class AdminEmployeeDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentService _employeeDocumentService;

    public AdminEmployeeDocumentsController(IEmployeeDocumentService employeeDocumentService)
    {
        _employeeDocumentService = employeeDocumentService;
    }

    [HttpGet]
    [ProducesResponseType<EmployeeDocumentProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentProfileDto>> GetEmployeeDocuments(Guid employeeId, CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.GetEmployeeDocumentProfileAsync(employeeId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<EmployeeDocumentListItemDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeDocumentListItemDto>> CreateDocument(Guid employeeId, [FromForm] SaveEmployeeDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var document = await _employeeDocumentService.CreateDocumentAsync(employeeId, request, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeDocuments), new { employeeId }, document);
    }

    [HttpPut("{documentId:guid}")]
    [ProducesResponseType<EmployeeDocumentListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentListItemDto>> UpdateDocument(Guid employeeId, Guid documentId, [FromBody] UpdateEmployeeDocumentMetadataRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.UpdateDocumentMetadataAsync(employeeId, documentId, request, cancellationToken));
    }

    [HttpPost("{documentId:guid}/replace")]
    [ProducesResponseType<EmployeeDocumentListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentListItemDto>> ReplaceDocument(Guid employeeId, Guid documentId, [FromForm] ReplaceEmployeeDocumentFileRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.ReplaceDocumentFileAsync(employeeId, documentId, request, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
    }

    [HttpPatch("{documentId:guid}/archive")]
    [ProducesResponseType<EmployeeDocumentListItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDocumentListItemDto>> SetArchiveState(Guid employeeId, Guid documentId, [FromBody] SetEmployeeDocumentArchiveStateRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _employeeDocumentService.SetArchiveStateAsync(employeeId, documentId, request, cancellationToken));
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        await _employeeDocumentService.DeleteDocumentAsync(employeeId, documentId, cancellationToken);
        return NoContent();
    }
}
