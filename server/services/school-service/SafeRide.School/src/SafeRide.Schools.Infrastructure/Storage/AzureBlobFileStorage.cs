using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Infrastructure.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(IConfiguration config)
    {
        var conn =
            config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not configured."
            );
        var containerName = config["AzureStorage:Container"] ?? "school-documents";

        _container = new BlobContainerClient(conn, containerName);
        _container.CreateIfNotExists();
    }

    public async Task UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default
    )
    {
        var blob = _container.GetBlobClient(key);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };
        await blob.UploadAsync(content, options, ct); // overwrites if the key exists
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default) =>
        await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct);

    public Uri GetDownloadUrl(string key, string downloadFileName, TimeSpan validFor)
    {
        var blob = _container.GetBlobClient(key);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validFor))
        {
            ContentDisposition = $"attachment; filename={downloadFileName}",
        };
        return blob.GenerateSasUri(sas);
    }
}
