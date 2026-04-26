using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class BasicEntitiesTests
{
    [Fact(DisplayName = "User.Activate, Deactivate e Suspend mudam status e atualizam UpdatedAt")]
    public void User_StatusTransitionsUpdateBothStatusAndTimestamp()
    {
        var user = new User { Status = UserStatus.Inactive };

        user.Activate();
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().NotBeNull();

        user.Suspend();
        user.Status.Should().Be(UserStatus.Suspended);

        user.Deactivate();
        user.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact(DisplayName = "User.IUser expõe Id como string e propriedades convertidas")]
    public void User_IUserAdapterExposesStringRepresentations()
    {
        var user = new User
        {
            Id = 5,
            Username = "u",
            Email = "u@x.com",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };
        IUser asContract = user;

        asContract.Id.Should().Be("5");
        asContract.Username.Should().Be("u");
        asContract.Email.Should().Be("u@x.com");
        asContract.Role.Should().Be("Admin");
        asContract.Status.Should().Be("Active");
    }

    [Fact(DisplayName = "User.Validate retorna IsValid=false quando dados são inválidos")]
    public void User_Validate_WithInvalidData_ReturnsErrors()
    {
        var user = new User();

        var result = user.Validate();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "BaseEntity.CompareTo retorna 1 quando comparado com null")]
    public void BaseEntity_CompareTo_NullReturnsOne()
    {
        var entity = new Customer { Id = Random.Shared.Next(1, int.MaxValue) };

        entity.CompareTo(null).Should().Be(1);
    }

    [Fact(DisplayName = "BaseEntity.CompareTo é inverso ao trocar a ordem")]
    public void BaseEntity_CompareTo_OrdersById()
    {
        var firstId = 1;
        var secondId = 2;
        BaseEntity a = new Customer { Id = firstId };
        BaseEntity b = new Customer { Id = secondId };

        a.CompareTo(b).Should().NotBe(0);
        Math.Sign(a.CompareTo(b)).Should().Be(-Math.Sign(b.CompareTo(a)));
    }

    [Fact(DisplayName = "BaseEntity.ValidateAsync delega para Validator estático")]
    public async Task BaseEntity_ValidateAsync_DelegatesToStaticValidator()
    {
        var entity = new Customer { Id = Random.Shared.Next(1, int.MaxValue), Name = "X" };

        var act = async () => await entity.ValidateAsync();

        await act.Should().ThrowAsync<MissingMethodException>();
    }

    [Fact(DisplayName = "DomainException expõe mensagem e inner exception")]
    public void DomainException_ConstructorsPreserveMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DomainException("msg", inner);

        ex.Message.Should().Be("msg");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact(DisplayName = "DomainException com somente mensagem é preservada")]
    public void DomainException_WithMessage_PreservesMessage()
    {
        var ex = new DomainException("apenas mensagem");

        ex.Message.Should().Be("apenas mensagem");
        ex.InnerException.Should().BeNull();
    }

    [Fact(DisplayName = "UserRegisteredEvent guarda referência ao usuário")]
    public void UserRegisteredEvent_StoresUserReference()
    {
        var user = new User { Id = 1, Username = "u" };
        var ev = new UserRegisteredEvent(user);

        ev.User.Should().BeSameAs(user);
    }

    [Fact(DisplayName = "Branch defaults: Sales lista vazia")]
    public void Branch_DefaultsAreEmpty()
    {
        var branch = new Branch();

        branch.Sales.Should().BeEmpty();
        branch.Name.Should().BeEmpty();
        branch.Cnpj.Should().BeEmpty();
    }

    [Fact(DisplayName = "Customer defaults: Sales lista vazia")]
    public void Customer_DefaultsAreEmpty()
    {
        var customer = new Customer();

        customer.Sales.Should().BeEmpty();
        customer.Name.Should().BeEmpty();
    }

    [Fact(DisplayName = "Cart defaults: LineItems lista vazia")]
    public void Cart_DefaultsAreEmpty()
    {
        var cart = new Cart();

        cart.LineItems.Should().BeEmpty();
        cart.Sale.Should().BeNull();
    }

    [Fact(DisplayName = "Product defaults: coleções vazias")]
    public void Product_DefaultsAreEmpty()
    {
        var product = new Product();

        product.UserRatings.Should().BeEmpty();
        product.CartLineItems.Should().BeEmpty();
        product.Inventory.Should().BeNull();
    }

    [Fact(DisplayName = "Category defaults: products vazia")]
    public void Category_DefaultsAreEmpty()
    {
        var category = new Category();

        category.Products.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProductUserRating tem propriedades default")]
    public void ProductUserRating_DefaultsAreSet()
    {
        var rating = new ProductUserRating();

        rating.Rate.Should().Be(0m);
        rating.ProductId.Should().Be(0);
    }

    [Fact(DisplayName = "Inventory tem propriedades default")]
    public void Inventory_DefaultsAreSet()
    {
        var inventory = new Inventory();

        inventory.AvailableQuantity.Should().Be(0);
        inventory.Product.Should().BeNull();
    }

    [Fact(DisplayName = "ExternalIdentity tem propriedades default")]
    public void ExternalIdentity_DefaultsAreSet()
    {
        var external = new ExternalIdentity();

        external.ExternalId.Should().Be(Guid.Empty);
        external.Name.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProductRating tem propriedades default")]
    public void ProductRating_DefaultsAreSet()
    {
        var rating = new ProductRating();

        rating.Rate.Should().Be(0m);
        rating.Count.Should().Be(0);
    }

    [Fact(DisplayName = "AddressGeolocation tem propriedades default")]
    public void AddressGeolocation_DefaultsAreSet()
    {
        var geo = new AddressGeolocation();

        geo.Lat.Should().BeEmpty();
        geo.Long.Should().BeEmpty();
    }

    [Fact(DisplayName = "UserPersonName tem propriedades default")]
    public void UserPersonName_DefaultsAreSet()
    {
        var name = new UserPersonName();

        name.FirstName.Should().BeEmpty();
        name.LastName.Should().BeEmpty();
    }

    [Fact(DisplayName = "UserAddress tem propriedades default")]
    public void UserAddress_DefaultsAreSet()
    {
        var address = new UserAddress();

        address.City.Should().BeEmpty();
        address.Street.Should().BeEmpty();
        address.Number.Should().Be(0);
        address.Geolocation.Should().NotBeNull();
    }
}




