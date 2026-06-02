using ValueKu.Core.Models;

namespace ValueKu.Core.Interfaces;

/// <summary>Computes (and caches) net-worth, historical trend and asset-allocation metrics.</summary>
public interface INetWorthService
{
    Task<NetWorthSnapshot> GetSnapshotAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<NetWorthPoint>> GetHistoryAsync(int userId, int months = 12, CancellationToken ct = default);
    Task<IReadOnlyList<AllocationSlice>> GetAllocationAsync(int userId, CancellationToken ct = default);

    /// <summary>Drop all cached metrics for a user (call after any data mutation).</summary>
    Task InvalidateAsync(int userId, CancellationToken ct = default);
}
