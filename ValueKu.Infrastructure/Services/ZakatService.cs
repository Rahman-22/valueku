using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Configuration;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>
/// Calculates zakat on wealth. Eligible wealth = liquid (non-liability) account balances plus
/// assets in liquid/investment categories. Locked instruments (EPF, Tabung Haji) and
/// personal-use assets (home, vehicle) are excluded.
/// </summary>
public sealed class ZakatService : IZakatService
{
    private static readonly AssetCategory[] EligibleAssetCategories =
    [
        AssetCategory.Cash,
        AssetCategory.Equity,
        AssetCategory.Asb,
        AssetCategory.UnitTrust,
        AssetCategory.Other
    ];

    private readonly ApplicationDbContext _db;
    private readonly ZakatOptions _options;

    public ZakatService(ApplicationDbContext db, IOptions<ZakatOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<ZakatResult> CalculateAsync(int userId, CancellationToken ct = default)
    {
        var lines = new List<ZakatLine>();

        var accounts = await _db.Accounts.Where(a => a.UserId == userId).ToListAsync(ct);
        var liquid = accounts.Where(a => !a.IsLiability).Sum(a => a.Balance);
        if (liquid > 0)
            lines.Add(new ZakatLine("Cash & bank accounts", liquid));

        var assetGroups = await _db.Assets
            .Where(a => a.UserId == userId && EligibleAssetCategories.Contains(a.Category))
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(a => a.CurrentValue) })
            .ToListAsync(ct);

        foreach (var g in assetGroups.Where(x => x.Total > 0).OrderByDescending(x => x.Total))
            lines.Add(new ZakatLine(g.Category.ToString(), g.Total));

        var eligible = lines.Sum(l => l.Amount);
        var aboveNisab = eligible >= _options.Nisab;
        var payable = aboveNisab ? decimal.Round(eligible * _options.Rate / 100m, 2) : 0m;

        return new ZakatResult(lines, eligible, _options.Nisab, _options.Rate, aboveNisab, payable);
    }
}
