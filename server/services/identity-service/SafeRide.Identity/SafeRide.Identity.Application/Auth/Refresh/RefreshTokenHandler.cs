using FluentValidation;
using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Refresh;

public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwt,
    IValidator<RefreshTokenCommand> validator,
    ILogger<RefreshTokenHandler> logger
)
{
    public async Task<Result<RefreshTokenResult>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken ct = default
    )
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.Failure<RefreshTokenResult>(
                new Error(
                    ErrorCodes.ValidationFailed,
                    string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))
                )
            );

        var incomingHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(command.RefreshToken)
            )
        );
        var existingToken = await refreshTokens.GetByTokenHashAsync(incomingHash, ct);

        if (existingToken is null || !existingToken.IsActive)
        {
            logger.LogWarning("Refresh failed — token invalid, expired, or already revoked");
            return Result.Failure<RefreshTokenResult>(AuthErrors.InvalidRefreshToken);
        }

        var user = await users.GetByIdAsync(existingToken.UserId, ct);
        if (user is null || !user.CanLogin())
        {
            logger.LogWarning(
                "Refresh failed — account not active for UserId {UserId}",
                existingToken.UserId
            );
            return Result.Failure<RefreshTokenResult>(AuthErrors.AccountNotActive);
        }

        existingToken.Revoke();
        var newAccessToken = jwt.GenerateAccessToken(user);
        var newRawRefreshToken = jwt.GenerateRefreshToken();
        var newHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(newRawRefreshToken)
            )
        );
        var newRefreshToken = RefreshToken.Issue(
            user.Id,
            newHash,
            lifetimeDays: (int)jwt.RefreshTokenLifetime.TotalDays
        );

        await refreshTokens.AddAsync(newRefreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Refresh token rotated successfully for UserId {UserId}", user.Id);

        return Result.Success(
            new RefreshTokenResult(
                newAccessToken,
                newRawRefreshToken,
                DateTime.UtcNow.Add(jwt.AccessTokenLifetime)
            )
        );
    }
}
