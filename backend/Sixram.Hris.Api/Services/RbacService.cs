using Sixram.Api.DTOs.Rbac;
using Sixram.Api.Repositories;

namespace Sixram.Api.Services;

public interface IRbacService
{
    Task<RbacSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public class RbacService : IRbacService
{
    private readonly IRbacReadRepository _rbacReadRepository;

    public RbacService(IRbacReadRepository rbacReadRepository)
    {
        _rbacReadRepository = rbacReadRepository;
    }

    public async Task<RbacSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var users = await _rbacReadRepository.GetUsersAsync(cancellationToken);
        var roles = await _rbacReadRepository.GetRolesAsync(cancellationToken);
        var assignments = await _rbacReadRepository.GetAssignmentsAsync(cancellationToken);

        return new RbacSummaryDto
        {
            Users = users
                .Select(user => new RbacUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    IsEnabled = user.IsEnabled,
                    Roles = user.Roles
                })
                .ToArray(),
            Roles = roles
                .Select(role => new DTOs.Roles.RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    UserCount = role.UserCount
                })
                .ToArray(),
            Assignments = assignments
                .Select(assignment => new RbacAssignmentDto
                {
                    UserId = assignment.UserId,
                    UserEmail = assignment.UserEmail,
                    RoleId = assignment.RoleId,
                    RoleName = assignment.RoleName
                })
                .ToArray()
        };
    }
}
