namespace ValueKu.Core.Models;

/// <summary>Output of the net-worth projection engine: the full curve plus headline horizons.</summary>
public record ProjectionResult(
    decimal CurrentNetWorth,
    decimal MonthlyNetCashFlow,
    IReadOnlyList<NetWorthPoint> Points,
    decimal At5Years,
    decimal At10Years,
    decimal At30Years);
