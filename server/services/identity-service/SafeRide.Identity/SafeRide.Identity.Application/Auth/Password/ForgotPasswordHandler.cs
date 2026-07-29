using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Password;

public sealed class ForgotPasswordHandler(
    IUserRepository users,
    IOtpCodeRepository otps,
    IOtpService otpService,
    IUnitOfWork uow,
    IEventPublisher publisher,
    ILogger<ForgotPasswordHandler> logger
)
{
    public async Task<Result> HandleAsync(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return Result.Success(); // don't reveal unknown emails

        var code = otpService.Generate();
        await otps.AddAsync(
            OtpCode.Issue(user.Id, otpService.Hash(code), OtpPurpose.PasswordReset),
            ct
        );
        await uow.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            "identity.events",
            "otp-email-requested",
            new OtpEmailRequested(
                user.Id,
                user.Email.Value,
                user.FirstName,
                code,
                "PasswordReset",
                DateTime.UtcNow
            ),
            ct
        );

        logger.LogInformation("DEV OTP for {Email}: {Code}", user.Email.Value, code);
        return Result.Success();
    }
}
