using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleCommand : IRequest<CreateSaleResult>
{
    public DateTime SaleDate { get; set; }

    public string? SaleNumber { get; set; }

    public int CustomerId { get; set; }

    public int BranchId { get; set; }

    public int CartId { get; set; }

    public bool SuppressEventPublication { get; set; }
}

