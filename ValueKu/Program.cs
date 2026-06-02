using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using ValueKu.Common;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure;
using ValueKu.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Clean Architecture composition root: DbContext, repositories, services, HybridCache, worker.
builder.Services.AddInfrastructure(builder.Configuration);

// Fall back to local-disk avatar storage when Azure Blob isn't configured.
builder.Services.TryAddSingleton<IFileStorage, LocalFileStorage>();

// Honour X-Forwarded-* headers from the Azure App Service reverse proxy so HTTPS redirects
// and the Google OAuth redirect_uri are built with the correct public https scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Google sign-in is optional: enabled only when both credentials are configured
// (appsettings or user-secrets). Without them, password login still works.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
builder.Services.AddSingleton(new GoogleAuthState(googleEnabled));

// Custom cookie authentication over the domain User entity (no full ASP.NET Identity).
var authBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

if (googleEnabled)
{
    // Temporary cookie that holds the Google identity until we map it to a local user.
    authBuilder.AddCookie(AuthConstants.ExternalScheme);
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SignInScheme = AuthConstants.ExternalScheme;
        // Default callback path is /signin-google (register this in the Google Cloud console).
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve runtime-uploaded files (profile pictures). MapStaticAssets only serves assets known
// at build time, so uploads under wwwroot/uploads need an explicit static-file handler.
var uploadsRoot = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// Apply migrations and seed the demo dataset on startup.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = services.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
