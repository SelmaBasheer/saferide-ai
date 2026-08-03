using FluentValidation;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Verify;

public sealed class VerifyEmailHandler(
    IUserRepository users,
    IOtpCodeRepository otps,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IValidator<VerifyEmailCommand> validator
)
{
    public async Task<Result> HandleAsync(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            return Result.Failure(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))
                )
            );

        var user = await users.GetByEmailAsync(cmd.Email, ct);
        if (user is null)
            return Result.Failure(AuthErrors.InvalidOtp); // vague on purpose

        if (user.Status != UserStatus.PendingVerification)
            return Result.Failure(AuthErrors.AlreadyVerified);

        var otp = await otps.GetLatestAsync(user.Id, OtpPurpose.EmailVerification, ct);
        if (otp is null || !otp.IsValid || !otpService.Verify(cmd.Otp, otp.CodeHash))
            return Result.Failure(AuthErrors.InvalidOtp);

        otp.Consume();
        user.VerifyEmail();
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
