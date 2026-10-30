namespace DriverService.Features.UpdateDriver;

/// <summary>
/// Email is absent on purpose — it is the login identity and the invitation
/// lookup key, so changing it here would silently orphan the driver's account.
/// </summary>
public sealed record UpdateDriverRequest(
    string FirstName,
    string LastName,
    string Phone,
    string LicenseNumber,
    DateOnly LicenseExpiryDate
);
