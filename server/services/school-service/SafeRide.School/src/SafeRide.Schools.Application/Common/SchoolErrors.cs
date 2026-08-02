namespace SafeRide.Schools.Application.Common;

public static class SchoolErrors
{
    public static readonly Error SchoolNotFound = new(
        ErrorCodes.SchoolNotFound,
        "School not found."
    );
}
