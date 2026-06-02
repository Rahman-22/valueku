using System.ComponentModel.DataAnnotations;
using ValueKu.Core.Enums;

namespace ValueKu.ViewModels;

public class AssetFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AssetCategory Category { get; set; }

    [Display(Name = "Purchase Price (RM)")]
    [Range(0, 1_000_000_000)]
    public decimal PurchasePrice { get; set; }

    [Display(Name = "Purchase Date")]
    [DataType(DataType.Date)]
    public DateOnly PurchaseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Annual Rate (%)")]
    [Range(-100, 1000)]
    public decimal AppreciationDepreciationRate { get; set; }

    [Required]
    [Display(Name = "Calculation Method")]
    public CalculationType CalculationType { get; set; }
}
