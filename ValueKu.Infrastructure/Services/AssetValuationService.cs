using Microsoft.EntityFrameworkCore;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Depreciation/appreciation engine. Recomputes each asset's CurrentValue via the pure
/// calculator and appends (or updates) the day's AssetValuationHistory row.
/// </summary>
public sealed class AssetValuationService : IAssetValuationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAssetValuationCalculator _calculator;
    private readonly INetWorthService _netWorth;

    public AssetValuationService(ApplicationDbContext db, IAssetValuationCalculator calculator, INetWorthService netWorth)
    {
        _db = db;
        _calculator = calculator;
        _netWorth = netWorth;
    }

    public async Task<int> RevalueAllAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var userIds = await _db.Assets.Select(a => a.UserId).Distinct().ToListAsync(ct);

        var total = 0;
        foreach (var userId in userIds)
            total += await RevalueUserInternalAsync(userId, asOf, ct);

        return total;
    }

    public Task<int> RevalueUserAsync(int userId, DateOnly asOf, CancellationToken ct = default)
        => RevalueUserInternalAsync(userId, asOf, ct);

    private async Task<int> RevalueUserInternalAsync(int userId, DateOnly asOf, CancellationToken ct)
    {
        var assets = await _db.Assets.Where(a => a.UserId == userId).ToListAsync(ct);
        if (assets.Count == 0)
            return 0;

        foreach (var asset in assets)
        {
            var value = _calculator.CalculateValue(asset, asOf);
            asset.CurrentValue = value;

            var existing = await _db.AssetValuationHistory
                .FirstOrDefaultAsync(h => h.AssetId == asset.Id && h.RecordedDate == asOf, ct);

            if (existing is null)
                _db.AssetValuationHistory.Add(new AssetValuationHistory
                {
                    AssetId = asset.Id,
                    Value = value,
                    RecordedDate = asOf
                });
            else
                existing.Value = value;
        }

        await _db.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(userId, ct);

        return assets.Count;
    }
}
