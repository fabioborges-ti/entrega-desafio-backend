using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsCommand : IRequest<ListCartsResult>
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public string? Order { get; set; }

    public CartListFilterCriteria? Filters { get; set; }
}
