using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.ListProductCategories;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.RateProduct;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista as categorias disponíveis no catálogo.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new ListProductCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    /// <summary>Obtém um produto por ID.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetProductCommand { Id = id }, cancellationToken);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Product not found", ex.Message));
        }
    }

    /// <summary>Lista produtos com filtros, paginação e ordenação.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet]
    [ProducesResponseType(typeof(ListProductsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var filters = ProductListFilterQueryParser.Parse(CollectQueryParameters());
        var result = await _mediator.Send(
            new ListProductsCommand { Page = page, Size = size, Order = order, Filters = filters },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Primeiro valor de cada chave da query (case-insensitive chave), para filtros general-api.</summary>
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

    /// <summary>Cria um novo produto no catálogo.</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var command = new CreateProductCommand
        {
            Title = request.Title,
            Price = request.Price,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Image = request.Image
        };

        var created = await _mediator.Send(command, cancellationToken);
        return Created(string.Empty, created);
    }

    /// <summary>Registra avaliação de um usuário para o produto.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpPost("{id:int}/ratings")]
    [ProducesResponseType(typeof(ProductRatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RateProduct(
        [FromRoute] int id,
        [FromBody] RateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new RateProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var rating = await _mediator.Send(
                new RateProductCommand { ProductId = id, UserId = userId, Rate = request.Rate },
                cancellationToken);
            return Ok(rating);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Product not found", ex.Message));
        }
    }

    /// <summary>Atualiza os dados de um produto.</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateProductRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var command = new UpdateProductCommand
        {
            Id = id,
            Title = request.Title,
            Price = request.Price,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Image = request.Image
        };

        try
        {
            var updated = await _mediator.Send(command, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Product not found", ex.Message));
        }
    }

    /// <summary>Exclui um produto por ID.</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(DeleteProductResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DeleteProductCommand { Id = id }, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Product not found", ex.Message));
        }
    }
}
