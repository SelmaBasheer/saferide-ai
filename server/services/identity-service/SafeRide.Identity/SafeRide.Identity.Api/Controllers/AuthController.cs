using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeRide.Identity.Api.Common;
using SafeRide.Identity.Api.Contracts;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Password;
using SafeRide.Identity.Application.Auth.Refresh;
using SafeRide.Identity.Application.Auth.Register;

namespace SafeRide.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    RegisterSchoolAdminHandler registerHandler,
    LoginHandler loginHandler,
    RefreshTokenHandler refreshHandler,
    IMapper mapper
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
            ResponseMessages.Registered
        );
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await loginHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => mapper.Map<AuthResponse>(v));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct
    )
    {
        var result = await refreshHandler.HandleAsync(command, ct);
        return result.ToApiResponse(v => mapper.Map<AuthResponse>(v));
    }

    [EnableRateLimiting("otp")]
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        [FromServices] ForgotPasswordHandler handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToApiResponse(ResponseMessages.OtpSent);
    }

    [EnableRateLimiting("otp")]
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpCommand command,
        [FromServices] ResendOtpHandler handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToApiResponse(ResponseMessages.OtpResent);
    }

    [EnableRateLimiting("otp-verify")]
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        [FromServices] ResetPasswordHandler handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(command, ct);
        return result.ToApiResponse(ResponseMessages.PasswordReset);
    }
}
