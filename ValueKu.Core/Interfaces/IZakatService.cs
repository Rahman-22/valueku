using ValueKu.Core.Models;

namespace ValueKu.Core.Interfaces;

/// <summary>Calculates zakat on eligible wealth against the nisab threshold.</summary>
public interface IZakatService
{
    Task<ZakatResult> CalculateAsync(int userId, CancellationToken ct = default);
}
