namespace ValueKu.Infrastructure.Configuration;

/// <summary>Credentials for the default user seeded on first run.</summary>
public sealed class SeedUserOptions
{
    public const string SectionName = "SeedUser";

    public string Username { get; set; } = "admin";
    public string Email { get; set; } = "admin@valueku.local";

    /// <summary>Fallback only — set the real value via user-secrets (local) or App Service settings (prod).</summary>
    public string Password { get; set; } = "ChangeMe123!";
}
