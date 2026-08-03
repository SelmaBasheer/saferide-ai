using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Schools.Query;

public sealed class GetDocumentDownloadUrlHandler(ISchoolRepository schools, IFileStorage storage)
{
    public async Task<Result<Uri>> GetAsync(Guid schoolId, Guid documentId, CancellationToken ct)
    {
        var school = await schools.GetWithDocumentsAsync(schoolId, ct);
        if (school is null)
            return Result.Failure<Uri>(SchoolErrors.SchoolNotFound);

        var doc = school.Documents.FirstOrDefault(d => d.Id == documentId);
        if (doc is null)
            return Result.Failure<Uri>(SchoolErrors.DocumentNotFound);

        var url = storage.GetDownloadUrl(doc.BlobKey, doc.FileName, TimeSpan.FromMinutes(10));
        return Result.Success(url);
    }
}
