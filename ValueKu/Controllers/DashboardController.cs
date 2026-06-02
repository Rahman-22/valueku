using Microsoft.AspNetCore.Mvc;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class DashboardController : AppControllerBase
{
    private readonly INetWorthService _netWorth;
    private readonly INetWorthProjectionService _projection;
    private readonly IInsightsService _insights;
    private readonly IBudgetService _budgets;
    private readonly IUnitOfWork _uow;

    public DashboardController(
        INetWorthService netWorth,
        INetWorthProjectionService projection,
        IInsightsService insights,
        IBudgetService budgets,
        IUnitOfWork uow)
    {
        _netWorth = netWorth;
        _projection = projection;
        _insights = insights;
        _budgets = budgets;
        _uow = uow;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var now = DateTime.Today;

        var snapshot = await _netWorth.GetSnapshotAsync(userId, ct);
        var history = await _netWorth.GetHistoryAsync(userId, 12, ct);
        var allocation = await _netWorth.GetAllocationAsync(userId, ct);
        var projection = await _projection.ProjectAsync(userId, ct);

        var recent = (await _uow.Repository<Transaction>().ListAsync(new TransactionsByUserSpec(userId), ct))
            .Take(8)
            .ToList();

        var vm = new DashboardViewModel
        {
            Snapshot = snapshot,
            MonthlyNetCashFlow = projection.MonthlyNetCashFlow,
            History = history,
            Allocation = allocation,
            Projection = projection,
            RecentTransactions = recent,
            SavingsRate = await _insights.GetSavingsRateAsync(userId, ct),
            CashFlow = await _insights.GetCashFlowSeriesAsync(userId, 6, ct),
            SpendingByCategory = await _insights.GetSpendingByCategoryAsync(userId, now.Year, now.Month, ct),
            Budgets = await _budgets.GetStatusAsync(userId, now.Year, now.Month, ct)
        };

        return View(vm);
    }
}
