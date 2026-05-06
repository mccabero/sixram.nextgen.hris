using System.ComponentModel.DataAnnotations;

namespace Sixram.Api.DTOs.Users;

public sealed class UserSummaryDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class CreateUserRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = true;

    public IReadOnlyList<string> RoleNames { get; init; } = Array.Empty<string>();
}

public sealed class UpdateUserRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class SetUserStatusRequestDto
{
    public bool IsEnabled { get; init; }
}

public sealed class SetUserRolesRequestDto
{
    [Required]
    public IReadOnlyList<string> RoleNames { get; init; } = Array.Empty<string>();
}

public sealed class AdminSetPasswordRequestDto
{
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}
