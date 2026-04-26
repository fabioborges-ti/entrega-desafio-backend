using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleCommand : IRequest<GetSaleResult>
{
    public int Id { get; set; }

    public DateTime SaleDate { get; set; }

    public int CustomerId { get; set; }

    public int BranchId { get; set; }

    /// <summary>
    /// Cart que origina os itens da venda. Se diferente do cart atual, dispara
    /// estorno + remoção do antigo e reconstrução dos itens a partir do novo.
    /// </summary>
    public int CartId { get; set; }

    public bool SuppressEventPublication { get; set; }
}


