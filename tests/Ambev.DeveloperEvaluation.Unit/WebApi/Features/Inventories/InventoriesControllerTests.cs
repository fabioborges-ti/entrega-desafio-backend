using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Application.Inventories.CreateInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;
using Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Inventories;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Inventories;

/// <summary>
/// Cobertura unitária do <see cref="InventoriesController"/>.
/// </summary>
public class InventoriesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly InventoriesController _controller;

    public InventoriesControllerTests()
    {
        _controller = new InventoriesController(_mediator);
    }

    private static InventoryDto FakeDto(int id = 1) => new()
    {
        Id = id,
        ProductId = 100,
        ProductTitle = "Notebook",
        CategoryId = 1,
        Category = "eletronics",
        AvailableQuantity = 50
    };

    [Fact(DisplayName = "List: retorna 200 com payload e propaga page/size/order")]
    public async Task List_WhenInvoked_ReturnsOk()
    {
        var result = new ListInventoriesResult
        {
            Data = new List<InventoryDto> { FakeDto() },
            TotalItems = 1,
            CurrentPage = 1,
            TotalPages = 1
        };
        _mediator.Send(Arg.Is<ListInventoriesCommand>(c => c.Page == 2 && c.Size == 7 && c.Order == "id desc"),
                       Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.List(2, 7, "id desc", CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact(DisplayName = "GetById: id existente retorna 200 com InventoryDto")]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var dto = FakeDto(5);
        _mediator.Send(Arg.Is<GetInventoryCommand>(c => c.Id == 5), Arg.Any<CancellationToken>())
                 .Returns(dto);

        var response = await _controller.GetById(5, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "GetById: KeyNotFoundException retorna 404")]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetInventoryCommand>(), Arg.Any<CancellationToken>())
                 .Returns<InventoryDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetById(99, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: payload válido retorna 201 (CreatedAtAction)")]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var dto = FakeDto(2);
        _mediator.Send(Arg.Is<CreateInventoryCommand>(c => c.ProductId == 100 && c.AvailableQuantity == 50),
                       Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Create(
            new CreateInventoryRequest { ProductId = 100, AvailableQuantity = 50 },
            CancellationToken.None);

        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(InventoriesController.GetById));
        created.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Create: payload inválido retorna 400")]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Create(
            new CreateInventoryRequest { ProductId = 0, AvailableQuantity = -1 },
            CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Update: payload válido retorna 200")]
    public async Task Update_WhenValid_ReturnsOk()
    {
        var dto = FakeDto(3);
        _mediator.Send(Arg.Is<UpdateInventoryCommand>(c => c.Id == 3 && c.AvailableQuantity == 25),
                       Arg.Any<CancellationToken>()).Returns(dto);

        var response = await _controller.Update(3,
            new UpdateInventoryRequest { AvailableQuantity = 25 },
            CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact(DisplayName = "Update: payload inválido retorna 400")]
    public async Task Update_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Update(3,
            new UpdateInventoryRequest { AvailableQuantity = -1 },
            CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: KeyNotFoundException retorna 404")]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateInventoryCommand>(), Arg.Any<CancellationToken>())
                 .Returns<InventoryDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Update(99,
            new UpdateInventoryRequest { AvailableQuantity = 5 },
            CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Delete: válido retorna 200 com mensagem")]
    public async Task Delete_WhenFound_ReturnsOk()
    {
        var deleteResult = new DeleteInventoryResult { Message = "ok" };
        _mediator.Send(Arg.Is<DeleteInventoryCommand>(c => c.Id == 7), Arg.Any<CancellationToken>())
                 .Returns(deleteResult);

        var response = await _controller.Delete(7, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(deleteResult);
    }

    [Fact(DisplayName = "Delete: KeyNotFoundException retorna 404")]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<DeleteInventoryCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteInventoryResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Delete(99, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }
}

