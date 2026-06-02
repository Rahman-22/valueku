namespace ValueKu.Core.Models;

/// <summary>One eligible-wealth line in the zakat breakdown.</summary>
public record ZakatLine(string Source, decimal Amount);

/// <summary>Result of a zakat-on-wealth calculation.</summary>
public record ZakatResult(
    IReadOnlyList<ZakatLine> Lines,
    decimal EligibleWealth,
    decimal Nisab,
    decimal Rate,
    bool IsAboveNisab,
    decimal ZakatPayable);
