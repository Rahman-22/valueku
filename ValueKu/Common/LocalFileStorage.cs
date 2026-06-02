using ValueKu.Core.Interfaces;

namespace ValueKu.Common;

/// <summary>Stores avatars under wwwroot/uploads/avatars. Default for local development.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAvatarAsync(Stream content, string extension, string contentType, CancellationToken ct = default)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(dir, fileName);
        await using (var stream = File.Create(fullPath))
            await content.CopyToAsync(stream, ct);

        return $"/uploads/avatars/{fileName}";
    }

    public Task DeleteAvatarAsync(string? url, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(url) && url.StartsWith('/'))
        {
            var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relative);
            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        return Task.CompletedTask;
    }
}
