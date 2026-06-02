using Microsoft.EntityFrameworkCore;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

public sealed class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _db;

    public ProfileService(ApplicationDbContext db) => _db = db;

    public Task<User?> GetAsync(int userId, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task<ProfileUpdateResult> UpdateAsync(int userId, ProfileUpdate update, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ProfileUpdateResult.Fail("User not found.");

        var email = update.Email.Trim();
        var emailTaken = await _db.Users.AnyAsync(u => u.Id != userId && u.Email == email, ct);
        if (emailTaken)
            return ProfileUpdateResult.Fail("That email is already in use by another account.");

        user.FirstName = update.FirstName.Trim();
        user.LastName = update.LastName.Trim();
        user.Email = email;

        var hasPhone = !string.IsNullOrWhiteSpace(update.PhoneNumber);
        user.PhoneNumber = hasPhone ? update.PhoneNumber!.Trim() : null;
        user.PhoneCountryCode = hasPhone ? update.PhoneCountryCode : null;

        await _db.SaveChangesAsync(ct);
        return ProfileUpdateResult.Ok();
    }

    public async Task SetAvatarAsync(int userId, string? avatarUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return;

        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync(ct);
    }
}
