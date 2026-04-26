using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? order,
        ProductListFilterCriteria? filters,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListByCategoryIdPagedAsync(
        int categoryId,
        int page,
        int pageSize,
        string? order,
        ProductListFilterCriteria? filters,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Product?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
