namespace SafeRide.Schools.Application.Abstractions;

public interface IFileStorage
{
    Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default
    );
    Task DeleteAsync(string key, CancellationToken ct = default);
    Uri GetDownloadUrl(string key, string downloadFileName, TimeSpan validFor);
}
