using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Customers;

public class CustomerHandlersTests
{
    private static IMapper Mapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(ci =>
        {
            var c = ci.Arg<Customer>();
            return new CustomerDto { Id = c.Id, Name = c.Name };
        });
        return mapper;
    }

    [Fact(DisplayName = "CreateCustomer: comando inválido lança ValidationException")]
    public async Task CreateCustomer_InvalidCommand_Throws()
    {
        var handler = new CreateCustomerHandler(Substitute.For<ICustomerRepository>(), Mapper());
        var act = async () => await handler.Handle(new CreateCustomerCommand { Name = "" }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "CreateCustomer: válido persiste e mapeia DTO")]
    public async Task CreateCustomer_Valid_PersistsAndMaps()
    {
        var customers = Substitute.For<ICustomerRepository>();
        customers.CreateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Customer>());
        var handler = new CreateCustomerHandler(customers, Mapper());

        var dto = await handler.Handle(new CreateCustomerCommand { Name = " Acme " }, CancellationToken.None);

        dto.Name.Should().Be("Acme");
        await customers.Received(1).CreateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetCustomer: id vazio lança ValidationException")]
    public async Task GetCustomer_EmptyId_Throws()
    {
        var handler = new GetCustomerHandler(Substitute.For<ICustomerRepository>(), Mapper());
        var act = async () => await handler.Handle(new GetCustomerCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "GetCustomer: não encontrado lança KeyNotFound")]
    public async Task GetCustomer_NotFound_Throws()
    {
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = new GetCustomerHandler(customers, Mapper());

        var act = async () => await handler.Handle(new GetCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue) }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetCustomer: encontrado retorna DTO")]
    public async Task GetCustomer_Found_ReturnsDto()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Customer { Id = id, Name = "X" });
        var handler = new GetCustomerHandler(customers, Mapper());

        var dto = await handler.Handle(new GetCustomerCommand { Id = id }, CancellationToken.None);

        dto.Id.Should().Be(id);
    }

    [Fact(DisplayName = "DeleteCustomer: id vazio lança ValidationException")]
    public async Task DeleteCustomer_EmptyId_Throws()
    {
        var handler = new DeleteCustomerHandler(Substitute.For<ICustomerRepository>());
        var act = async () => await handler.Handle(new DeleteCustomerCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "DeleteCustomer: não encontrado lança KeyNotFound")]
    public async Task DeleteCustomer_NotFound_Throws()
    {
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = new DeleteCustomerHandler(customers);

        var act = async () => await handler.Handle(new DeleteCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue) }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "DeleteCustomer: com vendas lança ValidationException")]
    public async Task DeleteCustomer_HasSales_Throws()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Customer { Id = id });
        customers.HasSalesAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteCustomerHandler(customers);

        var act = async () => await handler.Handle(new DeleteCustomerCommand { Id = id }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("vendas vinculadas", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "DeleteCustomer: válido remove")]
    public async Task DeleteCustomer_Valid_Removes()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Customer { Id = id });
        customers.HasSalesAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        customers.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteCustomerHandler(customers);

        var result = await handler.Handle(new DeleteCustomerCommand { Id = id }, CancellationToken.None);

        result.Deleted.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateCustomer: comando inválido lança ValidationException")]
    public async Task UpdateCustomer_InvalidCommand_Throws()
    {
        var handler = new UpdateCustomerHandler(Substitute.For<ICustomerRepository>(), Mapper());
        var act = async () => await handler.Handle(new UpdateCustomerCommand { Id = 0, Name = "" }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "UpdateCustomer: não encontrado lança KeyNotFound")]
    public async Task UpdateCustomer_NotFound_Throws()
    {
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetTrackedByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var handler = new UpdateCustomerHandler(customers, Mapper());

        var act = async () => await handler.Handle(new UpdateCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "X" }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateCustomer: válido persiste e mapeia DTO")]
    public async Task UpdateCustomer_Valid_Updates()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var entity = new Customer { Id = id, Name = "old" };
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        customers.UpdateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
        var handler = new UpdateCustomerHandler(customers, Mapper());

        var dto = await handler.Handle(new UpdateCustomerCommand { Id = id, Name = " new " }, CancellationToken.None);

        entity.Name.Should().Be("new");
        dto.Name.Should().Be("new");
    }

    [Fact(DisplayName = "ListCustomers: comando inválido lança ValidationException")]
    public async Task ListCustomers_InvalidCommand_Throws()
    {
        var handler = new ListCustomersHandler(Substitute.For<ICustomerRepository>(), Mapper());
        var act = async () => await handler.Handle(new ListCustomersCommand { Page = 0, Size = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "ListCustomers: paginação retorna mapeamento e contagens")]
    public async Task ListCustomers_Valid_ReturnsResult()
    {
        var customers = Substitute.For<ICustomerRepository>();
        var items = new List<Customer>
        {
            new() { Id = Random.Shared.Next(1, int.MaxValue), Name = "A" },
            new() { Id = Random.Shared.Next(1, int.MaxValue), Name = "B" }
        };
        customers.ListPagedAsync(2, 5, Arg.Any<CancellationToken>()).Returns((items, 8));
        var handler = new ListCustomersHandler(customers, Mapper());

        var result = await handler.Handle(new ListCustomersCommand { Page = 2, Size = 5 }, CancellationToken.None);

        result.TotalItems.Should().Be(8);
        result.TotalPages.Should().Be(2);
        result.CurrentPage.Should().Be(2);
        result.Data.Should().HaveCount(2);
    }
}

public class CustomerValidatorsTests
{
    [Fact(DisplayName = "CreateCustomerCommandValidator regras")]
    public void CreateCustomerCommandValidator_ValidationRules()
    {
        var v = new CreateCustomerCommandValidator();
        v.Validate(new CreateCustomerCommand { Name = "Ok" }).IsValid.Should().BeTrue();
        v.Validate(new CreateCustomerCommand { Name = "" }).IsValid.Should().BeFalse();
        v.Validate(new CreateCustomerCommand { Name = new string('x', 201) }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "UpdateCustomerCommandValidator regras")]
    public void UpdateCustomerCommandValidator_ValidationRules()
    {
        var v = new UpdateCustomerCommandValidator();
        v.Validate(new UpdateCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "Ok" }).IsValid.Should().BeTrue();
        v.Validate(new UpdateCustomerCommand { Id = 0, Name = "Ok" }).IsValid.Should().BeFalse();
        v.Validate(new UpdateCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue), Name = "" }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "DeleteCustomerCommandValidator regras")]
    public void DeleteCustomerCommandValidator_ValidationRules()
    {
        var v = new DeleteCustomerCommandValidator();
        v.Validate(new DeleteCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue) }).IsValid.Should().BeTrue();
        v.Validate(new DeleteCustomerCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "GetCustomerCommandValidator regras")]
    public void GetCustomerCommandValidator_ValidationRules()
    {
        var v = new GetCustomerCommandValidator();
        v.Validate(new GetCustomerCommand { Id = Random.Shared.Next(1, int.MaxValue) }).IsValid.Should().BeTrue();
        v.Validate(new GetCustomerCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Theory(DisplayName = "ListCustomersCommandValidator regras")]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListCustomersCommandValidator_ValidationRules(int page, int size, bool expected)
    {
        var v = new ListCustomersCommandValidator();
        v.Validate(new ListCustomersCommand { Page = page, Size = size }).IsValid.Should().Be(expected);
    }
}




