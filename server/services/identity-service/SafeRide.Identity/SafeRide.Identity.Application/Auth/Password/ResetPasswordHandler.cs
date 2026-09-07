using FluentValidation;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Password;

public sealed class ResetPasswordHandler(
    IUserRepository users,
    IOtpCodeRepository otps,
    IOtpService otpService,
    IPasswordHasher passwordHasher,
    IUnitOfWork uow,
    IValidator<ResetPasswordCommand> validator
)
{
    public async Task<Result> HandleAsync(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var v = await validator.ValidateAsync(cmd, ct);
        if (!v.IsValid)
            return Result.Failure(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", v.Errors.Select(e => e.ErrorMessage))
                )
            );

        var user = await users.GetByEmailAsync(cmd.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return Result.Failure(AuthErrors.InvalidOtp);

        var otp = await otps.GetLatestAsync(user.Id, OtpPurpose.PasswordReset, ct);
        if (otp is null)
            return Result.Failure(AuthErrors.InvalidOtp);

        // Tell the user they've run out of guesses — they already know they made
        // them, so this reveals nothing, and retyping a dead code can never work.
        if (otp.AttemptsExhausted)
            return Result.Failure(AuthErrors.OtpAttemptsExhausted);

        if (!otp.IsValid)
            return Result.Failure(AuthErrors.InvalidOtp);

        if (!otpService.Verify(cmd.Otp, otp.CodeHash))
        {
            otp.RecordFailedAttempt();
            await uow.SaveChangesAsync(ct);
            return Result.Failure(AuthErrors.InvalidOtp);
        }

        user.ResetPassword(passwordHasher.HashPassword(cmd.NewPassword));
        otp.Consume();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
