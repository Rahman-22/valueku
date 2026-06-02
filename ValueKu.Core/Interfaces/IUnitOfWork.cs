namespace ValueKu.Core.Interfaces;

/// <summary>Coordinates repositories over a single DbContext and commits them atomically.</summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
