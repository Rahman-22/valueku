namespace ValueKu.Core.Models;

/// <summary>A single (date, value) point on a net-worth trend or projection series.</summary>
public record NetWorthPoint(DateOnly Date, decimal Value);
