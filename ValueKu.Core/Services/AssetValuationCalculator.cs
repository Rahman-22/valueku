using ValueKu.Core.Entities;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;

namespace ValueKu.Core.Services;

/// <summary>
/// Pure, side-effect-free valuation math. Kept in Core so it is trivially unit-testable
/// and free of any I/O. Used by AssetValuationService and the projection engine.
/// </summary>
public sealed class AssetValuationCalculator : IAssetValuationCalculator
{
    private const double DaysPerYear = 365.25;

    public decimal CalculateValue(Asset asset, DateOnly asOf)
    {
        // Before (or on) the purchase date the asset is simply worth what was paid.
        if (asOf <= asset.PurchaseDate)
            return decimal.Round(asset.PurchasePrice, 2, MidpointRounding.AwayFromZero);

        var years = (asOf.DayNumber - asset.PurchaseDate.DayNumber) / DaysPerYear;
        var rate = (double)asset.AppreciationDepreciationRate / 100.0; // % -> fraction per year
        var principal = (double)asset.PurchasePrice;

        var value = asset.CalculationType switch
        {
            // Straight-line: a fixed fraction of the original principal each year.
            CalculationType.Linear => principal * (1 + rate * years),
            // Compounding: the rate applies to the running balance each year.
            CalculationType.Compounding => principal * Math.Pow(1 + rate, years),
            _ => principal
        };

        if (double.IsNaN(value) || double.IsInfinity(value))
            value = principal;

        // Value can never go below zero (e.g. a fully depreciated vehicle).
        value = Math.Max(0, value);

        return decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }
}
