using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ValueKu.Common;
using ValueKu.Core.Interfaces;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly GoogleAuthState _google;

    public AccountController(IAuthService auth, GoogleAuthState google)
    {
        _auth = auth;
        _google = google;
    }

    // Surface the Google-enabled flag to every view rendered by this controller.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.GoogleEnabled = _google.Enabled;
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _auth.ValidateCredentialsAsync(model.UsernameOrEmail, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(model);
        }

        await SignInAsync(user);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.RegisterAsync(model.Username, model.Email, model.Password);
        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        await SignInAsync(result.User);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ---- External (Google) sign-in ----------------------------------------

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!_google.Enabled)
            return RedirectToAction(nameof(Login));

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!_google.Enabled || remoteError is not null)
            return RedirectToAction(nameof(Login));

        var result = await HttpContext.AuthenticateAsync(AuthConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal is null)
            return RedirectToAction(nameof(Login));

        var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));

        var user = await _auth.FindOrCreateGoogleUserAsync(googleId, email, name);

        await SignInAsync(user);
        await HttpContext.SignOutAsync(AuthConstants.ExternalScheme); // clear the temporary external cookie
        return RedirectToLocal(returnUrl);
    }

    private Task SignInAsync(ValueKu.Core.Entities.User user) => AuthCookieHelper.SignInAsync(HttpContext, user);

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Dashboard");
}
