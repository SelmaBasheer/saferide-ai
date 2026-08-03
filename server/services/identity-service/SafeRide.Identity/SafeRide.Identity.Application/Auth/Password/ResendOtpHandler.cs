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
    public async Task<Result> HandleAsync(
        ResendOtpCommand cmd,
        OtpPurpose purpose,
        CancellationToken ct
    )
    {
        var user = await users.GetByEmailAsync(cmd.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return Result.Success();

        if (
            purpose == OtpPurpose.EmailVerification
            && user.Status != UserStatus.PendingVerification
        )
            return Result.Failure(AuthErrors.AlreadyVerified);

        var last = await otps.GetLatestAsync(user.Id, purpose, ct);
        if (last is not null && last.CreatedAtUtc > DateTime.UtcNow.AddSeconds(-60))
            return Result.Success(); // silent — same as unknown email

        var code = otpService.Generate();
        await otps.AddAsync(OtpCode.Issue(user.Id, otpService.Hash(code), purpose), ct);
        await uow.SaveChangesAsync(ct);

        try
        {
            await publisher.PublishAsync(
                "identity.events",
                "otp-email-requested",
                new OtpEmailRequested(
                    user.Id,
                    user.Email.Value,
                    user.FirstName,
                    code,
                    purpose.ToString(),
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OTP event for {UserId}", user.Id);
        }

        logger.LogInformation("DEV OTP for {UserId}: {Code}", user.Id, code); // TODO: remove before merge
        return Result.Success();
    }
}
