using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class UpdateSaleHandlerTests
{
    private const int ExistingCartId = 5;

    private static UpdateSaleHandler CreateSut(
        out ISaleRepository saleRepository,
        out IProductRepository productRepository,
        out ICustomerRepository customerRepository,
        out IBranchRepository branchRepository,
        out ICartRepository cartRepository,
        out IMapper mapper,
        out ISaleEventPublisher eventPublisher)
    {
        saleRepository = Substitute.For<ISaleRepository>();
        productRepository = Substitute.For<IProductRepository>();
        customerRepository = Substitute.For<ICustomerRepository>();
        branchRepository = Substitute.For<IBranchRepository>();
        cartRepository = Substitute.For<ICartRepository>();
        mapper = Substitute.For<IMapper>();
        eventPublisher = Substitute.For<ISaleEventPublisher>();
        return new UpdateSaleHandler(
            saleRepository,
            productRepository,
            customerRepository,
            branchRepository,
            cartRepository,
            mapper,
            eventPublisher,
            NullLogger<UpdateSaleHandler>.Instance);
    }

    private static Sale ExistingSale(int id, int? cartId = ExistingCartId)
    {
        var item = new SaleItem { Id = Random.Shared.Next(1, int.MaxValue), ProductId = 1, Quantity = 1, UnitPrice = 10m };
        item.RecalculatePricing();
        return new Sale
        {
            Id = id,
            SaleNumber = "UPD-1",
            SaleDate = DateTime.UtcNow.AddDays(-2),
            CustomerId = Random.Shared.Next(1, int.MaxValue),
            BranchId = Random.Shared.Next(1, int.MaxValue),
            CartId = cartId,
            IsCancelled = false,
            Items = new List<SaleItem> { item }
        };
    }

    private static Cart CartWithLine(int cartId, int productId, int quantity) =>
        new()
        {
            Id = cartId,
            UserId = 1,
            Date = DateTime.UtcNow,
            LineItems = new List<CartLineItem>
            {
                new() { Id = 1, CartId = cartId, ProductId = productId, Quantity = quantity }
            }
        };

    private static UpdateSaleCommand BuildValidCommand(
        int saleId,
        int customerId,
        int branchId,
        int cartId = ExistingCartId) =>
        new()
        {
            Id = saleId,
            SaleDate = DateTime.UtcNow,
            CustomerId = customerId,
            BranchId = branchId,
            CartId = cartId
        };

    [Fact]
    public async Task Handle_WhenSaleNotFound_ThrowsKeyNotFoundException()
    {
        var handler = CreateSut(out var saleRepo, out _, out _, out _, out _, out _, out _);
        saleRepo.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var command = BuildValidCommand(Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue));

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out _, out _, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var newCustomer = Random.Shared.Next(1, int.MaxValue);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(ExistingSale(saleId));
        customerRepo.GetByIdAsync(newCustomer, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var command = BuildValidCommand(saleId, newCustomer, Random.Shared.Next(1, int.MaxValue));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyCustomerId);
    }

    [Fact]
    public async Task Handle_WhenBranchDoesNotExist_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out _, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(ExistingSale(saleId));
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var command = BuildValidCommand(saleId, cid, bid);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyBranchId);
    }

    [Fact]
    public async Task Handle_WhenSaleHasNoLinkedCart_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out _, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(ExistingSale(saleId, cartId: null));
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });

        var command = BuildValidCommand(saleId, cid, bid);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyCartId);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExistOnCartReplacement_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int newCartId = 99;
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(ExistingSale(saleId, ExistingCartId));
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetTrackedByIdAsync(ExistingCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(ExistingCartId, 1, 1));
        cartRepo.GetByIdAsync(newCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(newCartId, 404, 2));
        saleRepo.ExistsSaleForCartAsync(newCartId, saleId, Arg.Any<CancellationToken>()).Returns(false);
        productRepo.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var command = BuildValidCommand(saleId, cid, bid, newCartId);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e => e.PropertyName == SaleSubmissionMessages.PropertyProductId);
    }

    [Fact]
    public async Task Handle_WhenSameCart_UpdatesHeaderAndCallsUpdateAsync()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out var cartRepo, out var mapper, out var events);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        var tracked = ExistingSale(saleId, ExistingCartId);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(tracked);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetByIdAsync(ExistingCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(ExistingCartId, 2, 4));

        saleRepo.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sale>());

        var mapped = new GetSaleResult { Id = saleId, SaleNumber = "UPD-1", TotalAmount = 0 };
        mapper.Map<GetSaleResult>(Arg.Any<Sale>()).Returns(mapped);

        var command = BuildValidCommand(saleId, cid, bid, ExistingCartId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(mapped);
        tracked.CustomerId.Should().Be(cid);
        tracked.BranchId.Should().Be(bid);
        tracked.CartId.Should().Be(ExistingCartId);
        tracked.Items.Should().HaveCount(1);
        tracked.Items.First().ProductId.Should().Be(1);
        await saleRepo.Received(1).UpdateAsync(tracked, Arg.Any<CancellationToken>());
        await saleRepo.DidNotReceive().ReplaceCartAndPersistAsync(
            Arg.Any<Sale>(),
            Arg.Any<Cart>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<SaleItem>>(),
            Arg.Any<CancellationToken>());
        events.Received(1).PublishSaleModified(Arg.Any<SaleModifiedPayload>());
    }

    [Fact]
    public async Task Handle_WhenCartIdChanged_CallsReplaceCartAndPersist()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out var mapper, out var events);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int newCartId = 99;
        var tracked = ExistingSale(saleId, ExistingCartId);
        var oldCart = CartWithLine(ExistingCartId, productId: 7, quantity: 3);
        var newCart = CartWithLine(newCartId, productId: 9, quantity: 2);

        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(tracked);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetTrackedByIdAsync(ExistingCartId, Arg.Any<CancellationToken>()).Returns(oldCart);
        cartRepo.GetByIdAsync(newCartId, Arg.Any<CancellationToken>()).Returns(newCart);
        saleRepo.ExistsSaleForCartAsync(newCartId, saleId, Arg.Any<CancellationToken>()).Returns(false);
        productRepo.GetByIdAsync(9, Arg.Any<CancellationToken>())
            .Returns(new Product { Id = 9, Title = "P9", Price = 25m, Description = "", CategoryId = 1, Image = "" });
        saleRepo.ReplaceCartAndPersistAsync(
                Arg.Any<Sale>(),
                Arg.Any<Cart>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyList<SaleItem>>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sale>());

        var mapped = new GetSaleResult { Id = saleId, SaleNumber = "UPD-1", TotalAmount = 50m };
        mapper.Map<GetSaleResult>(Arg.Any<Sale>()).Returns(mapped);

        var command = BuildValidCommand(saleId, cid, bid, newCartId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(mapped);
        await saleRepo.Received(1).ReplaceCartAndPersistAsync(
            tracked,
            oldCart,
            newCartId,
            Arg.Is<IReadOnlyList<SaleItem>>(items => items.Count == 1 && items[0].ProductId == 9 && items[0].Quantity == 2),
            Arg.Any<CancellationToken>());
        await saleRepo.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        events.Received(1).PublishSaleModified(Arg.Any<SaleModifiedPayload>());
    }

    [Fact]
    public async Task Handle_WhenNewCartDoesNotExist_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out var cartRepo, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int newCartId = 99;
        var tracked = ExistingSale(saleId, ExistingCartId);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(tracked);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetTrackedByIdAsync(ExistingCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(ExistingCartId, 7, 3));
        cartRepo.GetByIdAsync(newCartId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var command = BuildValidCommand(saleId, cid, bid, newCartId);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCartId &&
            e.ErrorMessage.Contains(newCartId.ToString()));
    }

    [Fact]
    public async Task Handle_WhenNewCartAlreadyHasAnotherSale_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out var cartRepo, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int newCartId = 99;
        var tracked = ExistingSale(saleId, ExistingCartId);
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(tracked);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetTrackedByIdAsync(ExistingCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(ExistingCartId, 7, 3));
        cartRepo.GetByIdAsync(newCartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(newCartId, 9, 2));
        saleRepo.ExistsSaleForCartAsync(newCartId, saleId, Arg.Any<CancellationToken>()).Returns(true);

        var command = BuildValidCommand(saleId, cid, bid, newCartId);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCartId &&
            e.ErrorMessage.Contains("já foi utilizado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenCancelledSale_ThrowsValidationExceptionFromDomain()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out _, out _, out _);
        var saleId = Random.Shared.Next(1, int.MaxValue);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        var sale = ExistingSale(saleId);
        sale.IsCancelled = true;
        saleRepo.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns(sale);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "X" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });

        var command = BuildValidCommand(saleId, cid, bid);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        ex.Errors.Should().Contain(e => e.ErrorMessage.Contains("cancelada", StringComparison.OrdinalIgnoreCase));
    }
}




