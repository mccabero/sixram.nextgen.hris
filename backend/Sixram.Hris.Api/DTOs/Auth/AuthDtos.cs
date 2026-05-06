using System.ComponentModel.DataAnnotations;

namespace Sixram.Api.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed class CurrentUserDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public Guid? LinkedEmployeeId { get; init; }

    public string LinkedEmployeeCode { get; init; } = string.Empty;

    public bool HasLinkedEmployee { get; init; }

    public bool IsManager { get; init; }

    public int ManagedEmployeeCount { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTime AccessTokenExpiresAtUtc { get; init; }

    public CurrentUserDto User { get; init; } = new();
}
