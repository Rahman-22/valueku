using System.ComponentModel.DataAnnotations.Schema;

namespace ValueKu.Core.Entities;

/// <summary>A savings target the user is working toward by a chosen date.</summary>
public class SavingsGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly TargetDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    [NotMapped]
    public bool IsAchieved => TargetAmount > 0 && CurrentAmount >= TargetAmount;

    [NotMapped]
    public double Percent => TargetAmount <= 0 ? 0 : Math.Min(100, (double)(CurrentAmount / TargetAmount) * 100);
}
