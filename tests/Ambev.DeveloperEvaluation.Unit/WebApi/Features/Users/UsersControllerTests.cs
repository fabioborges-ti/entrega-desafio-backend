using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.ListUsers;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Testes unitários do <see cref="UsersController"/> cobrindo todos os branches
/// (200/201, 400, 404, 409) e os getters do <see cref="BaseController"/>.
/// </summary>
public class UsersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator, _mapper);
    }

    private static CreateUserRequest ValidCreateRequest() => new()
    {
        Email = "user@example.com",
        Username = "user01",
        Password = "P@ssword1",
        Phone = "+5511988887777",
        Status = UserStatus.Active,
        Role = UserRole.Admin,
        Name = new UserNameContract { FirstName = "John", LastName = "Doe" },
        Address = new UserAddressContract
        {
            City = "São Paulo",
            Street = "Av. Paulista",
            Number = 1000,
            Zipcode = "01310-100",
            Geolocation = new GeolocationContract { Lat = "-23.55", Long = "-46.63" }
        }
    };

    private static UpdateUserRequest ValidUpdateRequest()
    {
        var c = ValidCreateRequest();
        return new UpdateUserRequest
        {
            Email = c.Email,
            Username = c.Username,
            Password = null,
            Phone = c.Phone,
            Status = c.Status,
            Role = c.Role,
            Name = c.Name,
            Address = c.Address
        };
    }

    private static GetUserResponse FakeResponse(int id = 1) => new()
    {
        Id = id,
        Email = "user@example.com",
        Username = "user01",
        Phone = "+5511988887777",
        Status = UserStatus.Active,
        Role = UserRole.Admin,
        Name = new UserNameContract { FirstName = "John", LastName = "Doe" },
        Address = new UserAddressContract
        {
            City = "São Paulo",
            Street = "Av. Paulista",
            Number = 1000,
            Zipcode = "01310-100",
            Geolocation = new GeolocationContract { Lat = "-23.55", Long = "-46.63" }
        }
    };

    [Fact(DisplayName = "ListUsers: retorna 200 com payload paginado")]
    public async Task ListUsers_WhenInvoked_ReturnsOkWithPagedList()
    {
        var listResult = new ListUsersResult
        {
            Data = new List<GetUserResult> { new() { Id = 1, Email = "a@b.c", Username = "u1" } },
            TotalItems = 1,
            CurrentPage = 1,
            TotalPages = 1
        };
        _mediator.Send(Arg.Any<ListUsersQuery>(), Arg.Any<CancellationToken>()).Returns(listResult);
        _mapper.Map<GetUserResponse>(Arg.Any<GetUserResult>()).Returns(FakeResponse());

        var response = await _controller.ListUsers(2, 5, "id desc", CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<UsersPagedListResponse>>().Subject;
        outer.Success.Should().BeTrue();
        outer.Data!.Data.Should().HaveCount(1);
        outer.Data!.TotalItems.Should().Be(1);
        await _mediator.Received(1).Send(
            Arg.Is<ListUsersQuery>(q => q.Page == 2 && q.PageSize == 5 && q.Order == "id desc"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CreateUser: dados válidos retornam 201 (CreatedAtRoute)")]
    public async Task CreateUser_WhenValid_ReturnsCreated()
    {
        var request = ValidCreateRequest();
        var command = new CreateUserCommand();
        var commandResult = new CreateUserResult { Id = 42, Email = request.Email, Username = request.Username };

        _mapper.Map<CreateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(commandResult);
        _mapper.Map<CreateUserResponse>(commandResult).Returns(new CreateUserResponse { Id = 42 });

        var response = await _controller.CreateUser(request, CancellationToken.None);

        var created = response.Should().BeOfType<CreatedResult>().Subject;
        created.Value.Should().BeOfType<ApiResponseWithData<CreateUserResponse>>()
               .Which.Data!.Id.Should().Be(42);
    }

    [Fact(DisplayName = "CreateUser: request inválido retorna 400")]
    public async Task CreateUser_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.CreateUser(new CreateUserRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "CreateUser: e-mail duplicado dispara InvalidOperationException �?' 409")]
    public async Task CreateUser_WhenInvalidOperationException_ReturnsConflict()
    {
        var request = ValidCreateRequest();
        var command = new CreateUserCommand();
        _mapper.Map<CreateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns<CreateUserResult>(_ => throw new InvalidOperationException("dup email"));

        var response = await _controller.CreateUser(request, CancellationToken.None);

        var conflict = response.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeOfType<ApiResponse>().Which.Message.Should().Be("dup email");
    }

    [Fact(DisplayName = "CreateUser: handler dispara ValidationException �?' 400")]
    public async Task CreateUser_WhenHandlerValidationException_ReturnsBadRequest()
    {
        var request = ValidCreateRequest();
        var command = new CreateUserCommand();
        _mapper.Map<CreateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns<CreateUserResult>(_ => throw new ValidationException(new[]
            {
                new ValidationFailure("Username", "duplicado")
            }));

        var response = await _controller.CreateUser(request, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "GetUser: id válido e usuário existente retorna 200")]
    public async Task GetUser_WhenFound_ReturnsOk()
    {
        var commandResult = new GetUserResult { Id = 7, Email = "x@y.z", Username = "u" };
        _mapper.Map<GetUserCommand>(7).Returns(new GetUserCommand(7));
        _mediator.Send(Arg.Any<GetUserCommand>(), Arg.Any<CancellationToken>()).Returns(commandResult);
        _mapper.Map<GetUserResponse>(commandResult).Returns(FakeResponse(7));

        var response = await _controller.GetUser(7, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<ApiResponseWithData<GetUserResponse>>>().Subject;
        outer.Data!.Data!.Id.Should().Be(7);
    }

    [Fact(DisplayName = "GetUser: id <= 0 retorna 400")]
    public async Task GetUser_WhenIdInvalid_ReturnsBadRequest()
    {
        var response = await _controller.GetUser(0, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<GetUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetUser: KeyNotFoundException retorna 404")]
    public async Task GetUser_WhenNotFound_ReturnsNotFound()
    {
        _mapper.Map<GetUserCommand>(99).Returns(new GetUserCommand(99));
        _mediator.Send(Arg.Any<GetUserCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetUserResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetUser(99, CancellationToken.None);

        var nf = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        nf.Value.Should().BeOfType<ApiErrorResponse>()
          .Which.Type.Should().Be("ResourceNotFound");
    }

    [Fact(DisplayName = "UpdateUser: dados válidos retornam 200")]
    public async Task UpdateUser_WhenValid_ReturnsOk()
    {
        var request = ValidUpdateRequest();
        var command = new UpdateUserCommand();
        var commandResult = new GetUserResult { Id = 5 };

        _mapper.Map<UpdateUserCommand>(request).Returns(command);
        _mediator.Send(Arg.Is<UpdateUserCommand>(c => c.Id == 5), Arg.Any<CancellationToken>())
            .Returns(commandResult);
        _mapper.Map<GetUserResponse>(commandResult).Returns(FakeResponse(5));

        var response = await _controller.UpdateUser(5, request, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<ApiResponseWithData<GetUserResponse>>>().Subject;
        outer.Data!.Data!.Id.Should().Be(5);
    }

    [Fact(DisplayName = "UpdateUser: request inválido retorna 400")]
    public async Task UpdateUser_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.UpdateUser(1, new UpdateUserRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "UpdateUser: KeyNotFoundException retorna 404")]
    public async Task UpdateUser_WhenNotFound_ReturnsNotFound()
    {
        var request = ValidUpdateRequest();
        var command = new UpdateUserCommand();
        _mapper.Map<UpdateUserCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetUserResult>(_ => throw new KeyNotFoundException("not found"));

        var response = await _controller.UpdateUser(99, request, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "UpdateUser: InvalidOperationException retorna 409")]
    public async Task UpdateUser_WhenConflict_ReturnsConflict()
    {
        var request = ValidUpdateRequest();
        var command = new UpdateUserCommand();
        _mapper.Map<UpdateUserCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetUserResult>(_ => throw new InvalidOperationException("conflict"));

        var response = await _controller.UpdateUser(1, request, CancellationToken.None);

        response.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact(DisplayName = "UpdateUser: ValidationException retorna 400")]
    public async Task UpdateUser_WhenValidationException_ReturnsBadRequest()
    {
        var request = ValidUpdateRequest();
        var command = new UpdateUserCommand();
        _mapper.Map<UpdateUserCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetUserResult>(_ => throw new ValidationException(new[]
            {
                new ValidationFailure("Email", "inválido")
            }));

        var response = await _controller.UpdateUser(1, request, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "DeleteUser: id válido retorna 200 com usuário removido")]
    public async Task DeleteUser_WhenFound_ReturnsOk()
    {
        var commandResult = new GetUserResult { Id = 12 };
        _mapper.Map<DeleteUserCommand>(12).Returns(new DeleteUserCommand(12));
        _mediator.Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>()).Returns(commandResult);
        _mapper.Map<GetUserResponse>(commandResult).Returns(FakeResponse(12));

        var response = await _controller.DeleteUser(12, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var outer = ok.Value.Should().BeOfType<ApiResponseWithData<ApiResponseWithData<GetUserResponse>>>().Subject;
        outer.Data!.Data!.Id.Should().Be(12);
    }

    [Fact(DisplayName = "DeleteUser: id <= 0 retorna 400")]
    public async Task DeleteUser_WhenIdInvalid_ReturnsBadRequest()
    {
        var response = await _controller.DeleteUser(0, CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "DeleteUser: KeyNotFoundException retorna 404")]
    public async Task DeleteUser_WhenNotFound_ReturnsNotFound()
    {
        _mapper.Map<DeleteUserCommand>(99).Returns(new DeleteUserCommand(99));
        _mediator.Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>())
            .Returns<GetUserResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.DeleteUser(99, CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }
}

