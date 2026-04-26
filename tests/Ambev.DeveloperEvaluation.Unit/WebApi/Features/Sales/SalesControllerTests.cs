using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Cobertura unitária para todos os endpoints e branches de erro do <see cref="SalesController"/>.
/// </summary>
public class SalesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ISaleCommandPublisher _saleCommandPublisher = Substitute.For<ISaleCommandPublisher>();
    private readonly ISalesMessageStatusStore _messageStatusStore = Substitute.For<ISalesMessageStatusStore>();
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _controller = new SalesController(_mediator, _mapper, _saleCommandPublisher, _messageStatusStore);
    }

    private static CreateSaleRequest ValidCreate() => new()
    {
        SaleDate = DateTime.UtcNow,
        SaleNumber = "SN-001",
        CustomerId = Random.Shared.Next(1, int.MaxValue),
        BranchId = Random.Shared.Next(1, int.MaxValue),
        CartId = 1
    };

    private static UpdateSaleRequest ValidUpdate() => new()
    {
        SaleDate = DateTime.UtcNow,
        CustomerId = Random.Shared.Next(1, int.MaxValue),
        BranchId = Random.Shared.Next(1, int.MaxValue),
        CartId = 1
    };

    [Fact(DisplayName = "CreateSale: válido retorna 202 e correlationId")]
    public async Task CreateSale_WhenValid_ReturnsAccepted()
    {
        var request = ValidCreate();
        _saleCommandPublisher.PublishCreateAsync(
                Arg.Any<CreateSaleRequestedMessage>(),
                Arg.Any<CancellationToken>())
            .Returns("corr-create-1");

        var response = await _controller.CreateSale(request, CancellationToken.None);

        var accepted = response.Should().BeOfType<AcceptedResult>().Subject;
        accepted.Value.Should().BeOfType<ApiResponseWithData<object>>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateSaleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CreateSale: request inválido retorna 400")]
    public async Task CreateSale_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.CreateSale(new CreateSaleRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateSaleCommand>(), Arg.Any<CancellationToken>());
        await _saleCommandPublisher.DidNotReceive()
            .PublishCreateAsync(Arg.Any<CreateSaleRequestedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetSale: id existente retorna 200")]
    public async Task GetSale_WhenFound_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var command = new GetSaleCommand(id);
        var commandResult = new GetSaleResult { Id = id, SaleNumber = "SN-001" };
        var responseDto = new GetSaleResponse { Id = id, SaleNumber = "SN-001" };

        _mapper.Map<GetSaleCommand>(id).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(commandResult);
        _mapper.Map<GetSaleResponse>(commandResult).Returns(responseDto);

        var response = await _controller.GetSale(id, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<ApiResponseWithData<GetSaleResponse>>>().Subject;
        outer.Data!.Data!.Id.Should().Be(id);
    }

    [Fact(DisplayName = "GetSale: KeyNotFoundException retorna 404")]
    public async Task GetSale_WhenNotFound_ReturnsNotFound()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        _mapper.Map<GetSaleCommand>(id).Returns(new GetSaleCommand(id));
        _mediator.Send(Arg.Any<GetSaleCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetSaleResult>(_ => throw new KeyNotFoundException("nope"));

        var response = await _controller.GetSale(id, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "ListSales: retorna 200 com payload mapeado")]
    public async Task ListSales_WhenInvoked_ReturnsOk()
    {
        var commandResult = new ListSalesResult { Items = new List<GetSaleResult>(), TotalCount = 0, Page = 1, PageSize = 10 };
        var responseDto = new ListSalesResponse { Items = new List<GetSaleResponse>(), TotalCount = 0, Page = 1, PageSize = 10 };

        _mediator.Send(Arg.Is<ListSalesCommand>(c => c.Page == 3 && c.PageSize == 7), Arg.Any<CancellationToken>())
                 .Returns(commandResult);
        _mapper.Map<ListSalesResponse>(commandResult).Returns(responseDto);

        var response = await _controller.ListSales(3, 7, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<ApiResponseWithData<ListSalesResponse>>>().Subject;
        outer.Data!.Data!.Page.Should().Be(1);
    }

    [Fact(DisplayName = "UpdateSale: válido retorna 202 e correlationId")]
    public async Task UpdateSale_WhenValid_ReturnsAccepted()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var request = ValidUpdate();
        _saleCommandPublisher.PublishUpdateAsync(
                Arg.Any<UpdateSaleRequestedMessage>(),
                Arg.Any<CancellationToken>())
            .Returns("corr-update-1");

        var response = await _controller.UpdateSale(id, request, CancellationToken.None);

        var accepted = response.Should().BeOfType<AcceptedResult>().Subject;
        accepted.Value.Should().BeOfType<ApiResponseWithData<object>>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateSaleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "UpdateSale: request inválido retorna 400")]
    public async Task UpdateSale_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.UpdateSale(Random.Shared.Next(1, int.MaxValue), new UpdateSaleRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateSaleCommand>(), Arg.Any<CancellationToken>());
        await _saleCommandPublisher.DidNotReceive()
            .PublishUpdateAsync(Arg.Any<UpdateSaleRequestedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetMessageStatus: status existente retorna 200")]
    public void GetMessageStatus_WhenFound_ReturnsOk()
    {
        var correlationId = "corr-123";
        SalesMessageStatus? ignored;
        _messageStatusStore.TryGet(correlationId, out ignored)
            .Returns(callInfo =>
            {
                callInfo[1] = new SalesMessageStatus
                {
                    CorrelationId = correlationId,
                    EventName = "sale.created.v1",
                    State = SalesMessageProcessingState.Succeeded
                };
                return true;
            });

        var response = _controller.GetMessageStatus(correlationId);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "GetMessageStatus: status inexistente retorna 404")]
    public void GetMessageStatus_WhenMissing_ReturnsNotFound()
    {
        var response = _controller.GetMessageStatus("corr-missing");
        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "DeleteSale: válido retorna 202 e correlationId")]
    public async Task DeleteSale_WhenFound_ReturnsAccepted()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        _saleCommandPublisher.PublishDeleteAsync(
                Arg.Any<DeleteSaleRequestedMessage>(),
                Arg.Any<CancellationToken>())
            .Returns("corr-delete-1");

        var response = await _controller.DeleteSale(id, CancellationToken.None);

        var accepted = response.Should().BeOfType<AcceptedResult>().Subject;
        accepted.Value.Should().BeOfType<ApiResponseWithData<object>>();
        await _mediator.DidNotReceive().Send(Arg.Any<DeleteSaleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CancelSale: válido retorna 202 e correlationId")]
    public async Task CancelSale_WhenFound_ReturnsAccepted()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        _saleCommandPublisher.PublishCancelAsync(
                Arg.Any<CancelSaleRequestedMessage>(),
                Arg.Any<CancellationToken>())
            .Returns("corr-cancel-1");

        var response = await _controller.CancelSale(id, CancellationToken.None);

        var accepted = response.Should().BeOfType<AcceptedResult>().Subject;
        accepted.Value.Should().BeOfType<ApiResponseWithData<object>>();
        await _mediator.DidNotReceive().Send(Arg.Any<CancelSaleCommand>(), Arg.Any<CancellationToken>());
    }
}




