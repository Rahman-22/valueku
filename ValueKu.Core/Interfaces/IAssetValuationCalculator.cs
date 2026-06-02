using ValueKu.Core.Entities;

namespace ValueKu.Core.Interfaces;

/// <summary>Pure domain math: computes an asset's value at a point in time from its profile.</summary>
public interface IAssetValuationCalculator
{
    decimal CalculateValue(Asset asset, DateOnly asOf);
}
