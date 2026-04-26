namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

public class CreateSaleRequest
{
    public DateTime SaleDate { get; set; }

    public string? SaleNumber { get; set; }

    /// <summary>Identificador do cliente cadastrado (tabela <c>Customers</c>).</summary>
    public int CustomerId { get; set; }

    /// <summary>Identificador da filial cadastrada (tabela <c>Branches</c>).</summary>
    public int BranchId { get; set; }

    /// <summary>Carrinho com linhas; os itens da venda são derivados das linhas do carrinho.</summary>
    public int CartId { get; set; }
}


