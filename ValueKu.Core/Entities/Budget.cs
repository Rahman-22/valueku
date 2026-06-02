using ValueKu.Core.Enums;

namespace ValueKu.Core.Entities;

/// <summary>A monthly spending limit for a transaction category.</summary>
public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public TransactionCategory Category { get; set; }
    public decimal MonthlyLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
