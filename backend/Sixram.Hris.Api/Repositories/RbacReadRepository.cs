using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;

namespace Sixram.Api.Repositories;

public class RbacReadRepository : IRbacReadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RbacReadRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserReadModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new UserReadModel(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsEnabled,
                user.CreatedAtUtc,
                user.UserRoles
                    .OrderBy(userRole => userRole.Role!.Name)
                    .Select(userRole => userRole.Role!.Name ?? string.Empty)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserReadModel?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserReadModel(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsEnabled,
                user.CreatedAtUtc,
                user.UserRoles
                    .OrderBy(userRole => userRole.Role!.Name)
                    .Select(userRole => userRole.Role!.Name ?? string.Empty)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleReadModel>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleReadModel(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.UserRoles.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleReadModel?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role => new RoleReadModel(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.UserRoles.Count))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleAssignmentReadModel>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .OrderBy(userRole => userRole.User!.Email)
            .ThenBy(userRole => userRole.Role!.Name)
            .Select(userRole => new UserRoleAssignmentReadModel(
                userRole.UserId,
                userRole.User!.Email ?? string.Empty,
                userRole.RoleId,
                userRole.Role!.Name ?? string.Empty))
            .ToListAsync(cancellationToken);
    }
}
