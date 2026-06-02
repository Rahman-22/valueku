using Microsoft.AspNetCore.Mvc;
using ValueKu.Common;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class ProfileController : AppControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];
    private const long MaxAvatarBytes = 2 * 1024 * 1024;

    private readonly IProfileService _profile;
    private readonly IAuthService _auth;
    private readonly IFileStorage _files;

    public ProfileController(IProfileService profile, IAuthService auth, IFileStorage files)
    {
        _profile = profile;
        _auth = auth;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await BuildPageAsync(null, "profile", ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileViewModel profile, CancellationToken ct)
    {
        var avatarFile = profile.AvatarFile;
        if (avatarFile is { Length: > 0 } && !IsValidImage(avatarFile))
            ModelState.AddModelError("Profile.AvatarFile", "Upload an image up to 2 MB (png, jpg, webp or gif).");

        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildPageAsync(profile, "profile", ct));

        var result = await _profile.UpdateAsync(CurrentUserId,
            new ProfileUpdate(profile.FirstName, profile.LastName, profile.Email, profile.PhoneCountryCode, profile.PhoneNumber), ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update profile.");
            return View(nameof(Index), await BuildPageAsync(profile, "profile", ct));
        }

        if (avatarFile is { Length: > 0 })
        {
            var existing = await _profile.GetAsync(CurrentUserId, ct);
            await _files.DeleteAvatarAsync(existing?.AvatarUrl, ct);

            var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            await using var upload = avatarFile.OpenReadStream();
            var url = await _files.SaveAvatarAsync(upload, extension, avatarFile.ContentType, ct);
            await _profile.SetAvatarAsync(CurrentUserId, url, ct);
        }

        await RefreshCookieAsync(ct);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct)
    {
        var user = await _profile.GetAsync(CurrentUserId, ct);
        await _files.DeleteAvatarAsync(user?.AvatarUrl, ct);
        await _profile.SetAvatarAsync(CurrentUserId, null, ct);

        await RefreshCookieAsync(ct);
        TempData["Success"] = "Profile picture removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel password, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildPageAsync(null, "password", ct, password));

        var result = await _auth.ChangePasswordAsync(CurrentUserId, password.CurrentPassword, password.NewPassword, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not change password.");
            return View(nameof(Index), await BuildPageAsync(null, "password", ct, password));
        }

        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Index));
    }

    // ---- helpers ------------------------------------------------------------

    private async Task<ProfilePageViewModel> BuildPageAsync(
        ProfileViewModel? profile, string activeTab, CancellationToken ct, ChangePasswordViewModel? password = null)
    {
        var user = await _profile.GetAsync(CurrentUserId, ct);
        ViewBag.ActiveTab = activeTab;

        return new ProfilePageViewModel
        {
            Profile = profile ?? ToProfileVm(user),
            Password = password ?? new ChangePasswordViewModel(),
            AvatarUrl = user?.AvatarUrl,
            DisplayName = user?.DisplayName ?? string.Empty,
            Username = user?.Username ?? string.Empty,
            HasPassword = !string.IsNullOrEmpty(user?.PasswordHash)
        };
    }

    private static ProfileViewModel ToProfileVm(User? u) => new()
    {
        FirstName = u?.FirstName ?? string.Empty,
        LastName = u?.LastName ?? string.Empty,
        Email = u?.Email ?? string.Empty,
        PhoneCountryCode = string.IsNullOrEmpty(u?.PhoneCountryCode) ? "+60" : u.PhoneCountryCode,
        PhoneNumber = u?.PhoneNumber
    };

    private async Task RefreshCookieAsync(CancellationToken ct)
    {
        var user = await _profile.GetAsync(CurrentUserId, ct);
        if (user is not null)
            await AuthCookieHelper.SignInAsync(HttpContext, user);
    }

    private static bool IsValidImage(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return file.Length is > 0 and <= MaxAvatarBytes
               && AllowedImageExtensions.Contains(ext)
               && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
