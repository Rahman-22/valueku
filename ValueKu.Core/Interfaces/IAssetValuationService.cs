namespace ValueKu.Core.Interfaces;

/// <summary>
/// Orchestrates re-valuation: updates each asset's CurrentValue and appends a daily
/// AssetValuationHistory row. Used by the background worker and on-demand after edits.
/// </summary>
public interface IAssetValuationService
{
    /// <summary>Revalue every asset in the system as of the given date. Returns assets updated.</summary>
    Task<int> RevalueAllAsync(DateOnly asOf, CancellationToken ct = default);

    /// <summary>Revalue a single user's assets as of the given date. Returns assets updated.</summary>
    Task<int> RevalueUserAsync(int userId, DateOnly asOf, CancellationToken ct = default);
}
