using System.ComponentModel.DataAnnotations.Schema;
using ValueKu.Core.Enums;

namespace ValueKu.Core.Entities;

/// <summary>A cash account or wallet. Liability-type accounts (credit card, loan) reduce net worth.</summary>
public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "MYR";

    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>True when the account represents money owed rather than money held.</summary>
    [NotMapped]
    public bool IsLiability => Type is AccountType.CreditCard or AccountType.Loan;
}
