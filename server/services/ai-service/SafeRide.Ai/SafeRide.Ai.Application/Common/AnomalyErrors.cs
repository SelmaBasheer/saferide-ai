namespace SafeRide.Ai.Application.Common;

public static class AnomalyErrors
{
    public static readonly Error NotFound = new(ErrorCodes.AnomalyNotFound, "Alert not found.");

    public static readonly Error AlreadyResolved = new(
        ErrorCodes.AlreadyResolved,
        "This alert has already been sent or dismissed."
    );
}
