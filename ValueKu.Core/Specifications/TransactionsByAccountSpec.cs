using ValueKu.Core.Entities;

namespace ValueKu.Core.Specifications;

public sealed class TransactionsByAccountSpec : BaseSpecification<Transaction>
{
    public TransactionsByAccountSpec(int accountId) : base(t => t.AccountId == accountId)
        => ApplyOrderByDescending(t => t.TransactionDate);
}
