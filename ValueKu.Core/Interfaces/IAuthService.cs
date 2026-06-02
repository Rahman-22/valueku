using ValueKu.Core.Entities;

namespace ValueKu.Core.Interfaces;

/// <summary>Credential validation, registration and password changes (hashing lives behind this).</summary>
public interface IAuthService
{
    /// <summary>Returns the user when credentials are valid, otherwise null.</summary>
    Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password, CancellationToken ct = default);

    /// <summary>Creates a new user. Returns success plus the created user, or an error message.</summary>
    Task<RegistrationResult> RegisterAsync(string username, string email, string password, CancellationToken ct = default);

    /// <summary>Changes a user's password after verifying the current one.</summary>
    Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Finds the user linked to the given Google id, links Google to an existing account with the
    /// same email, or provisions a new (passwordless) account. Always returns a user.
    /// </summary>
    Task<User> FindOrCreateGoogleUserAsync(string googleId, string email, string? name, CancellationToken ct = default);
}

/// <summary>Outcome of a registration attempt.</summary>
public record RegistrationResult(bool Success, User? User, string? Error)
{
    public static RegistrationResult Ok(User user) => new(true, user, null);
    public static RegistrationResult Fail(string error) => new(false, null, error);
}

/// <summary>Outcome of a password-change attempt.</summary>
public record PasswordChangeResult(bool Success, string? Error)
{
    public static PasswordChangeResult Ok() => new(true, null);
    public static PasswordChangeResult Fail(string error) => new(false, error);
}
