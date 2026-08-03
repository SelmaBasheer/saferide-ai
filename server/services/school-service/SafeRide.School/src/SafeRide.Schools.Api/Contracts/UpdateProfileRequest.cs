using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Api.Contracts;

public sealed record UpdateProfileRequest(
    string Name,
    string Address,
    string City,
    string District,
    string State,
    string Pincode,
    string? LegalName,
    AffiliationBoard? Board,
    string? RegistrationNumber,
    string? AuthorizedPersonName,
    string? AuthorizedPersonDesignation,
    string? OfficialPhone,
    string? OfficialEmail,
    BusCountRange? BusCount,
    StudentCountRange? StudentCount
);
