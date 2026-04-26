namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// Payload de atualização de venda. Espelha o POST exceto por <c>saleNumber</c>,
/// que é definido na criação e tratado como somente leitura após esse momento.
/// </summary>
public class UpdateSaleRequest
{
    public DateTime SaleDate { get; set; }

    /// <summary>Identificador do cliente cadastrado (tabela <c>Customers</c>).</summary>
    public int CustomerId { get; set; }

    /// <summary>Identificador da filial cadastrada (tabela <c>Branches</c>).</summary>
    public int BranchId { get; set; }

    /// <summary>
    /// Carrinho que origina os itens da venda. Quando alterado em relação ao cart atual,
    /// o cart antigo é estornado no estoque e removido, e a venda é recomposta a partir
    /// do novo carrinho (mesma lógica do POST).
    /// </summary>
    public int CartId { get; set; }
}


