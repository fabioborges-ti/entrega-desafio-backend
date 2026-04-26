using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Route("api/[controller]")]
public class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ISaleCommandPublisher _saleCommandPublisher;
    private readonly ISalesMessageStatusStore _messageStatusStore;

    public SalesController(
        IMediator mediator,
        IMapper mapper,
        ISaleCommandPublisher saleCommandPublisher,
        ISalesMessageStatusStore messageStatusStore)
    {
        _mediator = mediator;
        _mapper = mapper;
        _saleCommandPublisher = saleCommandPublisher;
        _messageStatusStore = messageStatusStore;
    }

    /// <summary>Enfileira a criação de uma venda (processamento assíncrono).</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var correlationId = await _saleCommandPublisher.PublishCreateAsync(
            new CreateSaleRequestedMessage(
                request.SaleDate,
                request.SaleNumber,
                request.CustomerId,
                request.BranchId,
                request.CartId),
            cancellationToken);

        return Accepted(new ApiResponseWithData<object>
        {
            Success = true,
            Message = "Solicitação de criação de venda enfileirada com sucesso",
            Data = new { CorrelationId = correlationId }
        });
    }

    /// <summary>Obtém os dados de uma venda por ID.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<GetSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<GetSaleCommand>(id);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseWithData<GetSaleResponse>
            {
                Success = true,
                Message = "Venda obtida com sucesso",
                Data = _mapper.Map<GetSaleResponse>(result)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound("Venda não encontrada", ex.Message));
        }
    }

    /// <summary>Lista vendas com paginação.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<ListSalesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSales([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var command = new ListSalesCommand { Page = page, PageSize = pageSize };
        var result = await _mediator.Send(command, cancellationToken);
        var data = _mapper.Map<ListSalesResponse>(result);

        return Ok(new ApiResponseWithData<ListSalesResponse>
        {
            Success = true,
            Message = "Lista de vendas obtida com sucesso",
            Data = data
        });
    }

    /// <summary>Consulta o status de processamento por correlationId.</summary>
    [Authorize(Roles = "Customer", Policy = "ActiveUser")]
    [HttpGet("messages/{correlationId}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SalesMessageStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetMessageStatus([FromRoute] string correlationId)
    {
        if (!_messageStatusStore.TryGet(correlationId, out var status) || status == null)
        {
            return NotFound(ApiErrorResponse.ResourceNotFound(
                "Status da mensagem não encontrado",
                $"Não encontramos status para o correlationId '{correlationId}'."));
        }

        return Ok(new ApiResponseWithData<SalesMessageStatus>
        {
            Success = true,
            Message = "Status da mensagem obtido com sucesso",
            Data = status
        });
    }

    /// <summary>Enfileira a atualização de uma venda (processamento assíncrono).</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSale([FromRoute] int id, [FromBody] UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(ApiErrorResponse.FromValidationFailures(validationResult.Errors));

        var correlationId = await _saleCommandPublisher.PublishUpdateAsync(
            new UpdateSaleRequestedMessage(
                id,
                request.SaleDate,
                request.CustomerId,
                request.BranchId,
                request.CartId),
            cancellationToken);

        return Accepted(new ApiResponseWithData<object>
        {
            Success = true,
            Message = "Solicitação de atualização de venda enfileirada com sucesso",
            Data = new { CorrelationId = correlationId }
        });
    }

    /// <summary>Enfileira a exclusão de uma venda (processamento assíncrono).</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponseWithData<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSale([FromRoute] int id, CancellationToken cancellationToken)
    {
        var correlationId = await _saleCommandPublisher.PublishDeleteAsync(
            new DeleteSaleRequestedMessage(id),
            cancellationToken);

        return Accepted(new ApiResponseWithData<object>
        {
            Success = true,
            Message = "Solicitação de exclusão de venda enfileirada com sucesso",
            Data = new { CorrelationId = correlationId }
        });
    }

    /// <summary>Enfileira o cancelamento de uma venda (processamento assíncrono).</summary>
    [Authorize(Roles = "Manager", Policy = "ActiveUser")]
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale([FromRoute] int id, CancellationToken cancellationToken)
    {
        var correlationId = await _saleCommandPublisher.PublishCancelAsync(
            new CancelSaleRequestedMessage(id),
            cancellationToken);

        return Accepted(new ApiResponseWithData<object>
        {
            Success = true,
            Message = "Solicitação de cancelamento de venda enfileirada com sucesso",
            Data = new { CorrelationId = correlationId }
        });
    }

}


