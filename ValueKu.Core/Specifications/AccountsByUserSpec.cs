using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class AccountsByUserSpec : BaseSpecification<Account>
{
    public AccountsByUserSpec(int userId) : base(a => a.UserId == userId)
        => ApplyOrderBy(a => a.Name);
}
