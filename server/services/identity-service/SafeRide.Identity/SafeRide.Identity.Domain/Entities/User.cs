using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public Guid? SchoolId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private User() { }

    public static User RegisterSchoolAdmin(
        string email,
        string passwordHash,
        string fullName,
        string phone
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Phone = phone.Trim(),
            Role = UserRole.SchoolAdmin,
            Status = UserStatus.PendingApproval,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

    public void Activate(Guid schoolId)
    {
        SchoolId = schoolId;
        Status = UserStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanLogin() => Status == UserStatus.Active;
}
