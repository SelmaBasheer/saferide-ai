namespace SafeRide.Identity.Application.Common;

public static class ErrorCodes
{
    public const string EmailTaken = "Auth.EmailTaken";
    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string InvalidRefreshToken = "Auth.InvalidRefreshToken";
    public const string AccountNotActive = "Auth.AccountNotActive";
    public const string OtpInvalid = "Otp.Invalid";
    public const string ValidationFailed = "Validation.Failed";
    public const string AlreadyVerified = "Auth.AlreadyVerified";
}
