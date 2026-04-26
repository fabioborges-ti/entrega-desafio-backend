using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Resultado da operação de cancelamento de venda com estorno de estoque e remoção do cart.
/// </summary>
public enum CancelSaleOutcome
{
    /// <summary>Venda não existe no banco.</summary>
    NotFound,
    /// <summary>Venda existe e já estava cancelada �?" nenhum efeito colateral foi aplicado.</summary>
    AlreadyCancelled,
    /// <summary>Venda foi cancelada agora; estoque do cart foi estornado e o cart removido (quando havia cart vinculado).</summary>
    Cancelled
}

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsSaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsSaleForCartAsync(int cartId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Variante usada em fluxos de atualização: ignora a venda <paramref name="exceptSaleId"/>
    /// para permitir reapontar uma venda existente para outro cart sem falso positivo.
    /// </summary>
    Task<bool> ExistsSaleForCartAsync(int cartId, int exceptSaleId, CancellationToken cancellationToken = default);

    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a venda e, quando houver carrinho de origem, estorna no estoque as quantidades agregadas
    /// por <c>ProductId</c> dos itens do carrinho e remove o próprio carrinho �?" tudo numa única transação.
    /// </summary>
    /// <returns><c>true</c> se a venda existia e foi removida; <c>false</c> caso contrário.</returns>
    Task<bool> DeleteWithCartAndStockReturnAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela a venda em uma única transação:
    /// 1) marca <c>IsCancelled = true</c> e zera <c>TotalAmount</c>;
    /// 2) quando houver carrinho de origem, desvincula o cart da venda, estorna no estoque
    ///    as quantidades agregadas por <c>ProductId</c> e remove o próprio carrinho.
    /// Em caso de qualquer falha, nada é persistido (rollback).
    /// </summary>
    /// <returns>
    /// <c>Outcome</c> indica o estado final; <c>SaleNumber</c> vem preenchido quando a venda existe
    /// (para suportar publicação de eventos pelo orquestrador) e é <c>null</c> em <see cref="CancelSaleOutcome.NotFound"/>.
    /// </returns>
    Task<(CancelSaleOutcome Outcome, string? SaleNumber)> CancelWithCartAndStockReturnAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reaponta a venda para um novo carrinho de origem em uma única transação:
    /// muda <c>CartId</c>, substitui os itens, persiste a venda (liberando o índice único de cart),
    /// estorna no estoque as quantidades do cart antigo (agregadas por <c>ProductId</c>) e remove o cart antigo.
    /// </summary>
    /// <param name="trackedSale">Venda já carregada com tracking; deve estar com <c>CartId</c> apontando para o cart antigo.</param>
    /// <param name="trackedOldCart">Cart antigo já carregado com tracking (com <c>LineItems</c>) que será estornado e removido.</param>
    /// <param name="newCartId">Novo identificador de cart que passará a originar a venda.</param>
    /// <param name="newItems">Itens recalculados a partir do novo cart (preço do catálogo já aplicado).</param>
    Task<Sale> ReplaceCartAndPersistAsync(
        Sale trackedSale,
        Cart trackedOldCart,
        int newCartId,
        IReadOnlyList<SaleItem> newItems,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}


