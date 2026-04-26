using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleEntityTests
{
    private static SaleItem Item(int productId = 1, int quantity = 2, decimal unitPrice = 10m) =>
        new() { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice };

    [Fact(DisplayName = "Sale.Create gera SaleNumber padrão quando não fornecido e calcula total")]
    public void Create_WithoutSaleNumber_GeneratesDefaultAndCalculatesTotal()
    {
        var sale = Sale.Create(
            DateTime.UtcNow,
            Random.Shared.Next(1, int.MaxValue),
            Random.Shared.Next(1, int.MaxValue),
            10,
            new[] { Item(quantity: 2, unitPrice: 50m) });

        sale.SaleNumber.Should().StartWith("VEN-");
        sale.Items.Should().HaveCount(1);
        sale.TotalAmount.Should().Be(100m);
        sale.IsCancelled.Should().BeFalse();
        sale.CartId.Should().Be(10);
        sale.Items.Single().SaleId.Should().Be(sale.Id);
    }

    [Fact(DisplayName = "Sale.Create usa SaleNumber explícito quando fornecido")]
    public void Create_WithExplicitSaleNumber_UsesIt()
    {
        var sale = Sale.Create(
            DateTime.UtcNow,
            Random.Shared.Next(1, int.MaxValue),
            Random.Shared.Next(1, int.MaxValue),
            5,
            new[] { Item() },
            saleNumber: "MY-NUM");

        sale.SaleNumber.Should().Be("MY-NUM");
    }

    [Fact(DisplayName = "Sale.Create preserva Id expl�cito em itens e aceita itens novos com Id zerado")]
    public void Create_PreservesExplicitItemIdAndAllowsTransientItems()
    {
        var existingId = Random.Shared.Next(1, int.MaxValue);
        var sale = Sale.Create(
            DateTime.UtcNow,
            Random.Shared.Next(1, int.MaxValue),
            Random.Shared.Next(1, int.MaxValue),
            7,
            new[]
            {
                Item(),
                new SaleItem { Id = existingId, ProductId = 2, Quantity = 1, UnitPrice = 5m }
            });

        sale.Items.Should().Contain(i => i.Id == 0);
        sale.Items.Should().Contain(i => i.Id == existingId);
    }

    [Fact(DisplayName = "EnsureNotCancelled lança DomainException quando cancelada")]
    public void EnsureNotCancelled_WhenCancelled_Throws()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });
        sale.Cancel();

        var act = () => sale.EnsureNotCancelled();

        act.Should().Throw<DomainException>().WithMessage("*cancelada*");
    }

    [Fact(DisplayName = "Cancel é idempotente e zera total")]
    public void Cancel_TwiceIsSafeAndZeroesTotal()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });

        sale.Cancel();
        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(0m);
    }

    [Fact(DisplayName = "ReplaceItems substitui linhas e recalcula total")]
    public void ReplaceItems_RebuildsItemsAndRecalculatesTotal()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item(quantity: 2, unitPrice: 50m) });

        sale.ReplaceItems(new[]
        {
            new SaleItem { ProductId = 99, Quantity = 5, UnitPrice = 8m }
        });

        sale.Items.Should().HaveCount(1);
        sale.Items.Single().ProductId.Should().Be(99);
        sale.TotalAmount.Should().Be(36m);
    }

    [Fact(DisplayName = "ReplaceItems não permitido em venda cancelada")]
    public void ReplaceItems_WhenCancelled_Throws()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });
        sale.Cancel();

        var act = () => sale.ReplaceItems(new[] { Item() });

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "UpdateHeader atualiza data, customer e branch")]
    public void UpdateHeader_UpdatesFieldsAndChecksCancellation()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });
        var newDate = DateTime.UtcNow.AddDays(1);
        var newCustomer = Random.Shared.Next(1, int.MaxValue);
        var newBranch = Random.Shared.Next(1, int.MaxValue);

        sale.UpdateHeader(newDate, newCustomer, newBranch);

        sale.SaleDate.Should().Be(newDate);
        sale.CustomerId.Should().Be(newCustomer);
        sale.BranchId.Should().Be(newBranch);
    }

    [Fact(DisplayName = "ChangeCart atualiza cartId quando válido")]
    public void ChangeCart_WithValidId_Updates()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });

        sale.ChangeCart(99);

        sale.CartId.Should().Be(99);
    }

    [Theory(DisplayName = "ChangeCart com id inválido lança DomainException")]
    [InlineData(0)]
    [InlineData(-3)]
    public void ChangeCart_WithInvalidId_Throws(int invalid)
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[] { Item() });

        var act = () => sale.ChangeCart(invalid);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "RefreshTotal ignora itens cancelados na soma")]
    public void RefreshTotal_IgnoresCancelledItems()
    {
        var sale = Sale.Create(DateTime.UtcNow, Random.Shared.Next(1, int.MaxValue), Random.Shared.Next(1, int.MaxValue), 1, new[]
        {
            Item(quantity: 1, unitPrice: 100m),
            Item(productId: 2, quantity: 1, unitPrice: 50m)
        });

        sale.Items.First().IsCancelled = true;
        sale.RefreshTotal();

        sale.TotalAmount.Should().Be(50m);
    }
}




