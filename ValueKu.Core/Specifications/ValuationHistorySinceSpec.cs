using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

/// <summary>All valuation-history rows for a user's assets recorded on or after a given date.</summary>
public sealed class ValuationHistorySinceSpec : BaseSpecification<AssetValuationHistory>
{
    public ValuationHistorySinceSpec(int userId, DateOnly since)
        : base(h => h.Asset!.UserId == userId && h.RecordedDate >= since)
    {
        AddInclude(h => h.Asset!);
        ApplyOrderBy(h => h.RecordedDate);
    }
}
