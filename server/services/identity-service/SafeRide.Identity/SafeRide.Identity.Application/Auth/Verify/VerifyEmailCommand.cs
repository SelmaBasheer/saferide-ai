namespace SafeRide.Identity.Application.Auth.Verify;

public sealed record VerifyEmailCommand(string Email, string Otp);
