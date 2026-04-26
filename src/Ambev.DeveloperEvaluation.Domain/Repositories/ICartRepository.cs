using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ICartRepository
{
    Task<(IReadOnlyList<Cart> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? order,
        CartListFilterCriteria? filters = null,
        CancellationToken cancellationToken = default);

    Task<Cart?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Cart?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Cart> CreateAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste o carrinho e debita a quantidade disponível no inventário por produto, na mesma transação.
    /// </summary>
    Task<Cart> CreateWithInventoryDeductionAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o carrinho ajustando (estorno/débito) o inventário na mesma transação.
    /// </summary>
    Task<Cart> UpdateWithInventoryAdjustmentAsync(
        Cart trackedCart,
        IReadOnlyList<CartLineItem> newLineItems,
        CancellationToken cancellationToken = default);

    Task<Cart> UpdateAsync(Cart cart, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

