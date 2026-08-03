namespace SafeRide.Identity.Application.Common;

public static class AuthErrors
{
    public static readonly Error EmailTaken = new(
        ErrorCodes.EmailTaken,
        "An account with this email already exists."
    );

    public static readonly Error InvalidCredentials = new(
        ErrorCodes.InvalidCredentials,
        "Invalid email or password."
    );

    public static readonly Error InvalidRefreshToken = new(
        ErrorCodes.InvalidRefreshToken,
        "Refresh token is invalid or expired."
    );

    public static readonly Error AccountNotActive = new(
        ErrorCodes.AccountNotActive,
        "Account is not active."
    );

    public static readonly Error AlreadyVerified = new(
        ErrorCodes.AlreadyVerified,
        "This account is already verified."
    );

    // Same message for both OTP failure cases (also better for security).
    public static readonly Error InvalidOtp = new(ErrorCodes.OtpInvalid, "Invalid or expired OTP.");
}
