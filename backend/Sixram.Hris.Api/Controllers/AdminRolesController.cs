using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sixram.Api.Constants;
using Sixram.Api.DTOs.Roles;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Authorize(Roles = SystemRoles.Administrator)]
[Route("api/admin/roles")]
public class AdminRolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public AdminRolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetRolesAsync(cancellationToken));
    }

    [HttpGet("{roleId}")]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> GetRoleById(string roleId, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetRoleByIdAsync(roleId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _roleService.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRoleById), new { roleId = role.Id }, role);
    }

    [HttpPut("{roleId}")]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleDto>> UpdateRole(string roleId, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.UpdateRoleAsync(roleId, request, cancellationToken));
    }

    [HttpDelete("{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken cancellationToken)
    {
        await _roleService.DeleteRoleAsync(roleId, cancellationToken);
        return NoContent();
    }
}
