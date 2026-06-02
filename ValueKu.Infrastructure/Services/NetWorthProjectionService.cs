using Microsoft.EntityFrameworkCore;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Predictive net-worth engine. Projects each asset forward using its own appreciation
/// rate/calculation type, and grows the liquid (account) side by the user's historical
/// average monthly net cash flow. Produces a 30-year curve plus 5/10/30-year headlines.
/// </summary>
public sealed class NetWorthProjectionService : INetWorthProjectionService
{
    private readonly ApplicationDbContext _db;
    private readonly INetWorthService _netWorth;
    private readonly IAssetValuationCalculator _calculator;

    public NetWorthProjectionService(
        ApplicationDbContext db,
        INetWorthService netWorth,
        IAssetValuationCalculator calculator)
    {
        _db = db;
        _netWorth = netWorth;
        _calculator = calculator;
    }

    public async Task<ProjectionResult> ProjectAsync(int userId, CancellationToken ct = default)
    {
        var snapshot = await _netWorth.GetSnapshotAsync(userId, ct);
        var assets = await _db.Assets.Where(a => a.UserId == userId).ToListAsync(ct);
        var monthlyNet = await ComputeMonthlyNetAsync(userId, ct);

        // The account side today, net of liabilities. Assets are projected individually below.
        var baselineAccounts = snapshot.LiquidAccounts - snapshot.Liabilities;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        decimal ValueAt(int monthsAhead)
        {
            var date = today.AddMonths(monthsAhead);
            var assetSum = assets.Sum(a => _calculator.CalculateValue(a, date));
            var accountSum = baselineAccounts + monthlyNet * monthsAhead;
            return decimal.Round(assetSum + accountSum, 2);
        }

        // Yearly points for charting (0..30 years).
        var points = new List<NetWorthPoint>(31);
        for (var year = 0; year <= 30; year++)
            points.Add(new NetWorthPoint(today.AddYears(year), ValueAt(year * 12)));

        return new ProjectionResult(
            snapshot.NetWorth,
            monthlyNet,
            points,
            ValueAt(60),
            ValueAt(120),
            ValueAt(360));
    }

    private async Task<decimal> ComputeMonthlyNetAsync(int userId, CancellationToken ct)
    {
        var txns = await _db.Transactions
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { t.TransactionDate, t.Type, t.Amount })
            .ToListAsync(ct);

        if (txns.Count == 0)
            return 0m;

        var monthlyNets = txns
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => g.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount))
            .ToList();

        return monthlyNets.Count == 0 ? 0m : decimal.Round(monthlyNets.Average(), 2);
    }
}
