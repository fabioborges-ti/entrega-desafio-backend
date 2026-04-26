using Ambev.DeveloperEvaluation.Domain.Entities;



namespace Ambev.DeveloperEvaluation.Domain.Repositories;



public interface IInventoryRepository

{

    Task<(IReadOnlyList<Inventory> Items, int TotalCount)> ListPagedAsync(

        int page,

        int pageSize,

        string? order,

        CancellationToken cancellationToken = default);



    Task<Inventory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);



    Task<Inventory?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);



    Task<bool> ExistsForProductIdAsync(int productId, CancellationToken cancellationToken = default);



    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);



    Task<Inventory> CreateAsync(Inventory inventory, CancellationToken cancellationToken = default);



    Task<Inventory> UpdateAsync(Inventory inventory, CancellationToken cancellationToken = default);



    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todos os registros de estoque cujo <c>AvailableQuantity</c> está igual ou
    /// abaixo do <c>MinimumStockAlert</c> configurado (e MinimumStockAlert &gt; 0).
    /// Inclui o <see cref="Product"/> para exibição do nome no alerta.
    /// </summary>
    Task<IReadOnlyList<Inventory>> ListBelowAlertAsync(CancellationToken cancellationToken = default);
}


