namespace SafeRide.Identity.Application.Auth.Password;

public sealed record ForgotPasswordCommand(string Email);

public sealed record ResendOtpCommand(string Email);

public sealed record ResetPasswordCommand(string Email, string Otp, string NewPassword);
