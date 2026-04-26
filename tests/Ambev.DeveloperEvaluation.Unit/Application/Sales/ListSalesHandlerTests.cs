using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class ListSalesHandlerTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Handle_WhenPaginationInvalid_ThrowsValidationException(int page, int pageSize)
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var handler = new ListSalesHandler(saleRepo, mapper);

        var act = async () => await handler.Handle(new ListSalesCommand { Page = page, PageSize = pageSize }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await saleRepo.DidNotReceive().ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsPagedMappedItems()
    {
        var saleRepo = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var s1 = new Sale { Id = Random.Shared.Next(1, int.MaxValue), SaleNumber = "A", SaleDate = DateTime.UtcNow };
        var s2 = new Sale { Id = Random.Shared.Next(1, int.MaxValue), SaleNumber = "B", SaleDate = DateTime.UtcNow };
        IReadOnlyList<Sale> page = new List<Sale> { s1, s2 };
        saleRepo.ListAsync(2, 5, Arg.Any<CancellationToken>()).Returns((page, 12));
        var r1 = new GetSaleResult { Id = s1.Id };
        var r2 = new GetSaleResult { Id = s2.Id };
        mapper.Map<GetSaleResult>(s1).Returns(r1);
        mapper.Map<GetSaleResult>(s2).Returns(r2);
        var handler = new ListSalesHandler(saleRepo, mapper);

        var result = await handler.Handle(new ListSalesCommand { Page = 2, PageSize = 5 }, CancellationToken.None);

        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Items[0].Should().Be(r1);
        result.Items[1].Should().Be(r2);
    }
}



