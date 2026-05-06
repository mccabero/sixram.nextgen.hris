using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Rbac;
using Sixram.Api.DTOs.Users;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/rbac")]
public class AdminRbacController : ControllerBase
{
    private readonly IRbacService _rbacService;
    private readonly IAdminUserService _adminUserService;

    public AdminRbacController(IRbacService rbacService, IAdminUserService adminUserService)
    {
        _rbacService = rbacService;
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [ProducesResponseType<RbacSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RbacSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _rbacService.GetSummaryAsync(cancellationToken));
    }

    [HttpPut("users/{userId}/roles")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> SetUserRoles(
        string userId,
        [FromBody] SetUserRolesRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _adminUserService.SetUserRolesAsync(userId, request, actorUserId, cancellationToken));
    }
}
