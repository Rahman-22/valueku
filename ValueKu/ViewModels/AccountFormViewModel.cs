using System.ComponentModel.DataAnnotations;
using ValueKu.Core.Enums;

namespace ValueKu.ViewModels;

public class AccountFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AccountType Type { get; set; }

    [Display(Name = "Balance (RM)")]
    public decimal Balance { get; set; }
}
