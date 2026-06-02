using System.ComponentModel.DataAnnotations;

namespace ValueKu.ViewModels;

public class SavingsGoalFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Target Amount (RM)")]
    [Range(1, 1_000_000_000)]
    public decimal TargetAmount { get; set; }

    [Display(Name = "Saved So Far (RM)")]
    [Range(0, 1_000_000_000)]
    public decimal CurrentAmount { get; set; }

    [Display(Name = "Target Date")]
    [DataType(DataType.Date)]
    public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
}
