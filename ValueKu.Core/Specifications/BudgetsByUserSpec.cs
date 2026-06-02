using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class BudgetsByUserSpec : BaseSpecification<Budget>
{
    public BudgetsByUserSpec(int userId) : base(b => b.UserId == userId) { }
}
