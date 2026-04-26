using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

public class DeleteSaleCommand : IRequest<DeleteSaleResult>
{
    public int Id { get; set; }

    public DeleteSaleCommand(int id) => Id = id;
}

