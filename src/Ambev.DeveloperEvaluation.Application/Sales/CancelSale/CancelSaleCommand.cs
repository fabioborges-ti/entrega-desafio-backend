using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleCommand : IRequest<CancelSaleResult>
{
    public int Id { get; set; }

    public bool SuppressEventPublication { get; set; }

    public CancelSaleCommand(int id) => Id = id;

    public CancelSaleCommand()
    {
    }
}

