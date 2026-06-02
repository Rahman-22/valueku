using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(ApplicationDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        var key = usernameOrEmail.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == key || u.Email == key, ct);

        // Passwordless (Google-only) accounts cannot sign in with a password.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<RegistrationResult> RegisterAsync(string username, string email, string password, CancellationToken ct = default)
    {
        username = username.Trim();
        email = email.Trim();

        var exists = await _db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);
        if (exists)
            return RegistrationResult.Fail("A user with that username or email already exists.");

        var user = new User { Username = username, Email = email, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return RegistrationResult.Ok(user);
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return PasswordChangeResult.Fail("User not found.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            return PasswordChangeResult.Fail("This account signs in with Google and has no password to change.");

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verify == PasswordVerificationResult.Failed)
            return PasswordChangeResult.Fail("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _db.SaveChangesAsync(ct);

        return PasswordChangeResult.Ok();
    }

    public async Task<User> FindOrCreateGoogleUserAsync(string googleId, string email, string? name, CancellationToken ct = default)
    {
        email = email.Trim();

        // 1) Already linked to this Google account.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);
        if (user is not null)
            return user;

        // 2) An existing local account with the same email — link Google to it.
        user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is not null)
        {
            user.GoogleId = googleId;
            await _db.SaveChangesAsync(ct);
            return user;
        }

        // 3) Provision a new passwordless account.
        var (firstName, lastName) = SplitName(name);
        user = new User
        {
            Username = await GenerateUniqueUsernameAsync(name, email, ct),
            Email = email,
            GoogleId = googleId,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = null,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private static (string? First, string? Last) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (null, null);

        var parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], null) : (parts[0], parts[1]);
    }

    private async Task<string> GenerateUniqueUsernameAsync(string? name, string email, CancellationToken ct)
    {
        var baseName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : email.Split('@')[0];
        if (baseName.Length > 60)
            baseName = baseName[..60];

        var candidate = baseName;
        var suffix = 1;
        while (await _db.Users.AnyAsync(u => u.Username == candidate, ct))
            candidate = $"{baseName}{++suffix}";

        return candidate;
    }
}
