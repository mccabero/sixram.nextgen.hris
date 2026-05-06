using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.DTOs.Auth;
using Sixram.Api.Services;

namespace Sixram.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, ITokenService tokenService, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var session = await _authService.LoginAsync(request, GetIpAddress(), cancellationToken);
        SetRefreshTokenCookie(session.RefreshToken, session.RefreshTokenExpiresAtUtc);
        return Ok(session.Response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_jwtOptions.RefreshTokenCookieName];
        var session = await _authService.RefreshAsync(refreshToken, GetIpAddress(), cancellationToken);

        if (session is null)
        {
            Response.Cookies.Delete(
                _jwtOptions.RefreshTokenCookieName,
                _tokenService.CreateExpiredRefreshTokenCookieOptions(Request.IsHttps));

            return NoContent();
        }

        SetRefreshTokenCookie(session.RefreshToken, session.RefreshTokenExpiresAtUtc);
        return Ok(session.Response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_jwtOptions.RefreshTokenCookieName];
        await _authService.LogoutAsync(refreshToken, GetIpAddress(), cancellationToken);
        Response.Cookies.Delete(_jwtOptions.RefreshTokenCookieName, _tokenService.CreateExpiredRefreshTokenCookieOptions(Request.IsHttps));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetCurrentUserAsync(User, cancellationToken));
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(
            _jwtOptions.RefreshTokenCookieName,
            refreshToken,
            _tokenService.CreateRefreshTokenCookieOptions(expiresAtUtc, Request.IsHttps));
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
