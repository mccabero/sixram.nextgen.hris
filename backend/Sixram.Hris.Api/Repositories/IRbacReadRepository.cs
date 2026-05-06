namespace Sixram.Api.Repositories;

public sealed record UserReadModel(
    string Id,
    string Email,
    string DisplayName,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    List<string> Roles);

public sealed record RoleReadModel(
    string Id,
    string Name,
    string Description,
    int UserCount);

public sealed record UserRoleAssignmentReadModel(
    string UserId,
    string UserEmail,
    string RoleId,
    string RoleName);

public interface IRbacReadRepository
{
    Task<IReadOnlyList<UserReadModel>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserReadModel?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleReadModel>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleReadModel?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleAssignmentReadModel>> GetAssignmentsAsync(CancellationToken cancellationToken = default);
}
