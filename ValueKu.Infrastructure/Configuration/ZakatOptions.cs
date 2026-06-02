namespace ValueKu.Infrastructure.Configuration;

/// <summary>Zakat calculation parameters (nisab changes yearly with the gold price).</summary>
public sealed class ZakatOptions
{
    public const string SectionName = "Zakat";

    /// <summary>Minimum wealth threshold (≈ value of 85g of gold). Default ~RM29,961 (2025).</summary>
    public decimal Nisab { get; set; } = 29_961m;

    /// <summary>Zakat rate as a percentage. Standard is 2.5%.</summary>
    public decimal Rate { get; set; } = 2.5m;
}
