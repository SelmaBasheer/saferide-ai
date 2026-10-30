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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (licenseExpiryDate < today.AddDays(5))
        {
            throw new ArgumentException(
                "The licence must have at least 5 days remaining.",
                nameof(licenseExpiryDate)
            );
        }
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

    /// <summary>
    /// Deliberately does NOT enforce the 5-day rule that Create does.
    /// Hiring someone whose licence is about to lapse is a mistake worth
    /// blocking; recording that an existing driver's licence has already
    /// expired is the truth, and refusing to store it would leave the record
    /// wrong. Eligibility is derived from the date, not from whether we
    /// allowed it to be saved.
    /// </summary>
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

    /// <returns>true if the status actually changed — the caller publishes an event only then.</returns>
    public bool Deactivate()
    {
        if (Status == DriverStatus.Inactive)
            return false;

        Status = DriverStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// No licence check here on purpose. Employment and licence validity are
    /// separate facts: a driver can be re-employed on Monday and renew their
    /// licence on Wednesday. Being assignable to a bus requires both.
    /// </summary>
    public bool Reactivate()
    {
        if (Status == DriverStatus.Active)
            return false;

        Status = DriverStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool LicenceIsValidOn(DateOnly date) => LicenseExpiryDate >= date;

    public bool CanBeAssigned(DateOnly today) =>
        Status == DriverStatus.Active && LicenceIsValidOn(today);
}
