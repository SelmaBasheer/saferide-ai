using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Application.Auth.Profile;

public sealed record PhotoUpload(Stream Content, long SizeBytes);

public sealed class ProfilePhotoHandler(
    IUserRepository users,
    IFileStorage storage,
    IUnitOfWork uow
)
{
    private const long MaxBytes = 2 * 1024 * 1024;

    public async Task<Result<ProfileView>> UploadAsync(
        Guid userId,
        PhotoUpload upload,
        CancellationToken ct
    )
    {
        if (upload.SizeBytes == 0)
            return Result.Failure<ProfileView>(ProfileErrors.EmptyFile);

        if (upload.SizeBytes > MaxBytes)
            return Result.Failure<ProfileView>(ProfileErrors.FileTooLarge);

        var user = await users.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.Failure<ProfileView>(AuthErrors.UserNotFound);

        // The first bytes of the file, not the Content-Type header — that header
        // is supplied by whoever is uploading, and a browser will happily claim
        // an executable is a JPEG.
        var contentType = await DetectImageTypeAsync(upload.Content, ct);

        if (contentType is null)
            return Result.Failure<ProfileView>(ProfileErrors.NotAnImage);

        // One key per user, overwritten on every upload. No extension: the blob
        // carries its own content type, so there is nothing to keep in step and
        // no old file left behind.
        var key = $"{userId:N}/profile";

        await storage.UploadAsync(key, upload.Content, contentType, ct);

        user.SetPhoto(key);
        await uow.SaveChangesAsync(ct);

        return Result.Success(ProfileMapper.ToView(user, storage));
    }

    public async Task<Result<ProfileView>> RemoveAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.Failure<ProfileView>(AuthErrors.UserNotFound);

        var key = user.PhotoBlobName;

        user.ClearPhoto();
        await uow.SaveChangesAsync(ct);

        if (key is not null)
        {
            await storage.DeleteAsync(key, ct);
        }

        return Result.Success(ProfileMapper.ToView(user, storage));
    }

    /// <summary>
    /// Reads the first bytes and matches them against known image signatures.
    /// The stream is rewound afterwards so the caller can still upload it.
    /// </summary>
    private static async Task<string?> DetectImageTypeAsync(Stream content, CancellationToken ct)
    {
        var header = new byte[12];
        var read = await content.ReadAsync(header, ct);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        if (read < 12)
            return null;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return "image/png";

        // WebP: "RIFF" .... "WEBP"
        if (
            header[0] == 0x52
            && header[1] == 0x49
            && header[2] == 0x46
            && header[3] == 0x46
            && header[8] == 0x57
            && header[9] == 0x45
            && header[10] == 0x42
            && header[11] == 0x50
        )
            return "image/webp";

        return null;
    }
}
