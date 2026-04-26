using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.ListProductsByCategory;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Features.Categories;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Categories;

/// <summary>
/// Cobertura unitária do <see cref="CategoriesController"/>.
/// </summary>
public class CategoriesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(_mediator);
        _controller.WithEmptyContext();
    }

    [Fact(DisplayName = "ListProducts: encaminha id, page, size, order e filtros para o command")]
    public async Task ListProducts_WhenInvoked_ReturnsOk()
    {
        _controller.WithQueryString("?title=note&_min_price=5");

        var result = new ListProductsResult
        {
            Data = new List<ProductDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            TotalPages = 0
        };
        _mediator.Send(Arg.Is<ListProductsByCategoryCommand>(c =>
            c.CategoryId == 9 && c.Page == 2 && c.Size == 5 && c.Order == "title asc" && c.Filters != null),
            Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.ListProducts(9, 2, 5, "title asc", CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact(DisplayName = "ListProducts: sem filtros relevantes envia Filters nulo e usa defaults")]
    public async Task ListProducts_WhenNoFilters_DefaultsAndNullFilters()
    {
        _controller.WithQueryString("?_page=1&_size=10");

        var result = new ListProductsResult
        {
            Data = new List<ProductDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            TotalPages = 0
        };
        _mediator.Send(Arg.Is<ListProductsByCategoryCommand>(c =>
            c.CategoryId == 1 && c.Page == 1 && c.Size == 10 && c.Order == null && c.Filters == null),
            Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.ListProducts(1, cancellationToken: CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact(DisplayName = "ListProducts: chave com StringValues vazio é ignorada (cobre branch defensivo)")]
    public async Task ListProducts_WhenQueryHasEmptyValues_SkipsKey()
    {
        var emptyValueQuery = new EmptyValueQueryCollection(new Dictionary<string, StringValues>
        {
            ["empty"] = StringValues.Empty,
            ["title"] = "note"
        });
        _controller.WithEmptyContext();
        _controller.ControllerContext.HttpContext.Request.Query = emptyValueQuery;

        var result = new ListProductsResult { Data = new List<ProductDto>(), TotalItems = 0, CurrentPage = 1, TotalPages = 0 };
        _mediator.Send(Arg.Any<ListProductsByCategoryCommand>(), Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.ListProducts(1, cancellationToken: CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }
}

