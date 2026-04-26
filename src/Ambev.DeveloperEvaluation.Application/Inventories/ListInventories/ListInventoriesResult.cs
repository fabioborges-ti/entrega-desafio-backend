using Ambev.DeveloperEvaluation.Application.Inventories;

namespace Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;

public class ListInventoriesResult
{
    public IReadOnlyList<InventoryDto> Data { get; set; } = Array.Empty<InventoryDto>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}
