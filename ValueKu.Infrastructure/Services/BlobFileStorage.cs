using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ValueKu.Core.Interfaces;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Stores avatars in an Azure Blob Storage container (public read), returning the blob URL.
/// Used in cloud deployments so uploads survive restarts/redeploys.
/// </summary>
public sealed class BlobFileStorage : IFileStorage
{
    private const string ContainerName = "avatars";
    private readonly BlobServiceClient _client;

    public BlobFileStorage(BlobServiceClient client) => _client = client;

    public async Task<string> SaveAvatarAsync(Stream content, string extension, string contentType, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blob = container.GetBlobClient($"{Guid.NewGuid():N}{extension}");
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);

        return blob.Uri.ToString();
    }

    public async Task DeleteAvatarAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        try
        {
            var name = Path.GetFileName(uri.AbsolutePath);
            await _client.GetBlobContainerClient(ContainerName).GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
