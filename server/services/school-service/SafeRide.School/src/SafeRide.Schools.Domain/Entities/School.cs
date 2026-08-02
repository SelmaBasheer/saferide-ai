using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Exceptions;

namespace SafeRide.Schools.Domain.Entities;

public class School : BaseEntity
{
    // School details (came from the registration)
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Pincode { get; private set; } = null!;

    // Who registered it — a UUID reference to the Identity service's user (no FK across services)
    public Guid AdminUserId { get; private set; }
    public string AdminEmail { get; private set; } = null!;
    public string AdminFirstName { get; private set; } = null!;
    public string AdminLastName { get; private set; } = null!;

    public string AdminPhone { get; private set; } = null!;

    public SchoolStatus Status { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    private School() { }

    // Factory: a school always starts life PendingApproval
    public static School CreatePending(
        Guid adminUserId,
        string adminEmail,
        string adminFirstName,
        string adminLastName,
        string adminPhone,
        string name,
        string address,
        string city,
        string district,
        string state,
        string pincode
    ) =>
        new()
        {
            AdminUserId = adminUserId,
            AdminEmail = adminEmail,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminPhone = adminPhone,
            Name = name,
            Address = address,
            City = city,
            District = district,
            State = state,
            Pincode = pincode,
            Status = SchoolStatus.PendingApproval,
        };

    public void Approve()
    {
        if (Status != SchoolStatus.PendingApproval)
            throw new DomainException(
                DomainErrorCodes.InvalidTransition,
                "Only a pending school can be approved."
            );
        Status = SchoolStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status != SchoolStatus.Approved)
            throw new DomainException(
                DomainErrorCodes.InvalidTransition,
                "Only an approved school can be suspended."
            );
        Status = SchoolStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
