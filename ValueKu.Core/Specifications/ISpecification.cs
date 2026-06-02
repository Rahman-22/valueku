using System.Linq.Expressions;

namespace ValueKu.Core.Specifications;

/// <summary>
/// Encapsulates query criteria, eager-loading, ordering and paging so that data-access
/// concerns stay out of the domain. Evaluated against EF Core in the Infrastructure layer.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}
