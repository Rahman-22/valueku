using ValueKu.Core.Models;

namespace ValueKu.Core.Interfaces;

/// <summary>Projects future net worth from current holdings, asset rates and historical cash flow.</summary>
public interface INetWorthProjectionService
{
    Task<ProjectionResult> ProjectAsync(int userId, CancellationToken ct = default);
}
