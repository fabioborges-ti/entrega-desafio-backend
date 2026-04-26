using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleCommand : IRequest<GetSaleResult>
{
    public int Id { get; set; }

    public GetSaleCommand(int id) => Id = id;
}

