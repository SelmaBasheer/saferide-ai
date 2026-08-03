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

    // Extended profile (filled in during onboarding — nullable while Draft)
    public string? LegalName { get; private set; }
    public AffiliationBoard? Board { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? AuthorizedPersonName { get; private set; }
    public string? AuthorizedPersonDesignation { get; private set; }
    public string? OfficialPhone { get; private set; }
    public string? OfficialEmail { get; private set; }
    public BusCountRange? BusCount { get; private set; }
    public StudentCountRange? StudentCount { get; private set; }

    // Review lifecycle
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    private readonly List<SchoolDocument> _documents = [];
    public IReadOnlyCollection<SchoolDocument> Documents => _documents.AsReadOnly();

    private School() { }

    // Factory: a school always starts life Draft
    public static School CreateDraft(
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
            Status = SchoolStatus.Draft,
        };

    public void UpdateProfile(
        string name,
        string address,
        string city,
        string district,
        string state,
        string pincode,
        string? legalName,
        AffiliationBoard? board,
        string? registrationNumber,
        string? authorizedPersonName,
        string? authorizedPersonDesignation,
        string? officialPhone,
        string? officialEmail,
        BusCountRange? busCount,
        StudentCountRange? studentCount
    )
    {
        if (Status is not (SchoolStatus.Draft or SchoolStatus.Rejected))
            throw new DomainException(
                DomainErrorCodes.NotEditable,
                "Profile can only be edited while Draft or Rejected."
            );

        Name = name;
        Address = address;
        City = city;
        District = district;
        State = state;
        Pincode = pincode;
        LegalName = legalName;
        Board = board;
        RegistrationNumber = registrationNumber;
        AuthorizedPersonName = authorizedPersonName;
        AuthorizedPersonDesignation = authorizedPersonDesignation;
        OfficialPhone = officialPhone;
        OfficialEmail = officialEmail;
        BusCount = busCount;
        StudentCount = studentCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddOrReplaceDocument(
        DocumentType type,
        string fileName,
        string blobKey,
        string contentType,
        long fileSizeBytes
    )
    {
        if (Status is not (SchoolStatus.Draft or SchoolStatus.Rejected))
            throw new DomainException(
                DomainErrorCodes.NotEditable,
                "Documents can only be changed while Draft or Rejected."
            );

        _documents.RemoveAll(d => d.Type == type);
        _documents.Add(
            SchoolDocument.Create(Id, type, fileName, blobKey, contentType, fileSizeBytes)
        );
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<string> GetMissingRequirements()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(LegalName))
            missing.Add("Legal name");
        if (Board is null)
            missing.Add("Affiliation board");
        if (string.IsNullOrWhiteSpace(RegistrationNumber))
            missing.Add("Registration number");
        if (string.IsNullOrWhiteSpace(AuthorizedPersonName))
            missing.Add("Authorized person name");
        if (string.IsNullOrWhiteSpace(AuthorizedPersonDesignation))
            missing.Add("Authorized person designation");
        if (string.IsNullOrWhiteSpace(OfficialPhone))
            missing.Add("Official phone");
        if (string.IsNullOrWhiteSpace(OfficialEmail))
            missing.Add("Official email");
        if (BusCount is null)
            missing.Add("Bus count");
        if (StudentCount is null)
            missing.Add("Student count");

        if (_documents.All(d => d.Type != DocumentType.RegistrationCertificate))
            missing.Add("Registration certificate document");
        if (_documents.All(d => d.Type != DocumentType.AdminIdProof))
            missing.Add("Admin ID proof document");

        return missing;
    }

    public void Submit()
    {
        if (Status is not (SchoolStatus.Draft or SchoolStatus.Rejected))
            throw new DomainException(
                DomainErrorCodes.InvalidTransition,
                "Only a draft or rejected school can be submitted."
            );

        var missing = GetMissingRequirements();
        if (missing.Count > 0)
            throw new DomainException(
                DomainErrorCodes.IncompleteSubmission,
                $"Profile is incomplete: {string.Join(", ", missing)}."
            );

        Status = SchoolStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        RejectedAtUtc = null;
        RejectionReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid reviewerUserId)
    {
        if (Status != SchoolStatus.Submitted)
            throw new DomainException(
                DomainErrorCodes.InvalidTransition,
                "Only a submitted school can be approved."
            );
        Status = SchoolStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;
        ReviewedByUserId = reviewerUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid reviewerUserId, string reason)
    {
        if (Status != SchoolStatus.Submitted)
            throw new DomainException(
                DomainErrorCodes.InvalidTransition,
                "Only a submitted school can be rejected."
            );
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = SchoolStatus.Rejected;
        RejectedAtUtc = DateTime.UtcNow;
        RejectionReason = reason;
        ReviewedByUserId = reviewerUserId;
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
