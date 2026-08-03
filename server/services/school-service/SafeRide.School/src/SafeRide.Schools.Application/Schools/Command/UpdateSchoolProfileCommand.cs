using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed record UpdateSchoolProfileCommand(
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
