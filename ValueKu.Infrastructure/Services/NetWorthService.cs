using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Computes net-worth, 12-month trend and asset-allocation metrics, caching each result
/// in the .NET 9 <see cref="HybridCache"/> under a per-user tag so a single mutation
/// (asset/account/transaction change) can invalidate everything for that user at once.
/// </summary>
public sealed class NetWorthService : INetWorthService
{
    private readonly ApplicationDbContext _db;
    private readonly HybridCache _cache;

    public NetWorthService(ApplicationDbContext db, HybridCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string UserTag(int userId) => $"user:{userId}";

    public async Task<NetWorthSnapshot> GetSnapshotAsync(int userId, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"networth:snapshot:{userId}",
            async token => await ComputeSnapshotAsync(userId, token),
            tags: [UserTag(userId)],
            cancellationToken: ct);

    public async Task<IReadOnlyList<NetWorthPoint>> GetHistoryAsync(int userId, int months = 12, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"networth:history:{userId}:{months}",
            async token => await ComputeHistoryAsync(userId, months, token),
            tags: [UserTag(userId)],
            cancellationToken: ct);

    public async Task<IReadOnlyList<AllocationSlice>> GetAllocationAsync(int userId, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            $"assets:allocation:{userId}",
            async token => await ComputeAllocationAsync(userId, token),
            tags: [UserTag(userId)],
            cancellationToken: ct);

    public Task InvalidateAsync(int userId, CancellationToken ct = default)
        => _cache.RemoveByTagAsync(UserTag(userId), ct).AsTask();

    // ---- computations -------------------------------------------------------

    private async Task<NetWorthSnapshot> ComputeSnapshotAsync(int userId, CancellationToken ct)
    {
        var physicalAssets = await _db.Assets
            .Where(a => a.UserId == userId)
            .SumAsync(a => (decimal?)a.CurrentValue, ct) ?? 0m;

        // IsLiability is a [NotMapped] domain rule, so partition in memory.
        var accounts = await _db.Accounts.Where(a => a.UserId == userId).ToListAsync(ct);
        var liquid = accounts.Where(a => !a.IsLiability).Sum(a => a.Balance);
        var liabilities = accounts.Where(a => a.IsLiability).Sum(a => a.Balance);

        return new NetWorthSnapshot(physicalAssets, liquid, liabilities);
    }

    private async Task<IReadOnlyList<AllocationSlice>> ComputeAllocationAsync(int userId, CancellationToken ct)
    {
        var byCategory = await _db.Assets
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key, Value = g.Sum(a => a.CurrentValue) })
            .ToListAsync(ct);

        var slices = byCategory
            .Where(x => x.Value != 0)
            .Select(x => new AllocationSlice(Friendly(x.Category), x.Value))
            .ToList();

        var liquid = (await _db.Accounts.Where(a => a.UserId == userId).ToListAsync(ct))
            .Where(a => !a.IsLiability)
            .Sum(a => a.Balance);

        if (liquid != 0)
            slices.Add(new AllocationSlice("Cash & Bank", liquid));

        return slices;
    }

    private async Task<IReadOnlyList<NetWorthPoint>> ComputeHistoryAsync(int userId, int months, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthEnds = Enumerable.Range(0, months)
            .Select(i => EndOfMonth(today.AddMonths(-(months - 1 - i))))
            .ToList();

        var history = await _db.AssetValuationHistory
            .Where(h => h.Asset!.UserId == userId && h.RecordedDate <= today)
            .Select(h => new { h.AssetId, h.RecordedDate, h.Value })
            .ToListAsync(ct);

        var accounts = await _db.Accounts.Where(a => a.UserId == userId).ToListAsync(ct);
        var txns = await _db.Transactions
            .Where(t => t.Account!.UserId == userId)
            .Select(t => new { t.AccountId, t.TransactionDate, t.Type, t.Amount })
            .ToListAsync(ct);

        var points = new List<NetWorthPoint>(monthEnds.Count);
        foreach (var monthEnd in monthEnds)
        {
            var cutoff = monthEnd.ToDateTime(TimeOnly.MaxValue);

            // Asset side: latest recorded value at-or-before the month end, per asset.
            var assetValue = history
                .Where(h => h.RecordedDate <= monthEnd)
                .GroupBy(h => h.AssetId)
                .Sum(g => g.OrderByDescending(h => h.RecordedDate).First().Value);

            // Account side: reconstruct historical balance by removing transactions after the cutoff.
            decimal liquid = 0m, liabilities = 0m;
            foreach (var acc in accounts)
            {
                var netAfter = txns
                    .Where(t => t.AccountId == acc.Id && t.TransactionDate > cutoff)
                    .Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

                var balanceAt = acc.Balance - netAfter;
                if (acc.IsLiability) liabilities += balanceAt; else liquid += balanceAt;
            }

            points.Add(new NetWorthPoint(monthEnd, assetValue + liquid - liabilities));
        }

        return points;
    }

    private static DateOnly EndOfMonth(DateOnly d) => new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));

    private static string Friendly(AssetCategory category) => category switch
    {
        AssetCategory.RealEstate => "Real Estate",
        _ => category.ToString()
    };
}
