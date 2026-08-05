using DriverService.Domain;

namespace DriverService.Features.CreateDriver;

public record CreateDriverResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status
)
{
    public static CreateDriverResponse From(Driver d) =>
        new(d.Id, d.FirstName, d.LastName, d.Email, d.Status.ToString());
}
