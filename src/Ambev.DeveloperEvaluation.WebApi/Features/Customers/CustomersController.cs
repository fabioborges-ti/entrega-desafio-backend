using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using Ambev.DeveloperEvaluation.WebApi.Common;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager", Policy = "ActiveUser")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public CustomersController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Lista clientes com paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListCustomersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery(Name = "_page")] int page = 1,
        [FromQuery(Name = "_size")] int size = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListCustomersCommand { Page = page, Size = size },
            cancellationToken);
        return Ok(_mapper.Map<ListCustomersResponse>(result));
    }

    /// <summary>Obtém um cliente por ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetCustomerCommand { Id = id }, cancellationToken);
            return Ok(_mapper.Map<CustomerResponse>(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Cliente não encontrado", ex.Message));
        }
    }

    /// <summary>Cria um novo cliente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new CreateCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var dto = await _mediator.Send(
                new CreateCustomerCommand { Name = request.Name },
                cancellationToken);
            var response = _mapper.Map<CustomerResponse>(dto);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Atualiza os dados de um cliente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateCustomerRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        try
        {
            var dto = await _mediator.Send(
                new UpdateCustomerCommand { Id = id, Name = request.Name },
                cancellationToken);
            return Ok(_mapper.Map<CustomerResponse>(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Cliente não encontrado", ex.Message));
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }

    /// <summary>Exclui um cliente por ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteCustomerCommand { Id = id }, cancellationToken);
            return Ok(new { deleted = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Cliente não encontrado", ex.Message));
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(ApiErrorResponse.FromValidationFailures(ex.Errors));
        }
    }
}


