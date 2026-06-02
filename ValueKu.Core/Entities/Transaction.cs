using System.ComponentModel.DataAnnotations.Schema;
using ValueKu.Core.Enums;

namespace ValueKu.Core.Entities;

/// <summary>An income or expense movement against an account.</summary>
public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public bool IsRecurring { get; set; }

    public Account? Account { get; set; }

    /// <summary>Income is positive, expense is negative — convenient for aggregation.</summary>
    [NotMapped]
    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;
}
