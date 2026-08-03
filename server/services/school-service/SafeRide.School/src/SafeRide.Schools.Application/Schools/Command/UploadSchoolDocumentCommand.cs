using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Application.Schools.Command;

public sealed record UploadSchoolDocumentCommand(
    DocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content
);
