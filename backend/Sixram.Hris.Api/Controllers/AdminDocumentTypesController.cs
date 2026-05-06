using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/document-types")]
public class AdminDocumentTypesController : ControllerBase
{
    private readonly IDocumentTypeService _documentTypeService;

    public AdminDocumentTypesController(IDocumentTypeService documentTypeService)
    {
        _documentTypeService = documentTypeService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResultDto<DocumentTypeRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<DocumentTypeRecordDto>>> GetDocumentTypes([FromQuery] DocumentTypeListQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _documentTypeService.GetDocumentTypesAsync(query, cancellationToken));
    }

    [HttpGet("{documentTypeId:guid}")]
    [ProducesResponseType<DocumentTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentTypeRecordDto>> GetDocumentTypeById(Guid documentTypeId, CancellationToken cancellationToken)
    {
        return Ok(await _documentTypeService.GetDocumentTypeByIdAsync(documentTypeId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<DocumentTypeRecordDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DocumentTypeRecordDto>> CreateDocumentType([FromBody] SaveDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var record = await _documentTypeService.CreateDocumentTypeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDocumentTypeById), new { documentTypeId = record.Id }, record);
    }

    [HttpPut("{documentTypeId:guid}")]
    [ProducesResponseType<DocumentTypeRecordDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentTypeRecordDto>> UpdateDocumentType(Guid documentTypeId, [FromBody] SaveDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _documentTypeService.UpdateDocumentTypeAsync(documentTypeId, request, cancellationToken));
    }

    [HttpDelete("{documentTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocumentType(Guid documentTypeId, CancellationToken cancellationToken)
    {
        await _documentTypeService.DeleteDocumentTypeAsync(documentTypeId, cancellationToken);
        return NoContent();
    }
}
