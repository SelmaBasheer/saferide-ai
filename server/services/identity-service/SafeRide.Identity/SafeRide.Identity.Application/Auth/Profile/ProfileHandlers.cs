using FluentValidation;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Application.Auth.Profile;

/// <summary>
/// Turns a user into the shape the API returns, including a signed photo link.
/// One hour: long enough that a page left open all morning still shows the
/// photo, short enough that a leaked URL stops working the same day.
/// </summary>
public static class ProfileMapper
{
    private static readonly TimeSpan PhotoUrlLifetime = TimeSpan.FromHours(1);

    public static ProfileView ToView(User user, IFileStorage storage) =>
        new(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Phone.Value,
            user.Role.ToString(),
            user.Status.ToString(),
            user.SchoolId,
            user.MustChangePassword,
            user.PhotoBlobName is null
                ? null
                : storage.GetReadUrl(user.PhotoBlobName, PhotoUrlLifetime).ToString()
        );
}

public sealed class GetProfileHandler(IUserRepository users, IFileStorage storage)
{
    public async Task<Result<ProfileView>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);

        return user is null
            ? Result.Failure<ProfileView>(AuthErrors.UserNotFound)
            : Result.Success(ProfileMapper.ToView(user, storage));
    }
}

public sealed class UpdateProfileHandler(
    IUserRepository users,
    IFileStorage storage,
    IUnitOfWork uow,
    IValidator<UpdateProfileCommand> validator
)
{
    public async Task<Result<ProfileView>> HandleAsync(
        Guid userId,
        UpdateProfileCommand cmd,
        CancellationToken ct
    )
    {
        var v = await validator.ValidateAsync(cmd, ct);

        if (!v.IsValid)
            return Result.Failure<ProfileView>(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", v.Errors.Select(e => e.ErrorMessage))
                )
            );

        var user = await users.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.Failure<ProfileView>(AuthErrors.UserNotFound);

        user.UpdateProfile(cmd.FirstName, cmd.LastName, Phone.Create(cmd.Phone));

        await uow.SaveChangesAsync(ct);

        return Result.Success(ProfileMapper.ToView(user, storage));
    }
}

public sealed class ChangePasswordHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork uow,
    IValidator<ChangePasswordCommand> validator
)
{
    public async Task<Result> HandleAsync(
        Guid userId,
        ChangePasswordCommand cmd,
        CancellationToken ct
    )
    {
        var v = await validator.ValidateAsync(cmd, ct);

        if (!v.IsValid)
            return Result.Failure(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", v.Errors.Select(e => e.ErrorMessage))
                )
            );

        var user = await users.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.Failure(AuthErrors.UserNotFound);

        // The current password is required even though the caller already holds
        // a valid token. A stolen token should not be enough to lock the real
        // owner out of their own account.
        if (!passwordHasher.VerifyPassword(cmd.CurrentPassword, user.PasswordHash))
            return Result.Failure(AuthErrors.IncorrectPassword);

        user.ResetPassword(passwordHasher.HashPassword(cmd.NewPassword));

        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
