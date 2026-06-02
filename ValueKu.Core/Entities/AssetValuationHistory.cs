namespace ValueKu.Core.Entities;

/// <summary>A point-in-time recording of an asset's value (one row per asset per day).</summary>
public class AssetValuationHistory
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public decimal Value { get; set; }
    public DateOnly RecordedDate { get; set; }

    public Asset? Asset { get; set; }
}
