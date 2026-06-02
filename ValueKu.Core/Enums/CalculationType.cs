namespace ValueKu.Core.Enums;

/// <summary>Strategy used to model an asset's value over time.</summary>
public enum CalculationType
{
    /// <summary>Straight-line: value changes by a fixed amount of the principal each year (e.g. vehicle depreciation).</summary>
    Linear,

    /// <summary>Compounding: value changes by a percentage of the running balance each year (e.g. real-estate appreciation).</summary>
    Compounding
}
