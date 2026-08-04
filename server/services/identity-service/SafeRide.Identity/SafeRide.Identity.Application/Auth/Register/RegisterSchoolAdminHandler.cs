using FluentValidation;
using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Application.Auth.Register;

public sealed class RegisterSchoolAdminHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    IValidator<RegisterSchoolAdminCommand> validator,
    IOtpCodeRepository otps,
    IOtpService otpService,
    ILogger<RegisterSchoolAdminHandler> logger
)
{
    public async Task<Result<Guid>> HandleAsync(
        RegisterSchoolAdminCommand command,
        CancellationToken ct
    )
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.Failure<Guid>(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))
                )
            );

        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
        {
            logger.LogWarning("Registration failed — email already exists {Email}", command.Email);
            return Result.Failure<Guid>(AuthErrors.EmailTaken);
        }

        var email = Email.Create(command.Email);
        var phone = Phone.Create(command.Phone);
        var hashedPassword = passwordHasher.HashPassword(command.Password);
        var user = User.RegisterSchoolAdmin(
            email,
            hashedPassword,
            command.FirstName,
            command.LastName,
            phone
        );

        await userRepository.AddAsync(user, ct);

        var code = otpService.Generate();
        await otps.AddAsync(
            OtpCode.Issue(user.Id, otpService.Hash(code), OtpPurpose.EmailVerification),
            ct
        );
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await publisher.PublishAsync(
                "identity.events",
                "school-admin-registered",
                new SchoolAdminRegistered(
                    user.Id,
                    user.Email.Value,
                    user.FirstName,
                    user.LastName,
                    user.Phone.Value,
                    command.SchoolName,
                    command.SchoolAddress.Trim(),
                    command.City.Trim(),
                    command.District.Trim(),
                    command.State.Trim(),
                    command.Pincode.Trim(),
                    DateTime.UtcNow
                ),
                ct
            );

            await publisher.PublishAsync(
                "identity.events",
                "otp-email-requested",
                new OtpEmailRequested(
                    user.Id,
                    user.Email.Value,
                    user.FirstName,
                    code,
                    "EmailVerification",
                    DateTime.UtcNow
                ),
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish registration events for UserId {UserId} — registration still succeeded",
                user.Id
            );
        }

        logger.LogInformation(
            "SchoolAdmin registered — UserId {UserId}, Email {Email}",
            user.Id,
            user.Email.Value
        );

        return Result.Success(user.Id);
    }
}
