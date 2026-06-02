using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class AssetsByUserSpec : BaseSpecification<Asset>
{
    public AssetsByUserSpec(int userId) : base(a => a.UserId == userId)
        => ApplyOrderBy(a => a.Name);
}
