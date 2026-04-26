using Ambev.DeveloperEvaluation.Application.Branches;
using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Branches;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Branches;

/// <summary>
/// Cobertura unitária para todos os endpoints e branches do <see cref="BranchesController"/>.
/// </summary>
public class BranchesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly BranchesController _controller;

    public BranchesControllerTests()
    {
        _controller = new BranchesController(_mediator, _mapper);
        _controller.WithEmptyContext();
    }

    private static CreateBranchRequest ValidCreate() => new()
    {
        Name = "Filial Centro",
        Cnpj = "12.345.678/0001-95"
    };

    private static UpdateBranchRequest ValidUpdate() => new()
    {
        Name = "Filial Centro v2",
        Cnpj = "12.345.678/0001-95"
    };

    private static BranchDto FakeDto(int id) => new()
    {
        Id = id,
        Name = "Filial Centro",
        Cnpj = "12345678000195",
        CreatedByUserId = 1,
        CreatedAt = DateTime.UtcNow,
        LastModifiedAt = DateTime.UtcNow
    };

    [Fact(DisplayName = "List: retorna 200 com payload mapeado")]
    public async Task List_WhenInvoked_ReturnsOk()
    {
        var commandResult = new ListBranchesResult
        {
            Data = new List<BranchDto> { FakeDto(Random.Shared.Next(1, int.MaxValue)) },
            TotalItems = 1,
            CurrentPage = 2,
            TotalPages = 1
        };
        var responseDto = new ListBranchesResponse
        {
            Data = new List<BranchResponse>(),
            TotalItems = 1,
            CurrentPage = 2,
            TotalPages = 1
        };
        _mediator.Send(Arg.Is<ListBranchesCommand>(c => c.Page == 2 && c.Size == 7), Arg.Any<CancellationToken>())
                 .Returns(commandResult);
        _mapper.Map<ListBranchesResponse>(commandResult).Returns(responseDto);

        var response = await _controller.List(2, 7, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "GetById: id existente retorna 200 com payload mapeado")]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var dto = FakeDto(id);
        var responseDto = new BranchResponse { Id = id, Name = dto.Name, Cnpj = dto.Cnpj };

        _mediator.Send(Arg.Is<GetBranchCommand>(c => c.Id == id), Arg.Any<CancellationToken>())
                 .Returns(dto);
        _mapper.Map<BranchResponse>(dto).Returns(responseDto);

        var response = await _controller.GetById(id, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "GetById: KeyNotFoundException retorna 404")]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<GetBranchCommand>(), Arg.Any<CancellationToken>())
                 .Returns<BranchDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.GetById(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: válido com usuário autenticado retorna 201 (CreatedAtAction)")]
    public async Task Create_WhenValidAndAuthenticated_ReturnsCreated()
    {
        _controller.WithAuthenticatedUser(33);

        var dto = FakeDto(Random.Shared.Next(1, int.MaxValue));
        var responseDto = new BranchResponse { Id = dto.Id, Name = dto.Name, Cnpj = dto.Cnpj };

        _mediator.Send(Arg.Is<CreateBranchCommand>(c => c.CreatedByUserId == 33 && c.Name == "Filial Centro"),
                       Arg.Any<CancellationToken>()).Returns(dto);
        _mapper.Map<BranchResponse>(dto).Returns(responseDto);

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BranchesController.GetById));
        created.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "Create: payload inválido retorna 400")]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        _controller.WithAuthenticatedUser(33);

        var response = await _controller.Create(new CreateBranchRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<ApiErrorResponse>();
    }

    [Fact(DisplayName = "Create: claim ausente retorna 401")]
    public async Task Create_WhenNoUserClaim_ReturnsUnauthorized()
    {
        _controller.WithUnauthenticatedUser();

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        response.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "Create: claim NameIdentifier inválido retorna 401")]
    public async Task Create_WhenInvalidUserClaim_ReturnsUnauthorized()
    {
        _controller.WithUnauthenticatedUser("abc");

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        response.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "Create: handler dispara ValidationException retorna 400")]
    public async Task Create_WhenHandlerValidationException_ReturnsBadRequest()
    {
        _controller.WithAuthenticatedUser(33);

        _mediator.Send(Arg.Any<CreateBranchCommand>(), Arg.Any<CancellationToken>())
            .Returns<BranchDto>(_ => throw new ValidationException(new[]
            {
                new ValidationFailure("Cnpj", "duplicado")
            }));

        var response = await _controller.Create(ValidCreate(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: válido retorna 200")]
    public async Task Update_WhenValid_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        var dto = FakeDto(id);
        var responseDto = new BranchResponse { Id = id, Name = dto.Name, Cnpj = dto.Cnpj };

        _mediator.Send(Arg.Is<UpdateBranchCommand>(c => c.Id == id && c.Name == "Filial Centro v2"),
                       Arg.Any<CancellationToken>()).Returns(dto);
        _mapper.Map<BranchResponse>(dto).Returns(responseDto);

        var response = await _controller.Update(id, ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(responseDto);
    }

    [Fact(DisplayName = "Update: payload inválido retorna 400")]
    public async Task Update_WhenInvalid_ReturnsBadRequest()
    {
        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue), new UpdateBranchRequest(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Update: KeyNotFoundException retorna 404")]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<UpdateBranchCommand>(), Arg.Any<CancellationToken>())
                 .Returns<BranchDto>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue), ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Update: ValidationException retorna 400")]
    public async Task Update_WhenValidationException_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<UpdateBranchCommand>(), Arg.Any<CancellationToken>())
            .Returns<BranchDto>(_ => throw new ValidationException(new[]
            {
                new ValidationFailure("Cnpj", "inválido")
            }));

        var response = await _controller.Update(Random.Shared.Next(1, int.MaxValue), ValidUpdate(), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "Delete: válido retorna 200 com objeto deletado")]
    public async Task Delete_WhenFound_ReturnsOk()
    {
        var id = Random.Shared.Next(1, int.MaxValue);
        _mediator.Send(Arg.Is<DeleteBranchCommand>(c => c.Id == id), Arg.Any<CancellationToken>())
                 .Returns(new DeleteBranchResult { Deleted = true });

        var response = await _controller.Delete(id, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "Delete: KeyNotFoundException retorna 404")]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<DeleteBranchCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteBranchResult>(_ => throw new KeyNotFoundException("missing"));

        var response = await _controller.Delete(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "Delete: ValidationException retorna 400")]
    public async Task Delete_WhenValidationException_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<DeleteBranchCommand>(), Arg.Any<CancellationToken>())
                 .Returns<DeleteBranchResult>(_ => throw new ValidationException(new[]
                 {
                     new ValidationFailure("Id", "Filial em uso")
                 }));

        var response = await _controller.Delete(Random.Shared.Next(1, int.MaxValue), CancellationToken.None);

        response.Should().BeOfType<BadRequestObjectResult>();
    }
}




