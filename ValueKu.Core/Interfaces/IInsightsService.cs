using ValueKu.Core.Models;

namespace ValueKu.Core.Interfaces;

/// <summary>Spending analytics for the dashboard (cached per user).</summary>
public interface IInsightsService
{
    Task<IReadOnlyList<CashFlowPoint>> GetCashFlowSeriesAsync(int userId, int months = 6, CancellationToken ct = default);
    Task<IReadOnlyList<AllocationSlice>> GetSpendingByCategoryAsync(int userId, int year, int month, CancellationToken ct = default);

    /// <summary>Current-month savings rate: (income − expense) / income, 0 when no income.</summary>
    Task<decimal> GetSavingsRateAsync(int userId, CancellationToken ct = default);
}
