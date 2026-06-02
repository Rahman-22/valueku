using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class ValuationHistoryByAssetSpec : BaseSpecification<AssetValuationHistory>
{
    public ValuationHistoryByAssetSpec(int assetId) : base(h => h.AssetId == assetId)
        => ApplyOrderBy(h => h.RecordedDate);
}
