using System.ComponentModel.DataAnnotations.Schema;

namespace ValueKu.Core.Entities;

/// <summary>Application user (single-user app, but data is always scoped by user).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Unique login handle (distinct from the display name).</summary>
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Display name (separate from the login Username).
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Contact number, split into dialling code + local number to match the UI.
    public string? PhoneCountryCode { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Relative URL of the uploaded profile picture, e.g. /uploads/avatars/{guid}.png.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Null for accounts that only sign in with an external provider (e.g. Google).</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Google subject id when the account is linked to Google sign-in; otherwise null.</summary>
    public string? GoogleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();

    /// <summary>Full display name, falling back to the username when no name is set.</summary>
    [NotMapped]
    public string DisplayName
    {
        get
        {
            var full = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrWhiteSpace(full) ? Username : full;
        }
    }
}
