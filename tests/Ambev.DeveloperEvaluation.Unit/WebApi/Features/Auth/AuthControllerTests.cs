using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Auth;

/// <summary>
/// Cobertura unitária para todos os caminhos do <see cref="AuthController"/>.
/// </summary>
public class AuthControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_mediator, _mapper);
    }

    private static AuthenticateUserRequest ValidRequest() => new()
    {
        Username = "fulano",
        Password = "Pwd@12345"
    };

    [Fact(DisplayName = "Login: credenciais válidas retornam 200 com token")]
    public async Task Login_WhenValid_ReturnsOkWithToken()
    {
        var request = ValidRequest();
        var command = new AuthenticateUserCommand { Username = request.Username, Password = request.Password };
        var result = new AuthenticateUserResult { Token = "jwt-token", Email = "x@y.com", Name = "X", Role = "Admin" };

        _mapper.Map<AuthenticateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);

        var response = await _controller.Login(request, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<LoginTokenResponse>().Subject;
        body.Token.Should().Be("jwt-token");
        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Login: request inválido retorna 400 com payload de erro de validação")]
    public async Task Login_WhenInvalid_ReturnsBadRequest()
    {
        var request = new AuthenticateUserRequest { Username = "", Password = "" };

        var response = await _controller.Login(request, CancellationToken.None);

        var bad = response.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeOfType<ApiErrorResponse>()
           .Which.Type.Should().Be("ValidationError");
        await _mediator.DidNotReceive().Send(Arg.Any<AuthenticateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Login: handler dispara UnauthorizedAccessException retorna 401")]
    public async Task Login_WhenHandlerThrowsUnauthorized_ReturnsUnauthorized()
    {
        var request = ValidRequest();
        var command = new AuthenticateUserCommand { Username = request.Username, Password = request.Password };

        _mapper.Map<AuthenticateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns<AuthenticateUserResult>(_ => throw new UnauthorizedAccessException("invalid creds"));

        var response = await _controller.Login(request, CancellationToken.None);

        var unauth = response.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauth.Value.Should().BeOfType<ApiErrorResponse>()
              .Which.Type.Should().Be("AuthenticationError");
    }
}

