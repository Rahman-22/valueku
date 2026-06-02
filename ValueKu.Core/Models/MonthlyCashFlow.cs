namespace ValueKu.Core.Models;

/// <summary>Income vs expense totals for a single month.</summary>
public record MonthlyCashFlow(int Year, int Month, decimal Income, decimal Expense)
{
    public decimal Net => Income - Expense;
}
