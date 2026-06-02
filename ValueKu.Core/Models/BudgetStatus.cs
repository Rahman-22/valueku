using ValueKu.Core.Enums;

namespace ValueKu.Core.Models;

/// <summary>A budget's limit vs actual spend for a given month.</summary>
public record BudgetStatus(int BudgetId, TransactionCategory Category, decimal Limit, decimal Spent)
{
    public decimal Remaining => Limit - Spent;
    public double Percent => Limit <= 0 ? 0 : (double)(Spent / Limit) * 100.0;
    public bool IsOver => Spent > Limit;
}
