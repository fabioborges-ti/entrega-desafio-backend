using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetOrderedNamesAsync(CancellationToken cancellationToken = default);
}
