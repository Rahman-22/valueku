using ValueKu.Core.Enums;

namespace ValueKu.Core.Entities;

/// <summary>A physical or financial asset whose value changes over time.</summary>
public class Asset
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; }

    public decimal PurchasePrice { get; set; }
    public DateOnly PurchaseDate { get; set; }

    /// <summary>Latest computed value (refreshed by the valuation worker / on edit).</summary>
    public decimal CurrentValue { get; set; }

    public string Currency { get; set; } = "MYR";

    /// <summary>Annual rate as a percentage. Positive = appreciation, negative = depreciation.</summary>
    public decimal AppreciationDepreciationRate { get; set; }

    public CalculationType CalculationType { get; set; }

    public User? User { get; set; }
    public ICollection<AssetValuationHistory> ValuationHistory { get; set; } = new List<AssetValuationHistory>();
}
