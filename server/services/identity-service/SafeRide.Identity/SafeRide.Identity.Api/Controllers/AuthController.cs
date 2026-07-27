using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Identity.Api.Common;
using SafeRide.Identity.Api.Contracts;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Refresh;
using SafeRide.Identity.Application.Auth.Register;

namespace SafeRide.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    RegisterSchoolAdminHandler registerHandler,
    LoginHandler loginHandler,
    RefreshTokenHandler refreshHandler
) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register/school-admin")]
    public async Task<IActionResult> RegisterSchoolAdmin(
        [FromBody] RegisterSchoolAdminCommand command,
        CancellationToken ct
    )
    {
        var result = await registerHandler.HandleAsync(command, ct);
        return result.ToApiResponse(
            id => new RegisterResponse(id),
            StatusCodes.Status201Created,
            "School administrator registered successfully."
        );
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await loginHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => new AuthResponse(
            v.AccessToken,
            v.RefreshToken,
            v.AccessTokenExpiresAtUtc
        ));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct
    )
    {
        var result = await refreshHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => new AuthResponse(
            v.AccessToken,
            v.RefreshToken,
            v.AccessTokenExpiresAtUtc
        ));
    }
}
