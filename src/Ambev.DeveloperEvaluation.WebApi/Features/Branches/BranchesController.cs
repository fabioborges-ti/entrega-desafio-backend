using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.WebApi.Common;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public BranchesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Lista filiais com paginação.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpGet]
    [ProducesResponseType(typeof(ListBranchesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListBranchesCommand { Page = page, Size = size },
            cancellationToken);
        return Ok(_mapper.Map<ListBranchesResponse>(result));
    }

    /// <summary>Obtém uma filial por ID.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetBranchCommand { Id = id }, cancellationToken);
            return Ok(_mapper.Map<BranchResponse>(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Filial não encontrada", ex.Message));
        }
    }

    /// <summary>Cria uma nova filial.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpPost]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var dto = await _mediator.Send(
                new CreateBranchCommand
                {
                    Name = request.Name,
                    Cnpj = request.Cnpj,
                    CreatedByUserId = userId
                },
                cancellationToken);
            var response = _mapper.Map<BranchResponse>(dto);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Atualiza os dados de uma filial.</summary>
    [Authorize(Roles = "Admin,Manager", Policy = "ActiveUser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateBranchRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var dto = await _mediator.Send(
                new UpdateBranchCommand { Id = id, Name = request.Name, Cnpj = request.Cnpj },
                cancellationToken);
            return Ok(_mapper.Map<BranchResponse>(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Filial não encontrada", ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Exclui uma filial por ID.</summary>
    [Authorize(Roles = "Admin", Policy = "ActiveUser")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteBranchCommand { Id = id }, cancellationToken);
            return Ok(new { deleted = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Filial não encontrada", ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }
}


