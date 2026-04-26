using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Customers;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Customers;

/// <summary>
/// Cobertura unitária do <see cref="CustomersController"/> para todos os endpoints e branches.
/// </summary>
public class CustomersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _controller = new CustomersController(_mediator, _mapper);
        _controller.WithEmptyContext();
    }

    private static CustomerDto FakeDto(int id) => new() { Id = id, Name = "Cliente" };

    [Fact(DisplayName = "List: retorna 200 com payload mapeado")]
    public async Task List_WhenInvoked_ReturnsOk()
    {
        var commandResult = new ListCustomersResult
        {
            Data = new List<CustomerDto> { FakeDto(Random.Shared.Next(1, int.MaxValue)) },
            TotalItems = 1,
            CurrentPage = 1,
            TotalPages = 1
        };
        var responseDto = new ListCustomersResponse
        {
            Data = new List<CustomerResponse>(),
            TotalItems = 1,
            CurrentPage = 1,
            TotalPages = 1
        };
        _mediator.Send(Arg.Is<ListCustomersCommand>(c => c.Page == 3 && c.Size == 4), Arg.Any<CancellationToken>())
                 .Returns(commandResult);
        _mapper.Map<ListCustomersResponse>(commandResult).Returns(responseDto);

        var response = await _controller.List(3, 4, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "GetById: id existente retorna 200 com CustomerResponse")]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var dto = FakeDto(id);
        var responseDto = new CustomerResponse { Id = id, Name = dto.Name };

        _mediator.Send(Arg.Is<GetCustomerCommand>(c => c.Id == id), Arg.Any<CancellationToken>())
                 .Returns(dto);
        _mapper.Map<CustomerResponse>(dto).Returns(responseDto);

        var response = await _controller.GetById(id, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "GetById: KeyNotFoundException retorna 404")]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CustomerDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetById(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: payload válido retorna 201 (CreatedAtAction)")]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var dto = FakeDto(id);
        var responseDto = new CustomerResponse { Id = id, Name = dto.Name };

        _mediator.Send(Arg.Is<CreateCustomerCommand>(c => c.Name == "Cliente"), Arg.Any<CancellationToken>())
                 .Returns(dto);
        _mapper.Map<CustomerResponse>(dto).Returns(responseDto);

        var response = await _controller.Create(new CreateCustomerRequest { Name = "Cliente" }, CancellationToken.None);

        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(CustomersController.GetById));
        created.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "Create: payload inválido retorna 400")]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Create(new CreateCustomerRequest { Name = string.Empty }, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: handler dispara ValidationException retorna 400")]
    public async Task Create_WhenHandlerValidationException_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<CreateCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CustomerDto>(_ => throw new ValidationException(new[]
                 {
                     new ValidationFailure("Name", "duplicado")
                 }));

        var response = await _controller.Create(new CreateCustomerRequest { Name = "Cliente" }, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: payload válido retorna 200")]
    public async Task Update_WhenValid_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var dto = FakeDto(id);
        var responseDto = new CustomerResponse { Id = id, Name = "Cliente v2" };

        _mediator.Send(Arg.Is<UpdateCustomerCommand>(c => c.Id == id && c.Name == "Cliente v2"),
                       Arg.Any<CancellationToken>()).Returns(dto);
        _mapper.Map<CustomerResponse>(dto).Returns(responseDto);

        var response = await _controller.Update(id, new UpdateCustomerRequest { Name = "Cliente v2" }, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "Update: payload inválido retorna 400")]
    public async Task Update_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue),
            new UpdateCustomerRequest { Name = string.Empty },
            CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: KeyNotFoundException retorna 404")]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CustomerDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue),
            new UpdateCustomerRequest { Name = "Cliente" },
            CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Update: ValidationException retorna 400")]
    public async Task Update_WhenValidationException_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<UpdateCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<CustomerDto>(_ => throw new ValidationException(new[]
                 {
                     new ValidationFailure("Name", "inválido")
                 }));

        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue),
            new UpdateCustomerRequest { Name = "Cliente" },
            CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Delete: válido retorna 200")]
    public async Task Delete_WhenInvoked_ReturnsOk()
    {
        _mediator.Send(Arg.Is<DeleteCustomerCommand>(c => c.Id != 0), Arg.Any<CancellationToken>())
                 .Returns(new DeleteCustomerResult { Deleted = true });

        var response = await _controller.Delete(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "Delete: KeyNotFoundException retorna 404")]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<DeleteCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteCustomerResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Delete(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Delete: ValidationException retorna 400")]
    public async Task Delete_WhenValidationException_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<DeleteCustomerCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteCustomerResult>(_ => throw new ValidationException(new[]
                 {
                     new ValidationFailure("Id", "Cliente em uso")
                 }));

        var response = await _controller.Delete(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }
}




