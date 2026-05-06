using Microsoft.AspNetCore.Identity;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Roles;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;
using Sixram.Api.Repositories;

namespace Sixram.Api.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleDto> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateRoleAsync(string roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);
}

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IRbacReadRepository _rbacReadRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        IRbacReadRepository rbacReadRepository,
        ApplicationDbContext dbContext,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _rbacReadRepository = rbacReadRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _rbacReadRepository.GetRolesAsync(cancellationToken);
        return roles.Select(MapRole).ToArray();
    }

    public async Task<RoleDto> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _rbacReadRepository.GetRoleByIdAsync(roleId, cancellationToken);
        return role is null
            ? throw new NotFoundException($"Role '{roleId}' was not found.")
            : MapRole(role);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
        {
            throw new ConflictException($"A role named '{name}' already exists.");
        }

        var role = new ApplicationRole
        {
            Name = name,
            Description = request.Description.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        ThrowIfIdentityFailed(result, "Unable to create the role.");

        _logger.LogInformation("Role {RoleName} created.", name);
        return await GetRoleByIdAsync(role.Id, cancellationToken);
    }

    public async Task<RoleDto> UpdateRoleAsync(string roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId) ?? throw new NotFoundException($"Role '{roleId}' was not found.");
        var desiredName = request.Name.Trim();

        var existingRole = await _roleManager.FindByNameAsync(desiredName);
        if (existingRole is not null && existingRole.Id != roleId)
        {
            throw new ConflictException($"A role named '{desiredName}' already exists.");
        }

        role.Name = desiredName;
        role.Description = request.Description.Trim();
        role.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(role);
        ThrowIfIdentityFailed(result, "Unable to update the role.");

        _logger.LogInformation("Role {RoleId} updated to {RoleName}.", roleId, desiredName);
        return await GetRoleByIdAsync(roleId, cancellationToken);
    }

    public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId) ?? throw new NotFoundException($"Role '{roleId}' was not found.");
        var roleSnapshot = await _rbacReadRepository.GetRoleByIdAsync(roleId, cancellationToken);

        if (roleSnapshot is not null && roleSnapshot.UserCount > 0)
        {
            throw new BadRequestException("You cannot delete a role that is still assigned to one or more users.");
        }

        var result = await _roleManager.DeleteAsync(role);
        ThrowIfIdentityFailed(result, "Unable to delete the role.");

        _logger.LogInformation("Role {RoleId} deleted.", roleId);
    }

    private static RoleDto MapRole(RoleReadModel role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            UserCount = role.UserCount
        };
    }

    private static void ThrowIfIdentityFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        throw new BadRequestException(message, errors);
    }
}
