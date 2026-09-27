namespace SafeRide.Identity.Application.Abstractions;

public interface IFileStorage
{
    Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default
    );

    /// A short-lived signed link. The database stores the blob name; a URL is
    /// generated when someone asks, so the container stays private.
    Uri GetReadUrl(string key, TimeSpan validFor);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
