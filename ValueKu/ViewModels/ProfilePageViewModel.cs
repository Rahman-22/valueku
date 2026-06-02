namespace ValueKu.ViewModels;

/// <summary>Backing model for the Profile page (Profile + Password tabs).</summary>
public class ProfilePageViewModel
{
    public ProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();

    // Display-only context.
    public string? AvatarUrl { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    /// <summary>False for Google-only accounts (no password to change).</summary>
    public bool HasPassword { get; set; }
}
