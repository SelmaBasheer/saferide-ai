using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Application.Common;

public static class ErrorCodes
{
    public const string SchoolNotFound = "School.NotFound";
    public const string SchoolNotEditable = DomainErrorCodes.NotEditable; // = "School.NotEditable"

    // ErrorCodes
    public const string InvalidFileType = "School.InvalidFileType";
    public const string FileTooLarge = "School.FileTooLarge";

    public const string IncompleteSubmission = DomainErrorCodes.IncompleteSubmission;

    public const string InvalidTransition = DomainErrorCodes.InvalidTransition;
    public const string RejectionReasonRequired = "School.RejectionReasonRequired";

    public const string DocumentNotFound = "School.DocumentNotFound";
}
