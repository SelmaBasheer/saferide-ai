namespace DriverService.Domain;

public class Driver
{
    private Driver() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string LicenseNumber { get; private set; } = null!;
    public DateOnly LicenseExpiryDate { get; private set; }
    public DriverStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Driver Create(
        Guid schoolId,
        string firstName,
        string lastName,
        string email,
        string phone,
        string licenseNumber,
        DateOnly licenseExpiryDate
    )
    {
        return new Driver
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(), // invitation lookup key — normalize once, here
            Phone = phone.Trim(),
            LicenseNumber = licenseNumber.Trim(),
            LicenseExpiryDate = licenseExpiryDate,
            Status = DriverStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateDetails(
        string firstName,
        string lastName,
        string phone,
        string licenseNumber,
        DateOnly licenseExpiryDate
    )
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        LicenseNumber = licenseNumber.Trim();
        LicenseExpiryDate = licenseExpiryDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = DriverStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
}
