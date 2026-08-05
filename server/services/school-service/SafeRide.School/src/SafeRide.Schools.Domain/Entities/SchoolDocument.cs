using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Domain.Entities;

public class SchoolDocument : BaseEntity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid SchoolId { get; private set; }
    public DocumentType Type { get; private set; }
    public string FileName { get; private set; } = null!; // original name, for display
    public string BlobKey { get; private set; } = null!; // path inside the Azure container
    public string ContentType { get; private set; } = null!; // e.g. application/pdf
    public long FileSizeBytes { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }

    private SchoolDocument() { } // for EF

    internal static SchoolDocument Create(
        Guid schoolId,
        DocumentType type,
        string fileName,
        string blobKey,
        string contentType,
        long fileSizeBytes
    ) =>
        new()
        {
            TenantId = schoolId,
            SchoolId = schoolId,
            Type = type,
            FileName = fileName,
            BlobKey = blobKey,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAtUtc = DateTime.UtcNow,
        };
}
