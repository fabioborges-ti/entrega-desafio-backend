using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;

public class DeleteInventoryCommand : IRequest<DeleteInventoryResult>
{
    public int Id { get; set; }
}

public class DeleteInventoryResult
{
    public string Message { get; set; } = string.Empty;
}
