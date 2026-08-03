namespace SafeRide.Schools.Api.Contracts;

public sealed record SchoolDetailResponse(
    Guid Id,
    string Name,
    string Address,
    string City,
    string District,
    string State,
    string Pincode,
    string? LegalName,
    string? Board,
    string? RegistrationNumber,
    string? AuthorizedPersonName,
    string? AuthorizedPersonDesignation,
    string? OfficialPhone,
    string? OfficialEmail,
    string? BusCount,
    string? StudentCount,
    string Status,
    string? RejectionReason,
    DateTime? SubmittedAtUtc,
    DateTime? RejectedAtUtc,
    DateTime? ApprovedAtUtc,
    IReadOnlyList<SchoolDocumentDto> Documents,
    IReadOnlyList<string> MissingRequirements
);
