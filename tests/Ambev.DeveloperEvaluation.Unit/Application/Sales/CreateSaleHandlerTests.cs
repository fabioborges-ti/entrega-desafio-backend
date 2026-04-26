using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleHandlerTests
{
    private static CreateSaleHandler CreateSut(
        out ISaleRepository saleRepository,
        out IProductRepository productRepository,
        out ICustomerRepository customerRepository,
        out IBranchRepository branchRepository,
        out ICartRepository cartRepository,
        out ISaleEventPublisher eventPublisher)
    {
        saleRepository = Substitute.For<ISaleRepository>();
        productRepository = Substitute.For<IProductRepository>();
        customerRepository = Substitute.For<ICustomerRepository>();
        branchRepository = Substitute.For<IBranchRepository>();
        cartRepository = Substitute.For<ICartRepository>();
        eventPublisher = Substitute.For<ISaleEventPublisher>();
        return new CreateSaleHandler(
            saleRepository,
            productRepository,
            customerRepository,
            branchRepository,
            cartRepository,
            eventPublisher,
            NullLogger<CreateSaleHandler>.Instance);
    }

    private static CreateSaleCommand BaseCommand(int customerId, int branchId, int cartId) =>
        new()
        {
            SaleDate = DateTime.UtcNow,
            CustomerId = customerId,
            BranchId = branchId,
            CartId = cartId
        };

    private static Cart CartWithLine(int cartId, int productId = 1, int quantity = 2) =>
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

    private static void ArrangeHappyPathRepositories(
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ISaleRepository saleRepository,
        int customerId,
        int branchId,
        int cartId,
        int productId = 1,
        int quantity = 2)
    {
        customerRepository.GetByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = customerId, Name = "C" });
        branchRepository.GetByIdAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = branchId, Name = "B", Cnpj = "12345678000199" });
        cartRepository.GetByIdAsync(cartId, Arg.Any<CancellationToken>())
            .Returns(CartWithLine(cartId, productId, quantity));
        productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new Product { Id = productId, Title = "P", Price = 10m, Description = "", CategoryId = 1, Image = "" });
        saleRepository.ExistsSaleForCartAsync(cartId, Arg.Any<CancellationToken>()).Returns(false);
        saleRepository.ExistsSaleNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ThrowsValidationExceptionWithCustomerProperty()
    {
        var handler = CreateSut(out _, out _, out var customerRepo, out _, out _, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var command = BaseCommand(cid, bid, 5);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCustomerId &&
            e.ErrorMessage.Contains(cid.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WhenBranchDoesNotExist_ThrowsValidationExceptionWithBranchProperty()
    {
        var handler = CreateSut(out _, out _, out var customerRepo, out var branchRepo, out _, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "C" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var command = BaseCommand(cid, bid, 3);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyBranchId &&
            e.ErrorMessage.Contains(bid.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WhenCartDoesNotExist_ThrowsValidationExceptionWithCartProperty()
    {
        var handler = CreateSut(out _, out _, out var customerRepo, out var branchRepo, out var cartRepo, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "C" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        var command = BaseCommand(cid, bid, 99);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCartId &&
            e.ErrorMessage.Contains("99", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WhenCartHasNoLineItems_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out var customerRepo, out var branchRepo, out var cartRepo, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int cartId = 42;
        customerRepo.GetByIdAsync(cid, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = cid, Name = "C" });
        branchRepo.GetByIdAsync(bid, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = bid, Name = "B", Cnpj = "12345678000199" });
        cartRepo.GetByIdAsync(cartId, Arg.Any<CancellationToken>())
            .Returns(new Cart { Id = cartId, UserId = 1, Date = DateTime.UtcNow, LineItems = new List<CartLineItem>() });
        saleRepo.ExistsSaleForCartAsync(cartId, Arg.Any<CancellationToken>()).Returns(false);

        var command = BaseCommand(cid, bid, cartId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCartId &&
            e.ErrorMessage.Contains("não possui itens", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenCartAlreadyLinkedToSale_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        var cartId = 7;
        ArrangeHappyPathRepositories(customerRepo, branchRepo, cartRepo, productRepo, saleRepo, cid, bid, cartId);
        saleRepo.ExistsSaleForCartAsync(cartId, Arg.Any<CancellationToken>()).Returns(true);

        var command = BaseCommand(cid, bid, cartId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyCartId &&
            e.ErrorMessage.Contains("já foi utilizado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsValidationExceptionWithItemsProperty()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        ArrangeHappyPathRepositories(customerRepo, branchRepo, cartRepo, productRepo, saleRepo, cid, bid, 2, productId: 999, quantity: 1);
        productRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var command = BaseCommand(cid, bid, 2);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e =>
            e.PropertyName == SaleSubmissionMessages.PropertyProductId &&
            e.ErrorMessage.Contains("999", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_WhenSaleNumberAlreadyExists_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out _, out _, out _, out _, out _);
        saleRepo.ExistsSaleNumberAsync("SN-DUP", Arg.Any<CancellationToken>()).Returns(true);

        var command = BaseCommand(Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1);
        command.SaleNumber = "SN-DUP";

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSaleCommand.SaleNumber));
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsPolicy_ThrowsValidationException()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out _);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        ArrangeHappyPathRepositories(customerRepo, branchRepo, cartRepo, productRepo, saleRepo, cid, bid, 4, quantity: 21);

        var command = BaseCommand(cid, bid, 4);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("permitido vender acima", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenValid_PersistsSaleAndPublishesEvent()
    {
        var handler = CreateSut(out var saleRepo, out var productRepo, out var customerRepo, out var branchRepo, out var cartRepo, out var events);
        var cid = Random.Shared.Next(1, int.MaxValue);
        var bid = Random.Shared.Next(1, int.MaxValue);
        const int cartId = 11;
        ArrangeHappyPathRepositories(customerRepo, branchRepo, cartRepo, productRepo, saleRepo, cid, bid, cartId, quantity: 3);

        Sale? persisted = null;
        saleRepo.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                persisted = ci.Arg<Sale>();
                return persisted;
            });

        var command = BaseCommand(cid, bid, cartId);

        var result = await handler.Handle(command, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.CustomerId.Should().Be(cid);
        persisted.BranchId.Should().Be(bid);
        persisted.CartId.Should().Be(cartId);
        persisted.Items.Should().HaveCount(1);
        result.CartId.Should().Be(cartId);
        result.TotalAmount.Should().Be(persisted.TotalAmount);
        await saleRepo.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        events.Received(1).PublishSaleCreated(Arg.Any<SaleCreatedPayload>());
    }

    [Fact]
    public async Task Handle_WhenCommandInvalid_ThrowsValidationExceptionBeforeRepositories()
    {
        var handler = CreateSut(out _, out _, out var customerRepo, out _, out _, out _);

        var command = new CreateSaleCommand
        {
            SaleDate = default,
            CustomerId = 0,
            BranchId = Random.Shared.Next(1, int.MaxValue),
            CartId = 0
        };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await customerRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}




