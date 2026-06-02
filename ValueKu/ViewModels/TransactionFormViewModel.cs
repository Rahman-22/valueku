using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ValueKu.Core.Enums;

namespace ValueKu.ViewModels;

public class TransactionFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Account")]
    public int AccountId { get; set; }

    [Display(Name = "Amount (RM)")]
    [Range(0.01, 1_000_000_000)]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public TransactionCategory Category { get; set; }

    [Display(Name = "Date")]
    [DataType(DataType.DateTime)]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    [StringLength(256)]
    public string? Description { get; set; }

    [Display(Name = "Recurring")]
    public bool IsRecurring { get; set; }

    /// <summary>Populated by the controller for the account dropdown.</summary>
    public IEnumerable<SelectListItem> Accounts { get; set; } = [];
}
