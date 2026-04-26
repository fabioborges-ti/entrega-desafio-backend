using Ambev.DeveloperEvaluation.Application.Users.ChangePassword;
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.ListUsers;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.ChangePassword;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.DeleteUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users;

/// <summary>
/// CRUD de usuários restrito a administradores (JWT role Admin).
/// Contrato: <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/users-api.md">users-api.md</see>.
/// </summary>
[ApiController]
public class UsersController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public UsersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Lista usuários com paginação e ordenação.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpGet]
    [ProducesResponseType(typeof(UsersPagedListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        [FromQuery(Name = "_order")] string? order = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListUsersQuery(page, size, order), cancellationToken);
        return Ok(new UsersPagedListResponse
        {
            Success = true,
            Message = "Users listed successfully",
            Data = result.Data.Select(r => _mapper.Map<GetUserResponse>(r)).ToList(),
            TotalItems = result.TotalItems,
            CurrentPage = result.CurrentPage,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>Cria um novo usuário.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var command = _mapper.Map<CreateUserCommand>(request);
            var response = await _mediator.Send(command, cancellationToken);
            return Created(string.Empty, new ApiResponseWithData<CreateUserResponse>
            {
                Success = true,
                Message = "User created successfully",
                Data = _mapper.Map<CreateUserResponse>(response)
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse { Success = false, Message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Obtém os dados de um usuário por ID.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        var request = new GetUserRequest { Id = id };
        var validator = new GetUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var command = _mapper.Map<GetUserCommand>(request.Id);
            var response = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseWithData<GetUserResponse>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = _mapper.Map<GetUserResponse>(response)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("User not found", ex.Message));
        }
    }

    /// <summary>Atualiza os dados de um usuário existente.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var command = _mapper.Map<UpdateUserCommand>(request);
        command.Id = id;

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseWithData<GetUserResponse>
            {
                Success = true,
                Message = "User updated successfully",
                Data = _mapper.Map<GetUserResponse>(response)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("User not found", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse { Success = false, Message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Altera a senha do usuário.</summary>
    [Authorize(Policy = "ActiveUser")]
    [HttpPatch("{id:int}/password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] int id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (id != GetCurrentUserId())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ApiResponse { Success = false, Message = "Você só pode alterar a própria senha." });
        }

        var validator = new ChangePasswordRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            await _mediator.Send(
                new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword),
                cancellationToken);
            return Ok(new ApiResponse { Success = true, Message = "Senha alterada com sucesso." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("User not found", ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse { Success = false, Message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Remove um usuário e retorna o registro excluído.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        var request = new DeleteUserRequest { Id = id };
        var validator = new DeleteUserRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var command = _mapper.Map<DeleteUserCommand>(request.Id);
            var deleted = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseWithData<GetUserResponse>
            {
                Success = true,
                Message = "User deleted successfully",
                Data = _mapper.Map<GetUserResponse>(deleted)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("User not found", ex.Message));
        }
    }
}
