using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.ListProductsByCategory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Categories;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista produtos de uma categoria específica.</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpGet("{id:int}/products")]
    [ProducesResponseType(typeof(ListProductsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProducts(
        [FromRoute] int id,
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var filters = ProductListFilterQueryParser.Parse(CollectQueryParameters());
        var result = await _mediator.Send(
            new ListProductsByCategoryCommand
            {
                CategoryId = id,
                Page = page,
                Size = size,
                Order = order,
                Filters = filters
            },
            cancellationToken);
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
