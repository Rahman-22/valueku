namespace ValueKu.Core.Models;

/// <summary>A point-in-time balance sheet: physical assets + liquid accounts vs liabilities.</summary>
public record NetWorthSnapshot(decimal PhysicalAssets, decimal LiquidAccounts, decimal Liabilities)
{
    public decimal TotalAssets => PhysicalAssets + LiquidAccounts;
    public decimal NetWorth => TotalAssets - Liabilities;
}
