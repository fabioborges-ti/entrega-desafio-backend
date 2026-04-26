using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Application.Inventories.CreateInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;
using Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;
using Ambev.DeveloperEvaluation.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Inventories;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager", Policy = "ActiveUser")]
public class InventoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista registros de estoque com paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListInventoriesResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListInventoriesCommand { Page = page, Size = size, Order = order },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtém um registro de estoque por ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetInventoryCommand { Id = id }, cancellationToken);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Inventory not found", ex.Message));
        }
    }

    /// <summary>Cria um novo registro de estoque.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateInventoryRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var command = new CreateInventoryCommand
        {
            ProductId = request.ProductId,
            AvailableQuantity = request.AvailableQuantity,
            MinimumStockAlert = request.MinimumStockAlert
        };

        var created = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Atualiza quantidade e alerta mínimo de estoque.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateInventoryRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var command = new UpdateInventoryCommand
        {
            Id = id,
            AvailableQuantity = request.AvailableQuantity,
            MinimumStockAlert = request.MinimumStockAlert
        };

        try
        {
            var updated = await _mediator.Send(command, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Inventory not found", ex.Message));
        }
    }

    /// <summary>Exclui um registro de estoque por ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteInventoryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DeleteInventoryCommand { Id = id }, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Inventory not found", ex.Message));
        }
    }
}
