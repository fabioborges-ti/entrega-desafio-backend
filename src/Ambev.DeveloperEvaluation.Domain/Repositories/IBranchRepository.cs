using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Branch?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsCnpjAsync(string cnpjDigits, int? excludeBranchId, CancellationToken cancellationToken = default);

    Task<bool> HasSalesAsync(int branchId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Branch> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default);

    Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

