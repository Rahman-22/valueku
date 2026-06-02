using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

/// <summary>All transactions belonging to a user (across their accounts), optionally bounded by a date range.</summary>
public sealed class TransactionsByUserSpec : BaseSpecification<Transaction>
{
    public TransactionsByUserSpec(int userId, DateTime? from = null, DateTime? to = null)
        : base(t => t.Account!.UserId == userId
                    && (from == null || t.TransactionDate >= from)
                    && (to == null || t.TransactionDate <= to))
    {
        AddInclude(t => t.Account!);
        ApplyOrderByDescending(t => t.TransactionDate);
    }
}
