using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Users;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;
using Sixram.Api.Repositories;

namespace Sixram.Api.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> UpdateUserAsync(string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> SetUserStatusAsync(string userId, SetUserStatusRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> SetUserRolesAsync(string userId, SetUserRolesRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(string userId, AdminSetPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(string userId, AdminSetPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, string? actorUserId, CancellationToken cancellationToken = default);
}

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IRbacReadRepository _rbacReadRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IRbacReadRepository rbacReadRepository,
        ApplicationDbContext dbContext,
        ILogger<AdminUserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _rbacReadRepository = rbacReadRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _rbacReadRepository.GetUsersAsync(cancellationToken);
        return users.Select(MapUser).ToArray();
    }

    public async Task<UserSummaryDto> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _rbacReadRepository.GetUserByIdAsync(userId, cancellationToken);
        return user is null
            ? throw new NotFoundException($"User '{userId}' was not found.")
            : MapUser(user);
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var roles = await NormalizeAndValidateRolesAsync(
            request.RoleNames.Count == 0 ? [SystemRoles.User] : request.RoleNames,
            cancellationToken);

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true,
            IsEnabled = request.IsEnabled,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        ThrowIfIdentityFailed(createResult, "Unable to create the user.");

        if (roles.Count > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(user, roles);
            ThrowIfIdentityFailed(roleResult, "Unable to assign roles to the user.");
        }

        _logger.LogInformation("User {Email} created with roles {Roles}.", user.Email, string.Join(", ", roles));
        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserSummaryDto> UpdateUserAsync(string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();

        var emailOwner = await _userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != userId)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        user.Email = email;
        user.UserName = email;
        user.DisplayName = request.DisplayName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        ThrowIfIdentityFailed(updateResult, "Unable to update the user.");

        _logger.LogInformation("User {UserId} updated.", userId);
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<UserSummaryDto> SetUserStatusAsync(string userId, SetUserStatusRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);

        if (!request.IsEnabled && string.Equals(actorUserId, userId, StringComparison.Ordinal))
        {
            throw new BadRequestException("You cannot disable your own account.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await EnsureAdministratorSafetyAsync(user, request.IsEnabled, currentRoles.ToArray(), cancellationToken);

        user.IsEnabled = request.IsEnabled;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        ThrowIfIdentityFailed(result, "Unable to update the user status.");

        _logger.LogInformation("User {UserId} status changed to {IsEnabled}.", userId, request.IsEnabled);
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<UserSummaryDto> SetUserRolesAsync(string userId, SetUserRolesRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);
        var normalizedRoles = await NormalizeAndValidateRolesAsync(request.RoleNames, cancellationToken);

        await EnsureAdministratorSafetyAsync(user, user.IsEnabled, normalizedRoles, cancellationToken);

        if (!normalizedRoles.Contains(SystemRoles.Administrator, StringComparer.OrdinalIgnoreCase) &&
            string.Equals(actorUserId, userId, StringComparison.Ordinal))
        {
            var actorRoles = await _userManager.GetRolesAsync(user);
            if (actorRoles.Contains(SystemRoles.Administrator, StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException("You cannot remove your own Administrator role from this screen.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = normalizedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToRemove = currentRoles.Except(normalizedRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            ThrowIfIdentityFailed(removeResult, "Unable to remove one or more roles from the user.");
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            ThrowIfIdentityFailed(addResult, "Unable to assign one or more roles to the user.");
        }

        _logger.LogInformation("User {UserId} roles updated to {Roles}.", userId, string.Join(", ", normalizedRoles));
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task ResetPasswordAsync(string userId, AdminSetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        ThrowIfIdentityFailed(result, "Unable to reset the user password.");
        _logger.LogInformation("Password reset for user {UserId}.", userId);
    }

    public async Task ChangePasswordAsync(string userId, AdminSetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        ThrowIfIdentityFailed(result, "Unable to change the user password.");
        _logger.LogInformation("Password changed for user {UserId}.", userId);
    }

    public async Task DeleteUserAsync(string userId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, cancellationToken);

        if (string.Equals(actorUserId, userId, StringComparison.Ordinal))
        {
            throw new BadRequestException("You cannot delete your own account.");
        }

        await EnsureAdministratorSafetyAsync(user, false, Array.Empty<string>(), cancellationToken);

        var result = await _userManager.DeleteAsync(user);
        ThrowIfIdentityFailed(result, "Unable to delete the user.");

        _logger.LogInformation("User {UserId} deleted.", userId);
    }

    private async Task<ApplicationUser> GetUserEntityAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
        return user ?? throw new NotFoundException($"User '{userId}' was not found.");
    }

    private async Task<IReadOnlyList<string>> NormalizeAndValidateRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var normalizedRoles = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            return normalizedRoles;
        }

        var existingRoles = await _roleManager.Roles
            .Where(role => normalizedRoles.Contains(role.Name!))
            .Select(role => role.Name!)
            .ToListAsync(cancellationToken);

        var missingRoles = normalizedRoles
            .Except(existingRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingRoles.Length > 0)
        {
            throw new BadRequestException($"Unknown roles: {string.Join(", ", missingRoles)}.");
        }

        return normalizedRoles;
    }

    private async Task EnsureAdministratorSafetyAsync(
        ApplicationUser user,
        bool willRemainEnabled,
        IReadOnlyCollection<string> finalRoles,
        CancellationToken cancellationToken)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        var isAdministrator = currentRoles.Contains(SystemRoles.Administrator, StringComparer.OrdinalIgnoreCase);

        if (!isAdministrator)
        {
            return;
        }

        var willRemainAdministrator = finalRoles.Contains(SystemRoles.Administrator, StringComparer.OrdinalIgnoreCase);
        if (willRemainEnabled && willRemainAdministrator)
        {
            return;
        }

        var enabledAdministratorCount = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(
                account => account.IsEnabled &&
                           account.UserRoles.Any(userRole => userRole.Role!.Name == SystemRoles.Administrator),
                cancellationToken);

        if (enabledAdministratorCount <= 1)
        {
            throw new BadRequestException("At least one enabled administrator must remain in the system.");
        }
    }

    private static UserSummaryDto MapUser(UserReadModel user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsEnabled = user.IsEnabled,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = user.Roles
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
