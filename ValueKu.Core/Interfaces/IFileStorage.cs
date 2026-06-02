namespace ValueKu.Core.Interfaces;

/// <summary>Stores user-uploaded files (profile pictures). Backed by local disk or Azure Blob.</summary>
public interface IFileStorage
{
    /// <summary>Saves an avatar image and returns the URL to display it.</summary>
    Task<string> SaveAvatarAsync(Stream content, string extension, string contentType, CancellationToken ct = default);

    /// <summary>Deletes a previously saved avatar (best-effort).</summary>
    Task DeleteAvatarAsync(string? url, CancellationToken ct = default);
}
