using System.ComponentModel.DataAnnotations;

namespace ValueKu.Core.Enums;

/// <summary>High-level classification of a physical or financial asset.</summary>
public enum AssetCategory
{
    [Display(Name = "Real Estate")]
    RealEstate,
    Cash,
    Vehicle,
    Equity,
    [Display(Name = "EPF (KWSP)")]
    Epf,
    [Display(Name = "ASB / ASNB")]
    Asb,
    [Display(Name = "Unit Trust")]
    UnitTrust,
    [Display(Name = "Tabung Haji")]
    TabungHaji,
    Other
}
