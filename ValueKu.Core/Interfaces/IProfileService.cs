using ValueKu.Core.Entities;

namespace ValueKu.Core.Interfaces;

/// <summary>Reads and updates the user's profile (name, email, phone, avatar).</summary>
public interface IProfileService
{
    Task<User?> GetAsync(int userId, CancellationToken ct = default);
    Task<ProfileUpdateResult> UpdateAsync(int userId, ProfileUpdate update, CancellationToken ct = default);
    Task SetAvatarAsync(int userId, string? avatarUrl, CancellationToken ct = default);
}

/// <summary>Editable profile fields.</summary>
public record ProfileUpdate(string FirstName, string LastName, string Email, string? PhoneCountryCode, string? PhoneNumber);

/// <summary>Outcome of a profile update.</summary>
public record ProfileUpdateResult(bool Success, string? Error)
{
    public static ProfileUpdateResult Ok() => new(true, null);
    public static ProfileUpdateResult Fail(string error) => new(false, error);
}
