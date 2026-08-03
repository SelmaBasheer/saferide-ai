namespace SafeRide.Schools.Application.Common;

public static class SchoolErrors
{
    public static readonly Error SchoolNotFound = new(
        ErrorCodes.SchoolNotFound,
        "School not found."
    );

    public static readonly Error NotEditable = new(
        ErrorCodes.SchoolNotEditable,
        "The school can only be edited while in Draft or Rejected status."
    );

    public static readonly Error InvalidFileType = new(
        ErrorCodes.InvalidFileType,
        "Only PDF, JPEG, and PNG files are allowed."
    );
    public static readonly Error FileTooLarge = new(
        ErrorCodes.FileTooLarge,
        "File size must not exceed 5 MB."
    );

    public static readonly Error NotSubmitted = new(
        ErrorCodes.InvalidTransition,
        "Only a submitted school can be reviewed."
    );

    public static readonly Error ReasonRequired = new(
        ErrorCodes.RejectionReasonRequired,
        "A rejection reason is required."
    );

    public static readonly Error DocumentNotFound = new(
        ErrorCodes.DocumentNotFound,
        "Document not found."
    );
}
