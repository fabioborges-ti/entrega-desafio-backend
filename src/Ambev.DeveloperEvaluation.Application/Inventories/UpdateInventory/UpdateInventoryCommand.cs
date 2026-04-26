using Ambev.DeveloperEvaluation.Application.Inventories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;

public class UpdateInventoryCommand : IRequest<InventoryDto>
{
    public int Id { get; set; }

    public int AvailableQuantity { get; set; }

    /// <summary>Quantidade mínima para disparo de alerta de estoque baixo. 0 = desativado.</summary>
    public int MinimumStockAlert { get; set; }
}
