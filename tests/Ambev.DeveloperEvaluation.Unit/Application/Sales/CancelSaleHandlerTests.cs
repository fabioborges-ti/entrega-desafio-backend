using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CancelSaleHandlerTests
{
    private static CancelSaleHandler CreateSut(
        out ISaleRepository saleRepository,
        out ISaleEventPublisher events)
    {
        saleRepository = Substitute.For<ISaleRepository>();
        events = Substitute.For<ISaleEventPublisher>();
        return new CancelSaleHandler(saleRepository, events, NullLogger<CancelSaleHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenIdEmpty_ThrowsValidationException()
    {
        var handler = CreateSut(out _, out _);

        var act = async () => await handler.Handle(new CancelSaleCommand(0), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenSaleNotFound_ThrowsKeyNotFoundExceptionAndDoesNotPublish()
    {
        var handler = CreateSut(out var saleRepo, out var events);
        var id = Random.Shared.Next(1, int.MaxValue);
        saleRepo.CancelWithCartAndStockReturnAsync(id, Arg.Any<CancellationToken>())
            .Returns((CancelSaleOutcome.NotFound, (string?)null));

        var act = async () => await handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
        ex.Which.Message.Should().Contain(id.ToString());
        events.DidNotReceive().PublishSaleCancelled(Arg.Any<SaleCancelledPayload>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyCancelled_ReturnsSuccessWithoutPublishing()
    {
        var handler = CreateSut(out var saleRepo, out var events);
        var id = Random.Shared.Next(1, int.MaxValue);
        saleRepo.CancelWithCartAndStockReturnAsync(id, Arg.Any<CancellationToken>())
            .Returns((CancelSaleOutcome.AlreadyCancelled, (string?)"S-1"));

        var result = await handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        result.Success.Should().BeTrue();
        events.DidNotReceive().PublishSaleCancelled(Arg.Any<SaleCancelledPayload>());
    }

    [Fact]
    public async Task Handle_WhenCancelled_ReturnsSuccessAndPublishesEvent()
    {
        var handler = CreateSut(out var saleRepo, out var events);
        var id = Random.Shared.Next(1, int.MaxValue);
        const string saleNumber = "S-42";
        saleRepo.CancelWithCartAndStockReturnAsync(id, Arg.Any<CancellationToken>())
            .Returns((CancelSaleOutcome.Cancelled, (string?)saleNumber));

        var result = await handler.Handle(new CancelSaleCommand(id), CancellationToken.None);

        result.Success.Should().BeTrue();
        events.Received(1).PublishSaleCancelled(Arg.Is<SaleCancelledPayload>(p =>
            p.SaleId == id && p.SaleNumber == saleNumber));
    }
}



