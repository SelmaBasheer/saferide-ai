using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Password;

public sealed class ResendOtpHandler(
    IUserRepository users,
    IOtpCodeRepository otps,
    IOtpService otpService,
    IUnitOfWork uow,
    IEventPublisher publisher,
    ILogger<ResendOtpHandler> logger
)
{
    public async Task<Result> HandleAsync(ResendOtpCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return Result.Success();

        var last = await otps.GetLatestAsync(user.Id, OtpPurpose.PasswordReset, ct);
        if (last is not null && last.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-60))
            return Result.Failure(
                new Error("Otp.Cooldown", "Please wait a minute before requesting another code.")
            );

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
