using Sixram.Api.DTOs.Roles;

namespace Sixram.Api.DTOs.Rbac;

public sealed class RbacUserDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class RbacAssignmentDto
{
    public string UserId { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;

    public string RoleId { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;
}

public sealed class RbacSummaryDto
{
    public IReadOnlyList<RbacUserDto> Users { get; init; } = Array.Empty<RbacUserDto>();

    public IReadOnlyList<RoleDto> Roles { get; init; } = Array.Empty<RoleDto>();

    public IReadOnlyList<RbacAssignmentDto> Assignments { get; init; } = Array.Empty<RbacAssignmentDto>();
}
