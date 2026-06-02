using ValueKu.Core.Models;

namespace ValueKu.Core.Interfaces;

/// <summary>Computes per-category budget vs actual spend for a month.</summary>
public interface IBudgetService
{
    Task<IReadOnlyList<BudgetStatus>> GetStatusAsync(int userId, int year, int month, CancellationToken ct = default);
}
