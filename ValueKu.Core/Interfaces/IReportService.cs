namespace ValueKu.Core.Interfaces;

/// <summary>Generates downloadable financial documents (PDF) for a user.</summary>
public interface IReportService
{
    /// <summary>Builds a monthly financial health statement and returns the PDF bytes.</summary>
    Task<byte[]> GenerateMonthlyStatementAsync(int userId, int year, int month, CancellationToken ct = default);
}
