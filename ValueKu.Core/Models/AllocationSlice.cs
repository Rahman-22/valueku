namespace ValueKu.Core.Models;

/// <summary>One slice of the asset-allocation donut chart.</summary>
public record AllocationSlice(string Category, decimal Value);
