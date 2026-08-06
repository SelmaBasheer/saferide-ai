using DriverService.Domain;

namespace DriverService.Features.GetDrivers;

public record DriverListItem(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string Status
)
{
    public static DriverListItem From(Driver d) =>
        new(
            d.Id,
            d.FirstName,
            d.LastName,
            d.Email,
            d.Phone,
            d.LicenseNumber,
            d.LicenseExpiryDate,
            d.Status.ToString()
        );
}
