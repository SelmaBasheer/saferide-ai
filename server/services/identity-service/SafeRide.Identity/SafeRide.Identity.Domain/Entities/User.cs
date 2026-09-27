using SafeRide.Identity.Domain.Enums;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public Guid? SchoolId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool MustChangePassword { get; private set; }

    /// The key in blob storage. Never a URL, and never shown to a user.
    public string? PhotoBlobName { get; private set; }

    private User() { }

    public static User RegisterSchoolAdmin(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        Phone phone
    ) =>
        Create(
            email,
            passwordHash,
            firstName,
            lastName,
            phone,
            UserRole.SchoolAdmin,
            UserStatus.PendingVerification
        );

    public static User CreateSuperAdmin(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        Phone phone
    ) =>
        Create(
            email,
            passwordHash,
            firstName,
            lastName,
            phone,
            UserRole.SuperAdmin,
            UserStatus.Active
        );

    private static User Create(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        Phone phone,
        UserRole role,
        UserStatus status
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = role,
            Status = status,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

    public static User CreateInvited(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        Phone phone,
        UserRole role,
        Guid schoolId
    )
    {
        var user = Create(email, passwordHash, firstName, lastName, phone, role, UserStatus.Active);
        user.SchoolId = schoolId;
        user.MustChangePassword = true;
        return user;
    }

    public void VerifyEmail()
    {
        if (Status != UserStatus.PendingVerification)
            throw new InvalidOperationException(
                "Only a pending-verification account can be verified."
            );
        Status = UserStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void LinkSchool(Guid schoolId)
    {
        SchoolId = schoolId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanLogin() => Status == UserStatus.Active;

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ResetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false; // invitation completed / password now user-chosen
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Email is deliberately not here. Changing it needs re-verification, and a
    /// student record stores the parent's email as the link to their children —
    /// so a silent change would quietly disconnect a family.
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, Phone phone)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPhoto(string blobName)
    {
        PhotoBlobName = blobName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearPhoto()
    {
        PhotoBlobName = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
