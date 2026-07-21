using FluentValidation;
using Microsoft.Extensions.Logging;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Entities;

namespace SafeRide.Identity.Application.Auth.Login;

public sealed class LoginHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt,
    IUnitOfWork unitOfWork,
    IValidator<LoginCommand> validator,
    ILogger<LoginHandler> logger
)
{
    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken ct = default
    )
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.Failure<LoginResult>(
                new Error(
                    "Validation.Failed",
                    string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage))
                )
            );

        var user = await users.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed — invalid credentials for {Email}", command.Email);
            return Result.Failure<LoginResult>(
                new Error("Auth.InvalidCredentials", "Invalid email or password.")
            );
        }

        if (!user.CanLogin())
        {
            logger.LogWarning(
                "Login failed — account not active ({Status}) for {Email}",
                user.Status,
                command.Email
            );
            return Result.Failure<LoginResult>(
                new Error("Auth.AccountNotActive", $"Account is {user.Status}.")
            );
        }

        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefreshToken = jwt.GenerateRefreshToken();
        var refreshTokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawRefreshToken)
            )
        );
        var refreshToken = RefreshToken.Issue(
            user.Id,
            refreshTokenHash,
            lifetimeDays: (int)jwt.RefreshTokenLifetime.TotalDays
        );

        await refreshTokens.AddAsync(refreshToken, ct);
        user.RecordSuccessfulLogin();
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Login succeeded for {Email}", command.Email);

        return Result.Success(
            new LoginResult(
                accessToken,
                rawRefreshToken,
                DateTime.UtcNow.Add(jwt.AccessTokenLifetime),
                user.Id,
                user.FirstName,
                user.LastName,
                user.Role,
                user.MustChangePassword
            )
        );
    }
}
