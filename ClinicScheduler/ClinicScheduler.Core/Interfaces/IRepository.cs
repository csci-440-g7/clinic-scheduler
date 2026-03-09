using System.Linq.Expressions;

namespace ClinicScheduler.Core.Interfaces;

/// <summary>
/// Defines a generic repository interface for a specific entity type 'T'.
/// This interface follows the Repository Pattern, creating an abstraction layer
/// between the application's business logic and the data storage mechanism.
/// It centralizes data access logic and improves testability.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);

    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
}