using System.ComponentModel.DataAnnotations;
using ValueKu.Core.Enums;

namespace ValueKu.ViewModels;

public class BudgetFormViewModel
{
    public int Id { get; set; }

    [Required]
    public TransactionCategory Category { get; set; }

    [Display(Name = "Monthly Limit (RM)")]
    [Range(1, 1_000_000)]
    public decimal MonthlyLimit { get; set; }
}
