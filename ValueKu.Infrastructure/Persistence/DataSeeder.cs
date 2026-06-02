using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ValueKu.Core.Entities;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure.Configuration;

namespace ValueKu.Infrastructure.Persistence;

/// <summary>
/// Seeds the default user plus a realistic demo portfolio (assets, accounts, a year of
/// transactions, valuation history, budgets and savings goals). Seeding is idempotent per
/// entity group, so an already-seeded database gains newly-added demo data without being wiped.
/// </summary>
public sealed class DataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IAssetValuationCalculator _calculator;
    private readonly SeedUserOptions _seedUser;
    private readonly SeedOptions _seed;

    public DataSeeder(
        ApplicationDbContext db,
        IPasswordHasher<User> hasher,
        IAssetValuationCalculator calculator,
        IOptions<SeedUserOptions> seedUser,
        IOptions<SeedOptions> seed)
    {
        _db = db;
        _hasher = hasher;
        _calculator = calculator;
        _seedUser = seedUser.Value;
        _seed = seed.Value;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // The admin user is always seeded so the app is usable; demo data is optional.
        if (!await _db.Users.AnyAsync(ct))
        {
            await SeedAdminUserAsync(ct);
            if (_seed.DemoData)
                await SeedPortfolioAsync(today, ct);
        }

        var user = await _db.Users.OrderBy(u => u.Id).FirstOrDefaultAsync(ct);
        if (user is null)
            return;

        // Backfill profile fields for an already-seeded admin that predates these columns.
        if (string.IsNullOrWhiteSpace(user.FirstName))
        {
            user.FirstName = "Admin";
            user.LastName = "User";
            user.PhoneCountryCode ??= "+60";
            user.PhoneNumber ??= "123456789";
        }

        if (_seed.DemoData)
        {
            if (!await _db.Budgets.AnyAsync(b => b.UserId == user.Id, ct))
                SeedBudgets(user.Id);

            if (!await _db.SavingsGoals.AnyAsync(g => g.UserId == user.Id, ct))
                SeedGoals(user.Id, today);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        var user = new User
        {
            Username = _seedUser.Username,
            Email = _seedUser.Email,
            FirstName = "Admin",
            LastName = "User",
            PhoneCountryCode = "+60",
            PhoneNumber = "123456789",
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, _seedUser.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedPortfolioAsync(DateOnly today, CancellationToken ct)
    {
        var user = await _db.Users.OrderBy(u => u.Id).FirstAsync(ct);

        var assets = new List<Asset>
        {
            new() { UserId = user.Id, Name = "Family Home", Category = AssetCategory.RealEstate, PurchasePrice = 550_000m, PurchaseDate = today.AddYears(-6), AppreciationDepreciationRate = 5.0m, CalculationType = CalculationType.Compounding },
            new() { UserId = user.Id, Name = "Perodua Myvi", Category = AssetCategory.Vehicle, PurchasePrice = 55_000m, PurchaseDate = today.AddYears(-3), AppreciationDepreciationRate = -12.0m, CalculationType = CalculationType.Linear },
            new() { UserId = user.Id, Name = "ASB Investment", Category = AssetCategory.Asb, PurchasePrice = 80_000m, PurchaseDate = today.AddYears(-4), AppreciationDepreciationRate = 6.5m, CalculationType = CalculationType.Compounding },
            new() { UserId = user.Id, Name = "EPF Savings", Category = AssetCategory.Epf, PurchasePrice = 120_000m, PurchaseDate = today.AddYears(-8), AppreciationDepreciationRate = 5.5m, CalculationType = CalculationType.Compounding },
            new() { UserId = user.Id, Name = "Gold Bullion", Category = AssetCategory.Other, PurchasePrice = 30_000m, PurchaseDate = today.AddYears(-2), AppreciationDepreciationRate = 8.0m, CalculationType = CalculationType.Compounding },
        };
        foreach (var asset in assets)
            asset.CurrentValue = _calculator.CalculateValue(asset, today);
        _db.Assets.AddRange(assets);

        var accounts = new List<Account>
        {
            new() { UserId = user.Id, Name = "Maybank Current", Type = AccountType.Checking, Balance = 12_000m },
            new() { UserId = user.Id, Name = "CIMB Savings", Type = AccountType.Savings, Balance = 45_000m },
            new() { UserId = user.Id, Name = "Rakuten Trade", Type = AccountType.Investment, Balance = 25_000m },
            new() { UserId = user.Id, Name = "Touch 'n Go eWallet", Type = AccountType.EWallet, Balance = 850m },
            new() { UserId = user.Id, Name = "AmBank Credit Card", Type = AccountType.CreditCard, Balance = 3_500m },
        };
        _db.Accounts.AddRange(accounts);
        await _db.SaveChangesAsync(ct);

        SeedTransactions(accounts, today);
        SeedValuationHistory(assets, today);

        await _db.SaveChangesAsync(ct);
    }

    private void SeedTransactions(IReadOnlyList<Account> accounts, DateOnly today)
    {
        var checking = accounts[0];
        var card = accounts[^1];
        var random = new Random(42);
        var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);

        for (var monthsAgo = 11; monthsAgo >= 0; monthsAgo--)
        {
            var monthStart = firstOfThisMonth.AddMonths(-monthsAgo);

            _db.Transactions.AddRange(
                new Transaction { AccountId = checking.Id, Amount = 8_500m, Type = TransactionType.Income, Category = TransactionCategory.Salary, TransactionDate = monthStart.AddDays(1), Description = "Monthly salary", IsRecurring = true },
                new Transaction { AccountId = checking.Id, Amount = 1_800m, Type = TransactionType.Expense, Category = TransactionCategory.Housing, TransactionDate = monthStart.AddDays(3), Description = "Home loan instalment", IsRecurring = true },
                new Transaction { AccountId = checking.Id, Amount = 900m + random.Next(0, 400), Type = TransactionType.Expense, Category = TransactionCategory.Food, TransactionDate = monthStart.AddDays(10), Description = "Groceries & dining" },
                new Transaction { AccountId = checking.Id, Amount = 350m + random.Next(0, 150), Type = TransactionType.Expense, Category = TransactionCategory.Transport, TransactionDate = monthStart.AddDays(12), Description = "Fuel & tolls" },
                new Transaction { AccountId = checking.Id, Amount = 280m, Type = TransactionType.Expense, Category = TransactionCategory.Utilities, TransactionDate = monthStart.AddDays(20), Description = "Electricity, water, internet", IsRecurring = true },
                new Transaction { AccountId = card.Id, Amount = 500m + random.Next(0, 600), Type = TransactionType.Expense, Category = TransactionCategory.Entertainment, TransactionDate = monthStart.AddDays(18), Description = "Lifestyle & subscriptions" });
        }
    }

    private void SeedValuationHistory(IReadOnlyList<Asset> assets, DateOnly today)
    {
        foreach (var asset in assets)
        {
            for (var monthsAgo = 12; monthsAgo >= 0; monthsAgo--)
            {
                var date = today.AddMonths(-monthsAgo);
                if (date < asset.PurchaseDate)
                    continue;

                _db.AssetValuationHistory.Add(new AssetValuationHistory
                {
                    AssetId = asset.Id,
                    Value = _calculator.CalculateValue(asset, date),
                    RecordedDate = date
                });
            }
        }
    }

    private void SeedBudgets(int userId)
    {
        _db.Budgets.AddRange(
            new Budget { UserId = userId, Category = TransactionCategory.Food, MonthlyLimit = 1_500m },
            new Budget { UserId = userId, Category = TransactionCategory.Transport, MonthlyLimit = 600m },
            new Budget { UserId = userId, Category = TransactionCategory.Entertainment, MonthlyLimit = 800m },
            new Budget { UserId = userId, Category = TransactionCategory.Utilities, MonthlyLimit = 400m });
    }

    private void SeedGoals(int userId, DateOnly today)
    {
        _db.SavingsGoals.AddRange(
            new SavingsGoal { UserId = userId, Name = "Emergency Fund", TargetAmount = 30_000m, CurrentAmount = 18_000m, TargetDate = today.AddMonths(12) },
            new SavingsGoal { UserId = userId, Name = "Umrah Trip", TargetAmount = 15_000m, CurrentAmount = 5_000m, TargetDate = today.AddMonths(18) });
    }
}
