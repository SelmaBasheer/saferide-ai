namespace SafeRide.Schools.Api.Contracts;

public sealed record SchoolDocumentDto(
    Guid Id,
    string Type,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAtUtc
);
