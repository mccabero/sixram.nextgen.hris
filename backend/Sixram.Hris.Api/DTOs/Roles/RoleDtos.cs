using System.ComponentModel.DataAnnotations;

namespace Sixram.Api.DTOs.Roles;

public sealed class RoleDto
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int UserCount { get; init; }
}

public sealed class CreateRoleRequestDto
{
    [Required]
    [MaxLength(256)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Role names may contain letters, numbers, periods, underscores, and hyphens only.")]
    public string Name { get; init; } = string.Empty;

    [MaxLength(256)]
    public string Description { get; init; } = string.Empty;
}

public sealed class UpdateRoleRequestDto
{
    [Required]
    [MaxLength(256)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Role names may contain letters, numbers, periods, underscores, and hyphens only.")]
    public string Name { get; init; } = string.Empty;

    [MaxLength(256)]
    public string Description { get; init; } = string.Empty;
}
