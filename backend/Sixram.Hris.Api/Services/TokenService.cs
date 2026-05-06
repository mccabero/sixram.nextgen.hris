using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sixram.Api.Configuration;
using Sixram.Api.DTOs.Auth;
using Sixram.Api.Entities;

namespace Sixram.Api.Services;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public sealed record RefreshTokenResult(string PlainTextToken, string TokenHash, DateTime ExpiresAtUtc);

public interface ITokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    RefreshTokenResult CreateRefreshToken();

    CookieOptions CreateRefreshTokenCookieOptions(DateTime expiresAtUtc, bool isHttps);

    CookieOptions CreateExpiredRefreshTokenCookieOptions(bool isHttps);

    string HashRefreshToken(string token);
}

public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<ApplicationUser> userManager)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
    }

    public async Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _userManager.GetRolesAsync(user);
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
        return new RefreshTokenResult(token, HashRefreshToken(token), expiresAtUtc);
    }

    public CookieOptions CreateRefreshTokenCookieOptions(DateTime expiresAtUtc, bool isHttps)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/api/auth",
            Expires = new DateTimeOffset(expiresAtUtc)
        };
    }

    public CookieOptions CreateExpiredRefreshTokenCookieOptions(bool isHttps)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
