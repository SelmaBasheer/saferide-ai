using FluentValidation;
using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Application.Auth.Register;

public sealed class RegisterSchoolAdminHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    IValidator<RegisterSchoolAdminCommand> validator,
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
                    "Validation.Failed",
                    string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))
                )
            );

        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
        {
            logger.LogWarning("Registration failed — email already exists {Email}", command.Email);
            return Result.Failure<Guid>(
                new Error("Auth.EmailTaken", "An account with this email already exists.")
            );
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
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish SchoolAdminRegistered event for UserId {UserId} — registration still succeeded",
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
