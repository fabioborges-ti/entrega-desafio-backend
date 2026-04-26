using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Customer?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> HasSalesAsync(int customerId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

