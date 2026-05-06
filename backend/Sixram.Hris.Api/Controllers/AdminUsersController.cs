using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Users;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _adminUserService.GetUsersAsync(cancellationToken));
    }

    [HttpGet("{userId}")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> GetUserById(string userId, CancellationToken cancellationToken)
    {
        return Ok(await _adminUserService.GetUserByIdAsync(userId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserSummaryDto>> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
    }

    [HttpPut("{userId}")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> UpdateUser(string userId, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _adminUserService.UpdateUserAsync(userId, request, cancellationToken));
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken cancellationToken)
    {
        await _adminUserService.DeleteUserAsync(userId, GetActorUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{userId}/status")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> SetUserStatus(
        string userId,
        [FromBody] SetUserStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _adminUserService.SetUserStatusAsync(userId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("{userId}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        string userId,
        [FromBody] AdminSetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _adminUserService.ResetPasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId}/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        string userId,
        [FromBody] AdminSetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _adminUserService.ChangePasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{userId}/roles")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> SetRoles(
        string userId,
        [FromBody] SetUserRolesRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _adminUserService.SetUserRolesAsync(userId, request, GetActorUserId(), cancellationToken));
    }

    [HttpPost("{userId}/roles/{roleName}")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> AssignRole(string userId, string roleName, CancellationToken cancellationToken)
    {
        var currentUser = await _adminUserService.GetUserByIdAsync(userId, cancellationToken);
        var updatedRoles = currentUser.Roles
            .Append(roleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(await _adminUserService.SetUserRolesAsync(
            userId,
            new SetUserRolesRequestDto { RoleNames = updatedRoles },
            GetActorUserId(),
            cancellationToken));
    }

    [HttpDelete("{userId}/roles/{roleName}")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryDto>> RemoveRole(string userId, string roleName, CancellationToken cancellationToken)
    {
        var currentUser = await _adminUserService.GetUserByIdAsync(userId, cancellationToken);
        var updatedRoles = currentUser.Roles
            .Where(role => !string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Ok(await _adminUserService.SetUserRolesAsync(
            userId,
            new SetUserRolesRequestDto { RoleNames = updatedRoles },
            GetActorUserId(),
            cancellationToken));
    }

    private string? GetActorUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
