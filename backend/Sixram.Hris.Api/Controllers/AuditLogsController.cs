using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetLogs([FromQuery] AuditLogQueryDto query, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetAuditLogsAsync(query, GetActorUserId(), cancellationToken));
    }

    [HttpGet("{auditLogId:guid}")]
    public async Task<ActionResult<AuditLogDto>> GetLogById(Guid auditLogId, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetAuditLogByIdAsync(auditLogId, GetActorUserId(), cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
