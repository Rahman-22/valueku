using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class SavingsGoalsByUserSpec : BaseSpecification<SavingsGoal>
{
    public SavingsGoalsByUserSpec(int userId) : base(g => g.UserId == userId)
        => ApplyOrderBy(g => g.TargetDate);
}
