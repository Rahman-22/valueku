using ValueKu.Core.Entities;
using ValueKu.Core.Models;

namespace ValueKu.ViewModels;

public class DashboardViewModel
{
    public NetWorthSnapshot Snapshot { get; set; } = new(0, 0, 0);
    public decimal MonthlyNetCashFlow { get; set; }
    public IReadOnlyList<NetWorthPoint> History { get; set; } = [];
    public IReadOnlyList<AllocationSlice> Allocation { get; set; } = [];
    public ProjectionResult Projection { get; set; } = new(0, 0, [], 0, 0, 0);
    public IReadOnlyList<Transaction> RecentTransactions { get; set; } = [];

    // Spending insights.
    public decimal SavingsRate { get; set; }
    public IReadOnlyList<CashFlowPoint> CashFlow { get; set; } = [];
    public IReadOnlyList<AllocationSlice> SpendingByCategory { get; set; } = [];
    public IReadOnlyList<BudgetStatus> Budgets { get; set; } = [];

    /// <summary>Net-worth change over the trailing window (last point minus first point).</summary>
    public decimal HistoryChange =>
        History.Count >= 2 ? History[^1].Value - History[0].Value : 0m;
}
