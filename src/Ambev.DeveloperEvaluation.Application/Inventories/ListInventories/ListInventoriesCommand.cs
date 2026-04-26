using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;

public class ListInventoriesCommand : IRequest<ListInventoriesResult>
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;

    public string? Order { get; set; }
}
