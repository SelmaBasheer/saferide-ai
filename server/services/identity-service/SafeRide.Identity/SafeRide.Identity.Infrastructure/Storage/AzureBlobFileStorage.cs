using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using SafeRide.Identity.Application.Abstractions;

namespace SafeRide.Identity.Infrastructure.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly Uri? _publicEndpoint;

    public AzureBlobFileStorage(IConfiguration config)
    {
        var conn =
            config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not configured."
            );

        var containerName = config["AzureStorage:Container"] ?? "profile-images";

        // The address this service reaches storage on and the address a browser
        // reaches it on are not the same in Docker: we connect to "azurite",
        // which does not resolve outside the compose network. The signature
        // covers the account, container and blob — never the hostname — so the
        // host can be swapped afterwards without breaking it.
        var publicEndpoint = config["AzureStorage:PublicEndpoint"];

        _publicEndpoint = string.IsNullOrWhiteSpace(publicEndpoint)
            ? null
            : new Uri(publicEndpoint);

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

        await blob.UploadAsync(content, options, ct); // overwrites the existing photo
    }

    public Uri GetReadUrl(string key, TimeSpan validFor)
    {
        var blob = _container.GetBlobClient(key);

        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(validFor));
        var uri = blob.GenerateSasUri(sas);

        if (_publicEndpoint is null)
        {
            return uri;
        }

        return new UriBuilder(uri)
        {
            Scheme = _publicEndpoint.Scheme,
            Host = _publicEndpoint.Host,
            Port = _publicEndpoint.Port,
        }.Uri;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default) =>
        await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct);
}
