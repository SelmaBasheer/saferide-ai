using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Entities;

namespace SafeRide.Identity.Application.Auth.Register;

public sealed class RegisterSchoolAdminHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public async Task<Result<Guid>> HandleAsync(
        RegisterSchoolAdminCommand command,
        CancellationToken ct
    )
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
        {
            return Result.Failure<Guid>(
                new Error("Auth.EmailTaken", "An account with this email already exists.")
            );
        }
        var hashedPassword = passwordHasher.HashPassword(command.Password);
        var user = User.RegisterSchoolAdmin(
            command.Email,
            hashedPassword,
            command.FullName,
            command.Phone
        );

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            "identity.events",
            "school-admin-registered",
            new SchoolAdminRegistered(
                user.Id,
                user.Email,
                user.FullName,
                user.Phone,
                command.SchoolName,
                command.SchoolAddress,
                command.City,
                command.State,
                command.Pincode,
                DateTime.UtcNow
            ),
            ct
        );

        return Result.Success(user.Id);
    }
}
