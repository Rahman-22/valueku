using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Services;
using ValueKu.Infrastructure.BackgroundJobs;
using ValueKu.Infrastructure.Configuration;
using ValueKu.Infrastructure.Persistence;
using ValueKu.Infrastructure.Services;

namespace ValueKu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // QuestPDF runs under the free Community license.
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // .NET 9 HybridCache (in-memory L1; no distributed backplane required for this app).
        services.AddHybridCache();

        services.Configure<ValuationWorkerOptions>(configuration.GetSection(ValuationWorkerOptions.SectionName));
        services.Configure<SeedUserOptions>(configuration.GetSection(SeedUserOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<ZakatOptions>(configuration.GetSection(ZakatOptions.SectionName));

        // Avatar storage: Azure Blob when configured, otherwise the Web layer's local fallback.
        var blobConnection = configuration["Storage:BlobConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobConnection))
        {
            services.AddSingleton(new BlobServiceClient(blobConnection));
            services.AddSingleton<IFileStorage, BlobFileStorage>();
        }

        // Data access.
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DataSeeder>();

        // Stateless helpers.
        services.AddSingleton<IAssetValuationCalculator, AssetValuationCalculator>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Application services.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAssetValuationService, AssetValuationService>();
        services.AddScoped<INetWorthService, NetWorthService>();
        services.AddScoped<INetWorthProjectionService, NetWorthProjectionService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<IZakatService, ZakatService>();

        // Daily background re-valuation.
        services.AddHostedService<AssetValuationWorker>();

        return services;
    }
}
