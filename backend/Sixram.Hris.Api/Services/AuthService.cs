using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Auth;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;
using Sixram.Api.Repositories;

namespace Sixram.Api.Services;

public sealed record AuthSessionResult(AuthResponseDto Response, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

public interface IAuthService
{
    Task<AuthSessionResult> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthSessionResult?> RefreshAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<AuthSessionResult> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedApiException("Invalid email or password.");
        }

        if (!user.IsEnabled)
        {
            throw new ForbiddenApiException("The user account is disabled.");
        }

        await RevokeActiveRefreshTokensAsync(user.Id, ipAddress, cancellationToken);

        var authSession = await CreateSessionAsync(user, ipAddress, cancellationToken);
        _logger.LogInformation("User {Email} signed in.", user.Email);

        return authSession;
    }

    public async Task<AuthSessionResult?> RefreshAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var hashedToken = _tokenService.HashRefreshToken(refreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(hashedToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive || existingToken.User is null)
        {
            return null;
        }

        if (!existingToken.User.IsEnabled)
        {
            if (existingToken.RevokedAtUtc is null)
            {
                existingToken.RevokedAtUtc = DateTime.UtcNow;
                existingToken.RevokedByIp = ipAddress;
                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;

        var replacement = _tokenService.CreateRefreshToken();
        existingToken.ReplacedByTokenHash = replacement.TokenHash;

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = replacement.TokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = replacement.ExpiresAtUtc,
            CreatedByIp = ipAddress
        }, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var accessToken = await _tokenService.CreateAccessTokenAsync(existingToken.User, cancellationToken);
        var userDto = await MapCurrentUserAsync(existingToken.User, cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {Email}.", existingToken.User.Email);

        return new AuthSessionResult(
            new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                User = userDto
            },
            replacement.PlainTextToken,
            replacement.ExpiresAtUtc);
    }

    public async Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hashedToken = _tokenService.HashRefreshToken(refreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(hashedToken, cancellationToken);

        if (existingToken is null || existingToken.RevokedAtUtc is not null)
        {
            return;
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Refresh token revoked during logout for user id {UserId}.", existingToken.UserId);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedApiException("The current access token is invalid.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedApiException("The user account no longer exists.");
        }

        if (!user.IsEnabled)
        {
            throw new ForbiddenApiException("The user account is disabled.");
        }

        return await MapCurrentUserAsync(user, cancellationToken);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.CreateAccessTokenAsync(user, cancellationToken);
        var refreshToken = _tokenService.CreateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
            CreatedByIp = ipAddress
        }, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
                User = await MapCurrentUserAsync(user, cancellationToken)
            },
            refreshToken.PlainTextToken,
            refreshToken.ExpiresAtUtc);
    }

    private async Task RevokeActiveRefreshTokensAsync(string userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAtUtc = DateTime.UtcNow;
            activeToken.RevokedByIp = ipAddress;
        }

        if (activeTokens.Count > 0)
        {
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<CurrentUserDto> MapCurrentUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _userManager.GetRolesAsync(user);
        var linkedEmployee = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.UserId == user.Id)
            .Select(record => new
            {
                record.Id,
                record.EmployeeCode
            })
            .SingleOrDefaultAsync(cancellationToken);

        var managedEmployeeCount = 0;
        if (linkedEmployee is not null)
        {
            managedEmployeeCount = await _dbContext.Employees
                .AsNoTracking()
                .CountAsync(record => record.ManagerId == linkedEmployee.Id && record.IsActive, cancellationToken);
        }

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            IsEnabled = user.IsEnabled,
            LinkedEmployeeId = linkedEmployee?.Id,
            LinkedEmployeeCode = linkedEmployee?.EmployeeCode ?? string.Empty,
            HasLinkedEmployee = linkedEmployee is not null,
            IsManager = managedEmployeeCount > 0,
            ManagedEmployeeCount = managedEmployeeCount,
            Roles = roles.OrderBy(role => role).ToArray()
        };
    }
}
