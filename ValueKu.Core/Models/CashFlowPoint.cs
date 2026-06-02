namespace ValueKu.Core.Models;

/// <summary>Income vs expense totals for a single month (for the cash-flow chart).</summary>
public record CashFlowPoint(DateOnly Month, decimal Income, decimal Expense)
{
    public decimal Net => Income - Expense;
}
