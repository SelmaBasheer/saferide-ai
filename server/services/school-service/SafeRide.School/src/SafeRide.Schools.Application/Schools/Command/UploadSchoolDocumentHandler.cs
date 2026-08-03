using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed class UploadSchoolDocumentHandler(
    ISchoolRepository schools,
    IUnitOfWork unitOfWork,
    IFileStorage storage
)
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
    ];

    public async Task<Result> UploadAsync(
        Guid adminUserId,
        UploadSchoolDocumentCommand cmd,
        CancellationToken ct
    )
    {
        if (!AllowedContentTypes.Contains(cmd.ContentType))
            return Result.Failure(SchoolErrors.InvalidFileType);
        if (cmd.SizeBytes is <= 0 or > MaxFileSizeBytes)
            return Result.Failure(SchoolErrors.FileTooLarge);

        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);
        if (school is null)
            return Result.Failure(SchoolErrors.SchoolNotFound);
        if (
            school.Status
            is not (Domain.Enums.SchoolStatus.Draft or Domain.Enums.SchoolStatus.Rejected)
        )
            return Result.Failure(SchoolErrors.NotEditable);

        var extension = Path.GetExtension(cmd.FileName); // ".pdf"
        var blobKey = $"schools/{school.Id}/{cmd.Type}{extension}";
        var oldBlobKey = school.Documents.FirstOrDefault(d => d.Type == cmd.Type)?.BlobKey;

        await storage.UploadAsync(blobKey, cmd.Content, cmd.ContentType, ct);

        school.AddOrReplaceDocument(
            cmd.Type,
            cmd.FileName,
            blobKey,
            cmd.ContentType,
            cmd.SizeBytes
        );
        await unitOfWork.SaveChangesAsync(ct);

        if (oldBlobKey is not null && oldBlobKey != blobKey)
            await storage.DeleteAsync(oldBlobKey, ct);

        return Result.Success();
    }
}
