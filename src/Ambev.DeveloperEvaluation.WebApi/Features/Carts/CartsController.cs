using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Carts;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;
using Ambev.DeveloperEvaluation.Application.Carts.GetCart;
using Ambev.DeveloperEvaluation.Application.Carts.ListCarts;
using Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts;

/// <summary>Endpoints conforme <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/carts-api.md">carts-api</see>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer", Policy = "ActiveUser")]
public class CartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista carrinhos com filtros, paginação e ordenação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListCartsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var filters = CartListFilterQueryParser.Parse(CollectQueryParameters());
        var result = await _mediator.Send(
            new ListCartsCommand { Page = page, Size = size, Order = order, Filters = filters },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtém um carrinho por ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetCartCommand { Id = id }, cancellationToken);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Cart not found", ex.Message));
        }
    }

    /// <summary>Cria um carrinho com seus itens.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCartRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateCartRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        if (!DateTime.TryParse(request.Date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            return BadRequest(ApiErrorResponse.ValidationDetail("Date inválida."));

        var command = new CreateCartCommand
        {
            UserId = request.UserId,
            Date = date,
            Products = request.Products
                .Select(p => new CartLineInput { ProductId = p.ProductId, Quantity = p.Quantity })
                .ToList()
        };

        var created = await _mediator.Send(command, cancellationToken);
        return Created(string.Empty, created);
    }

    /// <summary>Atualiza os dados e itens de um carrinho.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCartRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateCartRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        if (!DateTime.TryParse(request.Date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            return BadRequest(ApiErrorResponse.ValidationDetail("Date inválida."));

        try
        {
            var command = new UpdateCartCommand
            {
                Id = id,
                UserId = request.UserId,
                Date = date,
                Products = request.Products
                    .Select(p => new CartLineInput { ProductId = p.ProductId, Quantity = p.Quantity })
                    .ToList()
            };

            var updated = await _mediator.Send(command, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Cart not found", ex.Message));
        }
    }

    /// <summary>Exclui um carrinho por ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteCartResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCartCommand { Id = id }, cancellationToken);
        return Ok(result);
    }

    private IReadOnlyDictionary<string, string?> CollectQueryParameters()
    {
        var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Request.Query)
        {
            if (kv.Value.Count == 0)
                continue;
            d[kv.Key] = kv.Value[0];
        }

        return d;
    }
}

