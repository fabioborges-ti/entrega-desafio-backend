using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class GetSaleHandlerTests
{
    [Fact]
    public async Task Handle_WhenIdEmpty_ThrowsValidationException()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var handler = new GetSaleHandler(saleRepo, mapper);

        var act = async () => await handler.Handle(new GetSaleCommand(0), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await saleRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaleNotFound_ThrowsKeyNotFoundException()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        saleRepo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);
        var handler = new GetSaleHandler(saleRepo, mapper);

        var act = async () => await handler.Handle(new GetSaleCommand(Random.Shared.Next(1, int.MaxValue)), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSaleExists_ReturnsMappedResult()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var id = Random.Shared.Next(1, int.MaxValue);
        var sale = new Sale { Id = id, SaleNumber = "N", SaleDate = DateTime.UtcNow };
        saleRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(sale);
        var dto = new GetSaleResult { Id = id, SaleNumber = "N" };
        mapper.Map<GetSaleResult>(sale).Returns(dto);
        var handler = new GetSaleHandler(saleRepo, mapper);

        var result = await handler.Handle(new GetSaleCommand(id), CancellationToken.None);

        result.Should().BeSameAs(dto);
    }
}



