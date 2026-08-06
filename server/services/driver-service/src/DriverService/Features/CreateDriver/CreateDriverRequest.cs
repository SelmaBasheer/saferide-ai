namespace DriverService.Features.CreateDriver;

public record CreateDriverRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string LicenseNumber,
    DateOnly LicenseExpiryDate
);
