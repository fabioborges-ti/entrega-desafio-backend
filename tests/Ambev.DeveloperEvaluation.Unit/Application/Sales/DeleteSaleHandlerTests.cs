using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class DeleteSaleHandlerTests
{
    [Fact]
    public async Task Handle_WhenIdEmpty_ThrowsValidationException()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var handler = new DeleteSaleHandler(saleRepo, NullLogger<DeleteSaleHandler>.Instance);

        var act = async () => await handler.Handle(new DeleteSaleCommand(0), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await saleRepo.DidNotReceive().DeleteWithCartAndStockReturnAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaleNotFound_ThrowsKeyNotFoundException()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        saleRepo.DeleteWithCartAndStockReturnAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteSaleHandler(saleRepo, NullLogger<DeleteSaleHandler>.Instance);

        var act = async () => await handler.Handle(new DeleteSaleCommand(Random.Shared.Next(1, int.MaxValue)), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSaleDeleted_ReturnsSuccess()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var id = Random.Shared.Next(1, int.MaxValue);
        saleRepo.DeleteWithCartAndStockReturnAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteSaleHandler(saleRepo, NullLogger<DeleteSaleHandler>.Instance);

        var result = await handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        result.Success.Should().BeTrue();
        await saleRepo.Received(1).DeleteWithCartAndStockReturnAsync(id, Arg.Any<CancellationToken>());
    }
}



