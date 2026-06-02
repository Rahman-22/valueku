using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Spending analytics for the dashboard. Cached under the same <c>user:{id}</c> tag as
/// the net-worth metrics, so existing INetWorthService.InvalidateAsync clears these too.
/// </summary>
public sealed class InsightsService : IInsightsService
{
    private readonly ApplicationDbContext _db;
    private readonly HybridCache _cache;

    public InsightsService(ApplicationDbContext db, HybridCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string UserTag(int userId) => $"user:{userId}";

    public async Task<IReadOnlyList<CashFlowPoint>> GetCashFlowSeriesAsync(int userId, int months = 6, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"insights:cashflow:{userId}:{months}",
            async token => await ComputeCashFlowAsync(userId, months, token),
            tags: [UserTag(userId)], cancellationToken: ct);

    public async Task<IReadOnlyList<AllocationSlice>> GetSpendingByCategoryAsync(int userId, int year, int month, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"insights:spend:{userId}:{year}:{month}",
            async token => await ComputeSpendingAsync(userId, year, month, token),
            tags: [UserTag(userId)], cancellationToken: ct);

    public async Task<decimal> GetSavingsRateAsync(int userId, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"insights:savingsrate:{userId}",
            async token => await ComputeSavingsRateAsync(userId, token),
            tags: [UserTag(userId)], cancellationToken: ct);

    private async Task<IReadOnlyList<CashFlowPoint>> ComputeCashFlowAsync(int userId, int months, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var rows = await _db.Transactions
            .Where(t => t.Account!.UserId == userId && t.TransactionDate >= firstMonth)
            .Select(t => new { t.TransactionDate, t.Type, t.Amount })
            .ToListAsync(ct);

        var points = new List<CashFlowPoint>(months);
        for (var i = 0; i < months; i++)
        {
            var m = firstMonth.AddMonths(i);
            var monthRows = rows.Where(r => r.TransactionDate.Year == m.Year && r.TransactionDate.Month == m.Month).ToList();
            var income = monthRows.Where(r => r.Type == TransactionType.Income).Sum(r => r.Amount);
            var expense = monthRows.Where(r => r.Type == TransactionType.Expense).Sum(r => r.Amount);
            points.Add(new CashFlowPoint(DateOnly.FromDateTime(m), income, expense));
        }
        return points;
    }

    private async Task<IReadOnlyList<AllocationSlice>> ComputeSpendingAsync(int userId, int year, int month, CancellationToken ct)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var grouped = await _db.Transactions
            .Where(t => t.Account!.UserId == userId && t.Type == TransactionType.Expense
                        && t.TransactionDate >= start && t.TransactionDate < end)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        return grouped
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .Select(x => new AllocationSlice(x.Category.ToString(), x.Total))
            .ToList();
    }

    private async Task<decimal> ComputeSavingsRateAsync(int userId, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var start = new DateTime(today.Year, today.Month, 1);
        var end = start.AddMonths(1);

        var rows = await _db.Transactions
            .Where(t => t.Account!.UserId == userId && t.TransactionDate >= start && t.TransactionDate < end)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync(ct);

        var income = rows.Where(r => r.Type == TransactionType.Income).Sum(r => r.Amount);
        var expense = rows.Where(r => r.Type == TransactionType.Expense).Sum(r => r.Amount);

        return income <= 0 ? 0m : decimal.Round((income - expense) / income * 100m, 1);
    }
}
