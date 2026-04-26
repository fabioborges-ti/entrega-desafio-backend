using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Application.Products.ListProductCategories;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.RateProduct;
using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Products;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Products;

/// <summary>
/// Cobertura unitária do <see cref="ProductsController"/> para todos os endpoints e ramos de erro.
/// </summary>
public class ProductsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_mediator);
        _controller.WithEmptyContext();
    }

    private static CreateProductRequest ValidCreate() => new()
    {
        Title = "Notebook",
        Price = 1999.99m,
        Description = "Top",
        CategoryId = 1,
        Image = "http://img/1.png"
    };

    private static UpdateProductRequest ValidUpdate() => new()
    {
        Title = "Notebook v2",
        Price = 1499.99m,
        Description = "Atualizado",
        CategoryId = 1,
        Image = "http://img/2.png"
    };

    private static ProductDto FakeDto(int id = 1) => new()
    {
        Id = id,
        Title = "Notebook",
        Price = 1999.99m,
        Description = "Top",
        CategoryId = 1,
        Category = "eletronics",
        Image = "http://img/1.png",
        Rating = new ProductRatingDto { Rate = 0m, Count = 0 }
    };

    [Fact(DisplayName = "GetCategories: retorna 200 com lista de strings")]
    public async Task GetCategories_ReturnsOk()
    {
        var categories = new List<string> { "eletronics", "books" };
        _mediator.Send(Arg.Any<ListProductCategoriesQuery>(), Arg.Any<CancellationToken>())
                 .Returns(categories);

        var response = await _controller.GetCategories(CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(categories);
    }

    [Fact(DisplayName = "GetById: id existente retorna 200 com ProductDto")]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = FakeDto(7);
        _mediator.Send(Arg.Is<GetProductCommand>(c => c.Id == 7), Arg.Any<CancellationToken>())
                 .Returns(dto);

        var response = await _controller.GetById(7, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "GetById: KeyNotFoundException retorna 404")]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetProductCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ProductDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetById(999, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "List: retorna 200 e propaga page/size/order + filtros da query")]
    public async Task List_WhenInvoked_ReturnsOk()
    {
        _controller.WithQueryString("?title=note&_min_price=10");

        var result = new ListProductsResult
        {
            Data = new List<ProductDto> { FakeDto(1) },
            TotalItems = 1,
            CurrentPage = 1,
            TotalPages = 1
        };
        _mediator.Send(Arg.Is<ListProductsCommand>(c =>
            c.Page == 2 && c.Size == 20 && c.Order == "id desc" && c.Filters != null),
            Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.List(2, 20, "id desc", CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact(DisplayName = "List: sem filtros relevantes envia Filters nulo")]
    public async Task List_WhenNoFilters_PassesNullFilters()
    {
        _controller.WithQueryString("?_page=1&_size=10");

        var result = new ListProductsResult { Data = new List<ProductDto>(), TotalItems = 0, CurrentPage = 1, TotalPages = 0 };
        _mediator.Send(Arg.Is<ListProductsCommand>(c => c.Filters == null), Arg.Any<CancellationToken>())
                 .Returns(result);

        var response = await _controller.List(1, 10, null, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "List: chave de query com StringValues vazio é ignorada (cobre branch defensivo)")]
    public async Task List_WhenQueryHasEmptyValues_SkipsKey()
    {
        var emptyValueQuery = new EmptyValueQueryCollection(new Dictionary<string, StringValues>
        {
            ["empty"] = StringValues.Empty,
            ["title"] = "note"
        });
        _controller.WithEmptyContext();
        _controller.ControllerContext.HttpContext.Request.Query = emptyValueQuery;

        var result = new ListProductsResult { Data = new List<ProductDto>(), TotalItems = 0, CurrentPage = 1, TotalPages = 0 };
        _mediator.Send(Arg.Any<ListProductsCommand>(), Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.List(1, 10, null, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "Create: válido retorna 201")]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var dto = FakeDto(10);
        _mediator.Send(Arg.Is<CreateProductCommand>(c =>
            c.Title == "Notebook" && c.Price == 1999.99m && c.CategoryId == 1),
            Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        response.Should().BeOfType<CreatedResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Create: inválido retorna 400")]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Create(new CreateProductRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "RateProduct: usuário autenticado e dados válidos retorna 200")]
    public async Task RateProduct_WhenValidAndAuthenticated_ReturnsOk()
    {
        _controller.WithAuthenticatedUser(42);
        var rating = new ProductRatingDto { Rate = 4m, Count = 1 };
        _mediator.Send(Arg.Is<RateProductCommand>(c => c.ProductId == 5 && c.UserId == 42 && c.Rate == 4m),
                       Arg.Any<CancellationToken>()).Returns(rating);

        var response = await _controller.RateProduct(5, new RateProductRequest { Rate = 4m }, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(rating);
    }

    [Fact(DisplayName = "RateProduct: payload inválido retorna 400 antes de checar usuário")]
    public async Task RateProduct_WhenInvalid_ReturnsBadRequest()
    {
        _controller.WithAuthenticatedUser(42);

        var response = await _controller.RateProduct(5, new RateProductRequest { Rate = 0m }, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "RateProduct: claim NameIdentifier ausente retorna 401")]
    public async Task RateProduct_WhenNoUserClaim_ReturnsUnauthorized()
    {
        _controller.WithUnauthenticatedUser();

        var response = await _controller.RateProduct(5, new RateProductRequest { Rate = 4m }, CancellationToken.None);

        response.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "RateProduct: claim NameIdentifier não numérico retorna 401")]
    public async Task RateProduct_WhenInvalidUserClaim_ReturnsUnauthorized()
    {
        _controller.WithUnauthenticatedUser("abc");

        var response = await _controller.RateProduct(5, new RateProductRequest { Rate = 4m }, CancellationToken.None);

        response.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "RateProduct: KeyNotFoundException retorna 404")]
    public async Task RateProduct_WhenProductNotFound_ReturnsNotFound()
    {
        _controller.WithAuthenticatedUser(42);
        _mediator.Send(Arg.Any<RateProductCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ProductRatingDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.RateProduct(5, new RateProductRequest { Rate = 3m }, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Update: válido retorna 200 com produto atualizado")]
    public async Task Update_WhenValid_ReturnsOk()
    {
        var dto = FakeDto(2);
        _mediator.Send(Arg.Is<UpdateProductCommand>(c => c.Id == 2 && c.Title == "Notebook v2"),
                       Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Update(2, ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Update: payload inválido retorna 400")]
    public async Task Update_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Update(2, new UpdateProductRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: KeyNotFoundException retorna 404")]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateProductCommand>(), Arg.Any<CancellationToken>())
                 .Returns<ProductDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Update(99, ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Delete: válido retorna 200 com mensagem")]
    public async Task Delete_WhenFound_ReturnsOk()
    {
        var deleteResult = new DeleteProductResult { Message = "ok" };
        _mediator.Send(Arg.Is<DeleteProductCommand>(c => c.Id == 3), Arg.Any<CancellationToken>())
                 .Returns(deleteResult);

        var response = await _controller.Delete(3, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(deleteResult);
    }

    [Fact(DisplayName = "Delete: KeyNotFoundException retorna 404")]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<DeleteProductCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteProductResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Delete(99, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }
}

