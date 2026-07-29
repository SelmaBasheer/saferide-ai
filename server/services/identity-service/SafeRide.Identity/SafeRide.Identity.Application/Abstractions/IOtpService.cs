namespace SafeRide.Identity.Application.Abstractions;

public interface IOtpService
{
    string Generate(); // a fresh 6-digit code
    string Hash(string code); // hash for storage
    bool Verify(string code, string hash);
}
