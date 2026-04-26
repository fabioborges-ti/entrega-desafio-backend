using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Auth;

/// <summary>
/// Autenticação JWT alinhada a <c>POST /api/auth/login</c> (auth-api.md).
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public AuthController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Autentica o usuário e retorna um token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthenticateUserRequest request, CancellationToken cancellationToken)
    {
        var validator = new AuthenticateUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var command = _mapper.Map<AuthenticateUserCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new LoginTokenResponse { Token = result.Token });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiErrorResponse.Authentication("Invalid authentication token", ex.Message));
        }
    }
}

/// <summary>Contrato mínimo da documentação: apenas o JWT.</summary>
public sealed class LoginTokenResponse
{
    public string Token { get; set; } = string.Empty;
}

