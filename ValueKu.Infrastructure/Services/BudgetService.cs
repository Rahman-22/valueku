using Microsoft.EntityFrameworkCore;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

public sealed class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _db;

    public BudgetService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BudgetStatus>> GetStatusAsync(int userId, int year, int month, CancellationToken ct = default)
    {
        var budgets = await _db.Budgets.Where(b => b.UserId == userId).ToListAsync(ct);
        if (budgets.Count == 0)
            return [];

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var spend = await _db.Transactions
            .Where(t => t.Account!.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= start && t.TransactionDate < end)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var spendMap = spend.ToDictionary(x => x.Category, x => x.Total);

        return budgets
            .Select(b => new BudgetStatus(b.Id, b.Category, b.MonthlyLimit, spendMap.GetValueOrDefault(b.Category, 0m)))
            .OrderByDescending(s => s.Percent)
            .ToList();
    }
}
