using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Carts;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;
using Ambev.DeveloperEvaluation.Application.Carts.GetCart;
using Ambev.DeveloperEvaluation.Application.Carts.ListCarts;
using Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Carts;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Carts;

/// <summary>
/// Cobertura unitária do <see cref="CartsController"/>: caminhos felizes, validações e datas inválidas.
/// </summary>
public class CartsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CartsController _controller;

    public CartsControllerTests()
    {
        _controller = new CartsController(_mediator);
        _controller.WithEmptyContext();
    }

    private static CreateCartRequest ValidCreate(string? date = null) => new()
    {
        UserId = 1,
        Date = date ?? DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        Products = new List<CartProductRequest>
        {
            new() { ProductId = 10, Quantity = 2 }
        }
    };

    private static UpdateCartRequest ValidUpdate(string? date = null) => new()
    {
        UserId = 1,
        Date = date ?? DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        Products = new List<CartProductRequest>
        {
            new() { ProductId = 10, Quantity = 5 }
        }
    };

    private static CartDto FakeDto(int id = 1) => new()
    {
        Id = id,
        UserId = 1,
        Date = "2026-04-25",
        Products = new List<CartProductDto> { new() { ProductId = 10, Quantity = 2 } }
    };

    [Fact(DisplayName = "List: encaminha page/size/order e parsing de filtros")]
    public async Task List_WhenInvoked_ReturnsOk()
    {
        _controller.WithQueryString("?userid=42");

        var result = new ListCartsResult
        {
            Data = new List<CartDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            TotalPages = 0
        };
        _mediator.Send(Arg.Is<ListCartsCommand>(c =>
            c.Page == 2 && c.Size == 5 && c.Order == "id desc" && c.Filters != null),
            Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.List(2, 5, "id desc", CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact(DisplayName = "List: sem filtros relevantes envia Filters nulo")]
    public async Task List_WhenNoFilters_PassesNullFilters()
    {
        _controller.WithQueryString("?_page=1");

        var result = new ListCartsResult
        {
            Data = new List<CartDto>(),
            TotalItems = 0,
            CurrentPage = 1,
            TotalPages = 0
        };
        _mediator.Send(Arg.Is<ListCartsCommand>(c => c.Filters == null), Arg.Any<CancellationToken>())
                 .Returns(result);

        var response = await _controller.List(1, 10, null, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "List: chave com StringValues vazio é ignorada (cobre branch defensivo)")]
    public async Task List_WhenQueryHasEmptyValues_SkipsKey()
    {
        var emptyValueQuery = new EmptyValueQueryCollection(new Dictionary<string, StringValues>
        {
            ["empty"] = StringValues.Empty,
            ["userid"] = "42"
        });
        _controller.WithEmptyContext();
        _controller.ControllerContext.HttpContext.Request.Query = emptyValueQuery;

        var result = new ListCartsResult { Data = new List<CartDto>(), TotalItems = 0, CurrentPage = 1, TotalPages = 0 };
        _mediator.Send(Arg.Any<ListCartsCommand>(), Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.List(1, 10, null, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "GetById: id existente retorna 200 com CartDto")]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = FakeDto(7);
        _mediator.Send(Arg.Is<GetCartCommand>(c => c.Id == 7), Arg.Any<CancellationToken>())
                 .Returns(dto);

        var response = await _controller.GetById(7, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "GetById: KeyNotFoundException retorna 404")]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetCartCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CartDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetById(99, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: payload válido retorna 201")]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var dto = FakeDto(2);
        _mediator.Send(Arg.Is<CreateCartCommand>(c => c.UserId == 1 && c.Products.Count == 1),
                       Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        response.Should().BeOfType<CreatedResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Create: payload inválido retorna 400")]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Create(new CreateCartRequest
        {
            UserId = 0,
            Date = string.Empty,
            Products = new List<CartProductRequest>()
        }, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: payload válido pelo validator mas Date inválida pro TryParse �?' 400")]
    public async Task Create_WhenDateUnparseable_ReturnsBadRequest()
    {
        // Validator aceita Date com TryParse padrão; este formato passa no validator
        // (uma string que TryParse aceita) mas o controller usa TryParse com RoundtripKind, podendo falhar.
        // Para forçar este branch, usamos um conteúdo que o validator regex aceita mas TryParse RoundtripKind rejeita.
        var req = ValidCreate("not-a-real-iso");

        var response = await _controller.Create(req, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: payload válido retorna 200")]
    public async Task Update_WhenValid_ReturnsOk()
    {
        var dto = FakeDto(3);
        _mediator.Send(Arg.Is<UpdateCartCommand>(c => c.Id == 3 && c.UserId == 1),
                       Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Update(3, ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Update: payload inválido retorna 400")]
    public async Task Update_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Update(3, new UpdateCartRequest
        {
            UserId = 0,
            Date = string.Empty,
            Products = new List<CartProductRequest>()
        }, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: Date inválida no controller (RoundtripKind) retorna 400")]
    public async Task Update_WhenDateUnparseable_ReturnsBadRequest()
    {
        var response = await _controller.Update(3, ValidUpdate("not-a-real-iso"), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: KeyNotFoundException retorna 404")]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateCartCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CartDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Update(99, ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Delete: retorna 200 com resultado do handler")]
    public async Task Delete_WhenInvoked_ReturnsOk()
    {
        var deleteResult = new DeleteCartResult { Message = "ok" };
        _mediator.Send(Arg.Is<DeleteCartCommand>(c => c.Id == 4), Arg.Any<CancellationToken>())
                 .Returns(deleteResult);

        var response = await _controller.Delete(4, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(deleteResult);
    }
}

