using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Application.Auth.Invite;

public sealed class InviteUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IOtpCodeRepository otps,
    IOtpService otpService,
    IUnitOfWork uow,
    IEventPublisher publisher,
    ILogger<InviteUserHandler> logger
)
{
    public async Task HandleAsync(
        string email,
        string firstName,
        string lastName,
        string phone,
        UserRole role,
        Guid schoolId,
        CancellationToken ct
    )
    {
        var normalized = email.Trim().ToLowerInvariant();

        // Natural idempotency: redelivered event or already-known email → skip.
        if (await users.ExistsByEmailAsync(normalized, ct))
        {
            logger.LogInformation(
                "Invite skipped — account already exists ({Role}, school {SchoolId})",
                role,
                schoolId
            );
            return;
        }

        // Random unusable password — nobody knows it, including admins.
        var unusable = passwordHasher.HashPassword(
            Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
        );

        var user = User.CreateInvited(
            Email.Create(normalized),
            unusable,
            firstName,
            lastName,
            Phone.Create(phone),
            role,
            schoolId
        );

        await users.AddAsync(user, ct);

        // Purpose PasswordReset → the EXISTING reset endpoint/page completes the invitation.
        var code = otpService.Generate();
        await otps.AddAsync(
            OtpCode.Issue(user.Id, otpService.Hash(code), OtpPurpose.PasswordReset),
            ct
        );
        await uow.SaveChangesAsync(ct);

        try
        {
            await publisher.PublishAsync(
                MessagingConstants.IdentityEventsExchange, //"identity.events",
                MessagingConstants.OtpEmailRequestedKey, //"otp-email-requested",
                new OtpEmailRequested(
                    user.Id,
                    user.Email.Value,
                    user.FirstName,
                    code,
                    "Invitation", // Notification uses this for the email copy
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish invitation email for {UserId}", user.Id);
            throw;
        }

        logger.LogDebug("Invite OTP issued for {UserId} ({Role})", user.Id, role);
    }
}
