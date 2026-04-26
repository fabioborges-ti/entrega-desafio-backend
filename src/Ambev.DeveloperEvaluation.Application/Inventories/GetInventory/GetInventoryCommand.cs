using Ambev.DeveloperEvaluation.Application.Inventories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;

public class GetInventoryCommand : IRequest<InventoryDto>
{
    public int Id { get; set; }
}
