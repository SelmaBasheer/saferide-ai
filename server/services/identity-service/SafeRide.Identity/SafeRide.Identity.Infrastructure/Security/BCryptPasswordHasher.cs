using SafeRide.Identity.Application.Abstractions;

namespace SafeRide.Identity.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Adjust the work factor as needed for security and performance

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}
